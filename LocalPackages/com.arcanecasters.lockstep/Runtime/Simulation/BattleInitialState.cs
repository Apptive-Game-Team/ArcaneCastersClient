using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    [Serializable] public sealed class BattlePersonaSnapshot {
        public string name;
        public string tier;
        public int thinkingTimeMs;
        public int reactionIntervalFrames;
        public long counterAggressionRaw;
        public bool enabled;
        public bool hospitality;
        public string temperament;
        public string brainType;
        public string llmModel;
        internal BotPersona Build(long id) => new BotPersona {
            UserId = id, Name = name, Tier = BattleInitialState.Parse<BotTier>(tier), ThinkingTimeMs = thinkingTimeMs,
            ReactionIntervalFrames = reactionIntervalFrames, CounterAggression = Fix64.FromRaw(counterAggressionRaw), Enabled = enabled,
            Hospitality = hospitality, Temperament = BattleInitialState.Parse<BotTemperament>(temperament),
            BrainType = BattleInitialState.Parse<BotBrainType>(brainType), LlmModel = llmModel
        };
    }
    [Serializable] public sealed class BattleParticipantSnapshot {
        public long userId;
        public long noviceProgressRaw;
        public BattlePersonaSnapshot persona;
    }
    [Serializable] public sealed class BattleInitialState {
        public int schemaVersion;
        public long randomSeed;
        public string sessionType;
        public string map;
        public GameMapPolicySnapshot mapPolicy;
        public long? scenarioId;
        public long[] leftDeck;
        public long[] rightDeck;
        public BattleContentSnapshot content;
        public BattleTagSnapshot tags;
        public BattleParticipantSnapshot[] profiles;
        public BattleModelPolicySnapshot[] modelPolicies;
        public BattlePveSnapshot pve;
        internal static T Parse<T>(string value) where T : struct {
            if (value == null || !Enum.GetNames(typeof(T)).Contains(value)) throw new InvalidDataException("Unknown battle " + typeof(T).Name + ".");
            return (T)Enum.Parse(typeof(T), value);
        }
        public BattleSessionRuntime CreateRuntime(SessionStart start, IBattleFrameOutput output = null, IPveSessionSink pveSink = null,
            BotDecisionSourceFactory decisionFactory = null) {
            if (schemaVersion != 1 || start?.participants == null || start.participants.Length != 2 || start.tickRate != 20 || leftDeck == null || rightDeck == null || profiles == null || profiles.Length != 2)
                throw new InvalidDataException("Invalid battle initial snapshot.");
            long left = start.participants[0], right = start.participants[1];
            var contentSource = new BattleContent(content, left, right);
            var tagSource = new BattleTagCatalog(tags);
            var personas = new Dictionary<long, BotPersona>(); var progress = new Dictionary<long, Fix64>();
            foreach (var profile in profiles) {
                if (profile == null || (profile.userId != left && profile.userId != right) || progress.ContainsKey(profile.userId)) throw new InvalidDataException("Invalid battle participant profile.");
                progress.Add(profile.userId, Fix64.FromRaw(profile.noviceProgressRaw));
                personas.Add(profile.userId, profile.persona?.Build(profile.userId));
            }
            var type = Parse<SessionType>(sessionType); var selectedMap = Parse<GameMap>(map);
            if (mapPolicy != null && mapPolicy.Choose(type, scenarioId) != selectedMap)
                throw new InvalidDataException("Confirmed map does not match map selection policy.");
            var definition = new SessionDefinition(start.sessionId, left, right, randomSeed, leftDeck, rightDeck, type, selectedMap, scenarioId);
            PveScenarioRegistry scenarios = null;
            if (definition.Type == SessionType.PVE) {
                long id = scenarioId ?? PveMatch.DefaultScenarioId;
                if (pve == null || pve.scenarioId != id) throw new InvalidDataException("PVE scenario does not match the initial snapshot.");
                var scenario = pve.Build(); scenarios = new PveScenarioRegistry(requested => requested == id ? scenario : null);
            }
            if (LockstepVersion.UsesModelProtocol(start.simulationVersion)) {
                var policies = ValidateModelPolicies(start, personas);
                if (decisionFactory == null) decisionFactory = ModelBotDecisions.Confirmed(randomSeed, policies);
            }
            var factory = new BattleRuntimeFactory(contentSource, tagSource.CreateEvaluator(), id => personas[id], id => progress[id], decisionFactory);
            return factory.Create(definition, output, scenarios, pveSink);
        }
        private BattleModelPolicySnapshot[] ValidateModelPolicies(SessionStart start, IDictionary<long, BotPersona> personas) {
            var policies = new BattleModelPolicySnapshot[2];
            foreach (var policy in modelPolicies ?? Array.Empty<BattleModelPolicySnapshot>()) {
                if (policy == null || policy.slot < 0 || policy.slot > 1 || policies[policy.slot] != null
                    || start.participants[policy.slot] >= 0)
                    throw new InvalidDataException("Invalid frozen model policy slot.");
                policy.Validate(personas[start.participants[policy.slot]]);
                policies[policy.slot] = policy.Copy();
            }
            for (int slot = 0; slot < 2; slot++) {
                var persona = personas[start.participants[slot]];
                if (start.participants[slot] < 0 && persona != null && BotPersona.UsesLlm(persona.BrainType) && policies[slot] == null)
                    throw new InvalidDataException("Missing frozen model policy.");
            }
            return policies;
        }
    }
}
