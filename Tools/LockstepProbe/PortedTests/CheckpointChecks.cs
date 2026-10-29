using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class CheckpointChecks {
        public static BattleSessionRuntime Create() {
            var initial = InitialStateChecks.Example();
            var start = new SessionStart { sessionId = "checkpoint", participants = new long[] { 7, 9 }, tickRate = 20 };
            return initial.CreateRuntime(start);
        }
        private static string Trace(IEnumerable<string> states) {
            using (var hash = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", states)))).Replace("-", "").ToLowerInvariant();
        }
        private static void Changed(Action<BattleSessionRuntime> change, string label) {
            var runtime = Create(); var before = SimulationCheckpoint.Hash(runtime); change(runtime);
            if (before == SimulationCheckpoint.Hash(runtime)) throw new InvalidOperationException("Uncaptured checkpoint mutation: " + label);
        }
        public static void Run() {
            var first = Create(); var second = Create();
            Assert.Equal(SimulationCheckpoint.Hash(first), SimulationCheckpoint.Hash(second));
            var parameterPairs = second.Context.GameParameters.ToArray();
            second.Context.GameParameters.Clear();
            foreach (var pair in parameterPairs.Reverse()) second.Context.GameParameters.Add(pair.Key, pair.Value);
            Assert.Equal(SimulationCheckpoint.Hash(first), SimulationCheckpoint.Hash(second));
            using (var presentation = new BattleSessionOutput(7, 9, (_, __) => { }, () => true)) {
                var shown = InitialStateChecks.Example().CreateRuntime(new SessionStart { sessionId = "checkpoint", participants = new long[] { 7, 9 }, tickRate = 20 }, output: presentation);
                Assert.Equal(SimulationCheckpoint.Hash(first), SimulationCheckpoint.Hash(shown));
                first.AdvanceConfirmedFrame(1); shown.AdvanceConfirmedFrame(1);
                Assert.Equal(SimulationCheckpoint.Hash(first), SimulationCheckpoint.Hash(shown));
            }
            first = Create(); second = Create();
            var left = new LockstepBattleDriver(first) { CaptureCheckpoints = true };
            var right = new LockstepBattleDriver(second) { CaptureCheckpoints = true };
            // 외부 통계 구독은 두 replica의 규칙 상태를 바꾸지 않는다.
            int displayed = 0; first.Context.MagicUsed += (_, __, ___) => displayed++;
            string rolling = ""; var basicTrace = new List<string>();
            for (uint tick = 0; tick < 40; tick++) {
                var frame = new ConfirmedFrame { sessionId = "checkpoint", tick = tick, inputs = new[] { Array.Empty<CastCommand>(), Array.Empty<CastCommand>() } };
                left.Receive(frame); right.Receive(frame);
                Assert.Equal(left.LastCheckpoint.hash, right.LastCheckpoint.hash);
                rolling = left.LastCheckpoint.hash; basicTrace.Add(rolling);
            }
            Assert.True(first.Loop.StopRequested);
#if !UNITY_WEBGL || UNITY_EDITOR
            var dump = Environment.GetEnvironmentVariable("AC_LOCKSTEP_CHECKPOINT_DUMP");
            if (!string.IsNullOrEmpty(dump)) System.IO.File.WriteAllBytes(dump + "-basic.bin", SimulationCheckpoint.Bytes(first));
#endif
            Changed(runtime => runtime.Context.Random.NextInt(), "RNG state");
            Changed(runtime => runtime.Context.Random.NextGaussian(), "Gaussian spare");
            Changed(runtime => runtime.Context.Players[Master.LeftPlayer].Mana++, "mana");
            Changed(runtime => runtime.Context.Players[Master.LeftPlayer].AddCard(1), "cards");
            Changed(runtime => typeof(SimulationContext).GetField("nextObjectId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(runtime.Context, 999), "next object ID");
            Changed(runtime => typeof(SimulationContext).GetField("nextComponentId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(runtime.Context, 999L), "next component ID");
            Changed(runtime => runtime.Context.FrameNumber++, "frame clock");
            Changed(runtime => new BotAction().UseCard(runtime.Context.ActionSender(), new BotInputRequest(77, 1, new Vector3(3, 0, 5)), Master.LeftPlayer), "pending action payload");
            var complex = new[] { Create(), Create() };
            foreach (var runtime in complex) {
                runtime.AdvanceConfirmedFrame(1);
                runtime.BotLifecycle.ActivateForUser(7);
                var owner = runtime.Context.FindPlayer(Master.RightPlayer);
                owner.AddComponent(new BurnStatusEffect(owner, (Fix64)2, StatusEffectKey.BurnReceive));
                owner.AddComponent(new DotStatusEffect(owner, (Fix64)2, 8, Element.Fire, StatusEffectKey.DotDealBurn));
                owner.AddComponent(new SlowStatusEffect(owner, (Fix64)2, (Fix64)0.2m, StatusEffectKey.SnaredReceive));
                owner.FlushComponents();
                owner.GetComponent<MobComponent>().OnDamagedWithDelay(new Hit(2, 0, Element.None), (Fix64)0.6m);
                new BotAction().UseCard(runtime.Context.ActionSender(), new BotInputRequest(77, 1, new Vector3(3, 0, 5)), Master.LeftPlayer);
                var scheduler = new TickScheduler(); scheduler.Schedule(40, owner.Id, 3, 17); runtime.Context.InsertService(scheduler);
            }
            var complexTrace = new List<string>();
            for (int frame = 2; frame <= 30; frame++) {
                foreach (var runtime in complex) runtime.AdvanceConfirmedFrame(frame);
                Assert.Equal(SimulationCheckpoint.Hash(complex[0]), SimulationCheckpoint.Hash(complex[1]));
                complexTrace.Add(SimulationCheckpoint.Hash(complex[0]));
            }
#if !UNITY_WEBGL || UNITY_EDITOR
            if (!string.IsNullOrEmpty(dump)) System.IO.File.WriteAllBytes(dump + "-complex.bin", SimulationCheckpoint.Bytes(complex[0]));
#endif
            Console.WriteLine("CHECKPOINT COMPLEX ticks=29 traceSha256=" + Trace(complexTrace) + " lastState=" + SimulationCheckpoint.Hash(complex[0]));
            var schema = SimulationCheckpoint.FieldManifest();
            Assert.True(schema.Any(field => field.Contains("nextComponentId")));
            Assert.True(schema.Any(field => field.Contains("GaussianSpare")));
            Console.WriteLine("CHECKPOINT PASS ticks=40 traceSha256=" + Trace(basicTrace) + " lastState=" + rolling + " fieldEntries=" + schema.Length);
        }
    }
}
