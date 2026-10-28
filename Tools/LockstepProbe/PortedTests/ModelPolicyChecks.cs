using System;
using System.IO;
using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class ModelPolicyChecks {
        private static BattleInitialState Snapshot(string mode = "STANCE", bool fallback = false) {
            var snapshot = InitialStateChecks.Example();
            snapshot.profiles[0].userId = -7;
            snapshot.profiles[0].persona.brainType = "Jev";
            snapshot.modelPolicies = new[] { new BattleModelPolicySnapshot {
                slot = 0, brainType = "JEV", mode = mode, minIntervalTicks = 7, terminalTicks = 82, fallbackToRule = fallback
            } };
            return snapshot;
        }
        private static SessionStart Start(string version = LockstepVersion.SupportedModel) => new SessionStart {
            sessionId = "frozen-policy", participants = new long[] { -7, 9 }, tickRate = 20, simulationVersion = version
        };
        private static IBotDecisionSource Source(BattleInitialState snapshot, SessionStart start = null,
            BotDecisionSourceFactory factory = null) {
            var runtime = snapshot.CreateRuntime(start ?? Start(), decisionFactory: factory);
            runtime.BotLifecycle.ActivateForUser(-7);
            return runtime.Bots.Left.DecisionSource;
        }
        private static void Reject(Action<BattleInitialState> invalidate) {
            var snapshot = Snapshot(); invalidate(snapshot);
            bool rejected = false;
            try { snapshot.CreateRuntime(Start()); } catch (InvalidDataException) { rejected = true; }
            Assert.True(rejected);
        }
        public static void Run() {
            foreach (string mode in new[] { "PLAY", "STANCE", "CARD" }) {
                var source = (ModelDecisionSource)Source(Snapshot(mode));
                Assert.Equal(mode == "STANCE" ? ModelDecisionPolicy.Stance : ModelDecisionPolicy.Cast, source.Policy);
                Assert.Equal(7U, source.MinRequestIntervalTicks);
            }
            Assert.True(Source(Snapshot(fallback: true)) is RuleDecisionSource);
            var v1 = Snapshot(); v1.modelPolicies = null;
            bool v1Rejected = false;
            try { Source(v1, Start(LockstepVersion.Supported)); } catch (InvalidOperationException) { v1Rejected = true; }
            Assert.True(v1Rejected);
            v1.profiles[0].persona.brainType = "Rule";
            Assert.True(Source(v1, Start(LockstepVersion.Supported)) is RuleDecisionSource);
            Assert.True(Source(Snapshot(), factory: (_, _, _, fallback) => fallback) is RuleDecisionSource);
            Reject(s => s.modelPolicies = null);
            Reject(s => s.modelPolicies[0].slot = 1);
            Reject(s => s.modelPolicies[0].brainType = "OPENROUTER");
            Reject(s => s.modelPolicies[0].mode = "unknown");
            Reject(s => s.modelPolicies[0].terminalTicks = 0);
            Reject(s => s.modelPolicies = new[] { s.modelPolicies[0], s.modelPolicies[0] });
            Reject(s => s.profiles[0].persona.brainType = "Rule");
            Reject(s => { s.profiles[0].persona.brainType = "Openrouter"; s.modelPolicies[0].brainType = "OPENROUTER"; });
            var openrouter = Snapshot("PLAY"); openrouter.profiles[0].persona.brainType = "Openrouter";
            openrouter.modelPolicies[0].brainType = "OPENROUTER";
            Assert.Equal(ModelDecisionPolicy.Cast, ((ModelDecisionSource)Source(openrouter)).Policy);
            var frozen = Snapshot("PLAY");
            var runtime = frozen.CreateRuntime(Start());
            frozen.modelPolicies[0].mode = "STANCE"; frozen.modelPolicies[0].minIntervalTicks = 99;
            runtime.BotLifecycle.ActivateForUser(-7);
            Assert.Equal(ModelDecisionPolicy.Cast, ((ModelDecisionSource)runtime.Bots.Left.DecisionSource).Policy);
            Assert.Equal(7U, ((ModelDecisionSource)runtime.Bots.Left.DecisionSource).MinRequestIntervalTicks);
            Console.WriteLine("MODEL POLICY PASS modes=3 interval=7 fallback profile-validation v1 explicit-factory frozen-copy");
        }
    }
}
