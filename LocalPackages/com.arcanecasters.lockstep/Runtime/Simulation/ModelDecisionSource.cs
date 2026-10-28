using System;
using System.Collections.Generic;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public enum ModelDecisionPolicy { Cast, Stance }
    public enum ModelStance { Attack, Defend, Save }

    // 외부 모델 결정을 확정 입력으로만 받는 결정 소스다. Think 시점에 요청 intent를 만들고, 결과는 그것을 담은 확정 tick에서만 반영한다.
    // 분기별 규칙 brain·후보 호출 횟수와 재조준·손패 확인·소환 위치 규칙은 원본과 같다. 후보 메뉴 자체는 아직 표준 선택지만 담으므로
    // provider별 선택지 본문까지 포함한 원본 전체 동등 구현은 아니다.
    public sealed class ModelDecisionSource : IModelDecisionSource {
        // 원본의 min_interval_millis 1500과 MANA_GUARD 100을 20 tick 기준으로 옮긴 값이다.
        public const uint DefaultMinRequestIntervalTicks = 30;
        public const int ManaGuard = 100;
        private readonly IBotDecisionSource fallback;
        private readonly IModelCandidateBuilder candidates;
        private readonly ModelDecisionPolicy policy;
        private readonly string ruleId;
        private readonly uint minRequestIntervalTicks;
        private ModelRequestChannel channel;
        private ModelCandidateMenu answeredMenu;
        private ModelEvent result;
        private ModelStance stance = ModelStance.Defend;
        private uint lastRequestTick;
        private bool requested;
        public ModelStance Stance => stance;
        public ModelDecisionPolicy Policy => policy;
        public uint MinRequestIntervalTicks => minRequestIntervalTicks;
        public ModelDecisionSource(IBotDecisionSource fallback, IModelCandidateBuilder candidates, ModelDecisionPolicy policy,
            string ruleId, uint minRequestIntervalTicks = DefaultMinRequestIntervalTicks) {
            this.fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
            this.candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
            this.policy = policy;
            this.ruleId = string.IsNullOrEmpty(ruleId) ? throw new ArgumentException(nameof(ruleId)) : ruleId;
            this.minRequestIntervalTicks = minRequestIntervalTicks;
        }
        public static string RuleIdFor(BotBrainType brain) {
            switch (brain) {
                case BotBrainType.Jev: return "llm.jev";
                case BotBrainType.Openrouter: return "llm.openrouter";
                default: throw new ArgumentOutOfRangeException(nameof(brain));
            }
        }
        public static ModelDecisionPolicy PolicyFor(BotBrainType brain) => brain == BotBrainType.Jev ? ModelDecisionPolicy.Stance : ModelDecisionPolicy.Cast;

        public void Bind(ModelRequestChannel bound) {
            if (channel != null) throw new BotDecisionProtocolException("A model decision source binds to one bot generation only.");
            channel = bound ?? throw new ArgumentNullException(nameof(bound));
        }
        public void Deliver(ModelEvent confirmed) {
            if (confirmed == null) throw new ArgumentNullException(nameof(confirmed));
            if (result != null) throw new BotDecisionProtocolException("A confirmed model result is already waiting for this bot.");
            result = confirmed;
        }

        // 원본은 분기마다 규칙 brain과 후보 계산 횟수가 다르다. 그 횟수가 난수 수열의 진행이므로 지연 계산으로 분기별 호출을 그대로 보존한다.
        private sealed class Turn {
            private readonly ModelDecisionSource source;
            private readonly BotEye eye;
            private readonly IDictionary<string, Fix64> parameters;
            private readonly Master side;
            private readonly bool overdue;
            private InputDecision standard;
            private ModelCandidateMenu menu;
            private bool standardComputed, menuComputed;
            public Turn(ModelDecisionSource source, BotEye eye, IDictionary<string, Fix64> parameters, Master side, bool overdue) {
                this.source = source; this.eye = eye; this.parameters = parameters; this.side = side; this.overdue = overdue;
            }
            public BotEye Eye => eye;
            public InputDecision Standard() {
                if (standardComputed) return standard;
                standardComputed = true;
                return standard = source.fallback.Think(eye, parameters, side, overdue);
            }
            // Cast 정책은 표준 선택지를 담아, Stance 정책은 원본처럼 standard:null로 후보를 만든다.
            public ModelCandidateMenu Menu() {
                if (menuComputed) return menu;
                menuComputed = true;
                var given = source.policy == ModelDecisionPolicy.Cast ? Standard() : null;
                return menu = source.candidates.Build(eye, parameters, side, given)
                    ?? throw new BotDecisionProtocolException("Model candidate menu is missing.");
            }
        }

        public InputDecision Think(BotEye eye, IDictionary<string, Fix64> parameters, Master side, bool overdue) {
            if (channel == null) throw new BotDecisionProtocolException("Model bot is not bound to the confirmed model ledger.");
            if (side != channel.Side) throw new BotDecisionProtocolException("Model bot changed sides.");
            var turn = new Turn(this, eye, parameters, side, overdue);
            var confirmed = result; result = null;
            return policy == ModelDecisionPolicy.Stance ? ThinkStance(turn, confirmed) : ThinkCast(turn, confirmed);
        }

        private InputDecision ThinkCast(Turn turn, ModelEvent confirmed) {
            if (confirmed != null) {
                var answered = answeredMenu; answeredMenu = null;
                // 거부·실패·backoff는 그 확정 프레임에서 규칙 brain이 판단한다. 후보는 만들지 않는다.
                if (confirmed.kind != ModelProtocol.Completed) return turn.Standard();
                if (answered == null) throw new BotDecisionProtocolException("Model result arrived without a recorded candidate menu.");
                // 대기 선택은 규칙 brain도 후보도 건드리지 않는다.
                if (confirmed.playKey == ModelProtocol.WaitPlayKey) {
                    if (!answered.OffersWait) throw new BotDecisionProtocolException("Model answered wait where waiting was not offered.");
                    return null;
                }
                var chosen = answered.Play(confirmed.playKey)
                    ?? throw new BotDecisionProtocolException("Model answered play " + confirmed.playKey + ", which the observation did not offer.");
                // 답이 가리킨 카드가 이미 손패를 떠났으면 시전하지 않고, 이때도 규칙 brain을 부르지 않는다.
                if (!Holds(turn.Eye, chosen.MagicId)) return null;
                if (confirmed.aimKey != null) {
                    var aim = chosen.Aim(confirmed.aimKey)
                        ?? throw new BotDecisionProtocolException("Model answered aim " + confirmed.aimKey + ", which the observation did not offer.");
                    // 모델이 고른 소환 위치는 그 사이 유닛이 움직여도 그대로 둔다. 재조준이 없으므로 계산도 없다.
                    return Cast(chosen, aim.Target, Reason(confirmed, confirmed.playKey));
                }
                return Reaim(turn, chosen, confirmed);
            }
            if (channel.HasOutstandingRequest) return null;
            if (channel.CurrentTick < channel.ResumeTick) return turn.Standard();
            if (requested && channel.CurrentTick - lastRequestTick < minRequestIntervalTicks) return null;
            var menu = turn.Menu();
            if (!menu.HasPlays) return null;
            // 선택지가 하나뿐이고 기다릴 이유가 없으면 묻지 않고 바로 시전한다.
            if (menu.Plays.Count == 1 && !menu.OffersWait) {
                var only = menu.Plays[0];
                return Cast(only, only.Target, "only play: " + only.Key);
            }
            Ask(turn.Eye, menu, menu.Criteria());
            answeredMenu = menu;
            return null;
        }

        private InputDecision ThinkStance(Turn turn, ModelEvent confirmed) {
            if (confirmed != null) {
                answeredMenu = null;
                if (confirmed.kind == ModelProtocol.Completed) {
                    if (confirmed.aimKey != null) throw new BotDecisionProtocolException("A stance answer carries no aim.");
                    stance = ParseStance(confirmed.playKey)
                        ?? throw new BotDecisionProtocolException("Model answered stance " + confirmed.playKey + ", which is not a stance.");
                }
                // 실패·거부·backoff는 기존 stance를 그대로 둔다.
            }
            // 원본처럼 규칙 brain을 먼저 한 번 부르고, backoff 중에는 후보를 만들지 않는다.
            var rule = turn.Standard();
            if (channel.CurrentTick < channel.ResumeTick) return rule;
            var menu = turn.Menu();
            var stanceTurn = new ModelStanceTurn(turn.Eye, menu, rule, ruleId);
            // 원본 maybe_ask처럼 stance 선택지 본문을 묻는다.
            if (!channel.HasOutstandingRequest && (!requested || channel.CurrentTick - lastRequestTick >= minRequestIntervalTicks))
                Ask(turn.Eye, menu, stanceTurn.Criteria());
            // 요청 결과를 기다리는 동안에도 현재 stance로 계속 판단한다.
            return stanceTurn.Play(stance);
        }


        // 정확한 Think 시점의 메뉴와 criteria에서 provider 입력을 고정한다. client는 provider/model/URL/system prompt를 정하지 않는다.
        private void Ask(BotEye eye, ModelCandidateMenu menu, ModelJsonMap criteria) {
            string policy = this.policy == ModelDecisionPolicy.Stance ? ModelProtocol.StancePolicy : ModelProtocol.CastPolicy;
            ModelRequestPayload payload;
            try { payload = ModelRequestPayload.Of(policy, menu, criteria); }
            catch (System.IO.InvalidDataException error) { throw new BotDecisionProtocolException("Model request payload is not canonical: " + error.Message); }
            channel.Submit((sequence, tick) =>
                ModelDigest.Compute(channel.SessionId, channel.Slot, channel.Generation, sequence, tick, eye, menu, payload), payload);
            lastRequestTick = channel.CurrentTick; requested = true;
        }

        private InputDecision Reaim(Turn turn, ModelCandidatePlay chosen, ModelEvent confirmed) {
            // 답은 한 반응 주기 전의 관측에 대한 것이다. 현재 전장에서 같은 마법의 최신 지점으로 다시 겨눈다.
            string reason = Reason(confirmed, confirmed.playKey);
            var standard = turn.Standard();
            if (chosen.Standard && standard != null && standard.MagicId == chosen.MagicId) return Cast(chosen, standard.Target, reason);
            var fresh = turn.Menu().Nearest(chosen.MagicId, chosen.Target);
            if (fresh != null) return Cast(chosen, fresh.Target, reason);
            return chosen.Summon ? Cast(chosen, chosen.Target, reason) : null;
        }

        // provider가 고른 사유를 그대로 보존하고, 없을 때만 선택지 키로 적는다.
        private static string Reason(ModelEvent confirmed, string playKey) =>
            ModelProtocol.TruncateReason(confirmed.reason) ?? "model play: " + playKey;

        private InputDecision Cast(ModelCandidatePlay play, Vector3 target, string reason) {
            string rule = policy == ModelDecisionPolicy.Stance ? ruleId + "." + StanceKey(stance) : ruleId;
            return new InputDecision(play.MagicId, target, rule, reason);
        }
        private static bool Holds(BotEye eye, long magicId) {
            foreach (long card in eye.Cards) if (card == magicId) return true;
            return false;
        }
        public static string StanceKey(ModelStance value) {
            switch (value) {
                case ModelStance.Attack: return "attack";
                case ModelStance.Defend: return "defend";
                case ModelStance.Save: return "save";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
        public static ModelStance? ParseStance(string key) {
            foreach (ModelStance value in new[] { ModelStance.Attack, ModelStance.Defend, ModelStance.Save })
                if (StanceKey(value) == key) return value;
            return null;
        }
    }

    // 원본 stance_decision_source.rs의 Turn이다. 한 관측에서 각 stance가 무엇을 낼지 정한다.
    public sealed class ModelStanceTurn {
        private const string Blocked = "not possible now, an enemy unit is close to our base, so this defends instead.";
        private readonly BotEye eye;
        private readonly ModelCandidateMenu menu;
        private readonly InputDecision rule;
        private readonly string ruleId;
        public ModelStanceTurn(BotEye eye, ModelCandidateMenu menu, InputDecision rule, string ruleId = "llm.jev") {
            this.eye = eye ?? throw new ArgumentNullException(nameof(eye));
            this.menu = menu ?? throw new ArgumentNullException(nameof(menu));
            this.rule = rule; this.ruleId = ruleId;
        }
        public bool Urgent => menu.Urgent;
        public InputDecision Play(ModelStance stance) {
            // 적 유닛이 기지에 붙었으면 틀린 계획이 템포만 잃게 두고 기지는 지킨다.
            if (stance == ModelStance.Defend || menu.Urgent) return rule;
            var play = stance == ModelStance.Attack ? menu.AttackPlay : menu.SavePlay;
            if (play != null) return Cast(play, stance);
            return eye.Mana >= ModelDecisionSource.ManaGuard ? rule : null;
        }
        private InputDecision Cast(ModelCandidatePlay play, ModelStance stance) => new InputDecision(play.MagicId, play.Target,
            ruleId + "." + ModelDecisionSource.StanceKey(stance),
            ModelText.Truncate(ModelDecisionSource.StanceKey(stance) + ": " + play.Description));

        // jev가 읽는 계획별 선택지 본문이다. 지금 무엇을 낼지 말한다.
        public ModelJsonMap Criteria() {
            var criteria = new ModelJsonMap();
            var attackPlay = menu.AttackPlay;
            string attack = menu.Urgent ? Blocked
                : attackPlay == null ? "nothing in hand can reach the enemy base or push; hold mana." : "cast " + attackPlay.Description;
            criteria.Put(ModelDecisionSource.StanceKey(ModelStance.Attack), "Attack: " + attack);
            string defend = rule == null ? "nothing to defend with right now; hold mana."
                : "the cautious rule bot's play: " + Describe(rule);
            criteria.Put(ModelDecisionSource.StanceKey(ModelStance.Defend), "Defend: " + defend);
            var savePlay = menu.SavePlay;
            string save = menu.Urgent ? Blocked
                : savePlay == null
                    ? "cast nothing until the dearest card in hand (cost " + menu.DearestManaCost + ") is affordable; mana is " + eye.Mana + " now."
                    : "the dearest card is affordable, cast " + savePlay.Description;
            criteria.Put(ModelDecisionSource.StanceKey(ModelStance.Save), "Save: " + save);
            return criteria;
        }
        private string Describe(InputDecision decision) => menu.Nearest(decision.MagicId, decision.Target)?.Description ?? decision.Reason;
    }

    // lockstep-v2 경기의 모델 봇 생성 경로다. 규칙 봇 persona는 규칙 소스를 그대로 돌려준다.
    public static class ModelBotDecisions {
        // slot별 설정을 복사하여 호출자가 snapshot을 바꾸어도 재생 설정은 유지한다.
        public static BotDecisionSourceFactory Confirmed(long sessionSeed, BattleModelPolicySnapshot[] policies,
            MagicDescriptions descriptions = null) {
            if (policies == null || policies.Length != 2) throw new ArgumentException(nameof(policies));
            var frozen = new[] { policies[0]?.Copy(), policies[1]?.Copy() };
            return (side, generation, persona, fallback) => {
                if (!BotPersona.UsesLlm(persona.BrainType)) return fallback;
                int slot = side == Master.LeftPlayer ? 0 : 1;
                var selected = frozen[slot] ?? throw new InvalidOperationException("Missing frozen model policy.");
                if (selected.slot != slot) throw new InvalidOperationException("Frozen model policy belongs to a different slot.");
                selected.Validate(persona);
                if (selected.fallbackToRule) return fallback;
                var brain = (fallback as RuleDecisionSource)?.Brain
                    ?? throw new InvalidOperationException("A model bot needs the rule brain as its fallback.");
                var candidates = new ModelCandidateBuilder(brain.Catalog, brain.Counters,
                    PlacementRandom(sessionSeed, side, generation), selected.mode == "CARD", descriptions);
                return new ModelDecisionSource(fallback, candidates,
                    selected.mode == "STANCE" ? ModelDecisionPolicy.Stance : ModelDecisionPolicy.Cast,
                    ModelDecisionSource.RuleIdFor(persona.BrainType), selected.minIntervalTicks);
            };
        }
        // 원본의 ThreadLocalRandom은 lockstep에서 쓸 수 없다. 확정 초기 snapshot의 randomSeed와 side·generation만으로 배치 seed를 정한다.
        public const long PlacementSalt = 0x5D1CEB00D5EEDL;
        public static long PlacementSeed(long sessionSeed, Master side, long generation) => unchecked(
            (sessionSeed * 0x2545F4914F6CDD1DL) ^ PlacementSalt
            ^ (generation * unchecked((long)0x9E3779B97F4A7C15UL))
            ^ (side == Master.LeftPlayer ? 0L : 0x4F1BBCDCL));
        // 배치마다 새 generator를 넘기는 원본 supplier 계약을 지킨다. gameplay RNG와 규칙 brain RNG는 공유하지 않으므로 소비 순서가 바뀌지 않는다.
        public static Func<JavaRandom> PlacementRandom(long sessionSeed, Master side, long generation) {
            long seed = PlacementSeed(sessionSeed, side, generation);
            return () => new JavaRandom(seed);
        }
        // cardMenu는 원본 서버 속성 jev_card에 해당한다. 그 속성 연결 전까지는 두 정책 모두 완전한 play 메뉴를 받는다.
        // descriptions를 주지 않으면 묶인 카드 설명문을 쓴다.
        public static BotDecisionSourceFactory Confirmed(long sessionSeed,
            uint minRequestIntervalTicks = ModelDecisionSource.DefaultMinRequestIntervalTicks,
            MagicDescriptions descriptions = null, bool cardMenu = false) {
            return (side, generation, persona, fallback) => {
                if (!BotPersona.UsesLlm(persona.BrainType)) return fallback;
                var brain = (fallback as RuleDecisionSource)?.Brain
                    ?? throw new InvalidOperationException("A model bot needs the rule brain as its fallback.");
                var candidates = new ModelCandidateBuilder(brain.Catalog, brain.Counters,
                    PlacementRandom(sessionSeed, side, generation), cardMenu, descriptions);
                return new ModelDecisionSource(fallback, candidates,
                    ModelDecisionSource.PolicyFor(persona.BrainType), ModelDecisionSource.RuleIdFor(persona.BrainType), minRequestIntervalTicks);
            };
        }
    }
}
