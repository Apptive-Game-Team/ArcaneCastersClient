using System;
using ArcaneCasters.Simulation;
using Global.Serialization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace WordOnline.Tests {
    public class LockstepPresentationTests {
        [Test] public void LockstepSelectionCommandsRoundTripWithoutLosingKind() {
            var original = new ConfirmedFrame { sessionId = "match", tick = 0, inputs = new[] {
                new[] { new CastCommand { kind = "selectCard", magicId = 7 }, new CastCommand { kind = "unselectCard", magicId = 7 } },
                new[] { new CastCommand { magicId = 8, xRaw = 4294967297L, zRaw = 2 } }
            } };
            var decoded = JsonCodec.Deserialize<ConfirmedFrame>(JsonCodec.Serialize(original));
            var stream = new ConfirmedInputStream("match"); stream.Enqueue(decoded); Assert.IsTrue(stream.TryTake(out var frame));
            Assert.AreEqual("selectCard", frame.inputs[0][0].kind); Assert.AreEqual("unselectCard", frame.inputs[0][1].kind);
            Assert.AreEqual("cast", frame.inputs[1][0].kind); Assert.AreEqual(4294967297L, frame.inputs[1][0].xRaw);
            var legacy = JsonCodec.Deserialize<CastCommand>("{\"magicId\":7,\"xRaw\":0,\"zRaw\":0}"); Assert.AreEqual("cast", legacy.kind);
            var invalid = JsonCodec.Deserialize<ConfirmedFrame>(JsonCodec.Serialize(original)); invalid.inputs[0][0].kind = null;
            Assert.Throws<System.IO.InvalidDataException>(() => new ConfirmedInputStream("match").Enqueue(invalid));
        }
        [Test] public void MapsInputResultsAndPveMessagesToExistingHandlers() {
            var mapper = Type.GetType("GameScene.LockstepPresentationMapper, Assembly-CSharp", true);
            var codes = new[] { "SUCCESS", "FAIL_INSUFFICIENT_MANA", "FAIL_LACK_OF_CARD", "FAIL_INVALID_PLACE", "FAIL_INVALID_MAGIC", "FAIL_GAME_ENDED" };
            foreach (InputResultCode code in Enum.GetValues(typeof(InputResultCode))) {
                var response = new InputResponse(code == InputResultCode.Success, code, "message", 12, 735, 42);
                var mapped = mapper.GetMethod("Input").Invoke(null, new object[] { response });
                var dto = (GameScene.Dto.MagicValidInfo)JsonCodec.Deserialize<GameScene.Dto.ServerMessage>(JsonCodec.Serialize(mapped));
                Assert.AreEqual(codes[(int)code], dto.resultCode); Assert.AreEqual(735, dto.id); Assert.AreEqual(42, dto.magicId); Assert.AreEqual(12, dto.updatedMana);
                Assert.AreEqual(code == InputResultCode.Success, dto.valid);
            }
            var objective = mapper.GetMethod("Objective").Invoke(null, new object[] { new PveObjectiveState(PveWinCondition.Survive, 60, 17, 0, 0) });
            var goal = (GameScene.Dto.PveObjectiveInfo)JsonCodec.Deserialize<GameScene.Dto.ServerMessage>(JsonCodec.Serialize(objective));
            Assert.AreEqual("Survive", goal.winCondition); Assert.AreEqual(60, goal.surviveSeconds); Assert.AreEqual(17, goal.remainingSeconds);
            var script = mapper.GetMethod("Script").Invoke(null, new object[] { new PveScriptEvent("intro", 8, new[] { "line" }, 3) });
            var dialogue = (GameScene.Dto.PveScriptEventInfo)JsonCodec.Deserialize<GameScene.Dto.ServerMessage>(JsonCodec.Serialize(script));
            Assert.AreEqual("intro", dialogue.key); Assert.AreEqual(8, dialogue.speakerObjectId); Assert.AreEqual(3, dialogue.seq); Assert.AreEqual("line", dialogue.lines[0]);
            var state = mapper.GetMethod("State").Invoke(null, new object[] { new PveChannelState("bgm", null, 4) });
            var bgm = (GameScene.Dto.PveStateInfo)JsonCodec.Deserialize<GameScene.Dto.ServerMessage>(JsonCodec.Serialize(state));
            Assert.AreEqual("bgm", bgm.channel); Assert.IsNull(bgm.value); Assert.AreEqual(4, bgm.seq);
        }
        [Test] public void FramePreservesProjectileTargetsAndEventDiscriminators() {
            var owner = new SimObject(new SimulationContext(), Master.LeftPlayer, "FireShot", BEPUutilities.Vector3.Zero);
            var snapshot = new ObjectSnapshot(owner);
            var frame = new BattlePresentationFrame {
                Frame = 2, TickRate = 20, Mana = 9, RemainingTime = 179, LeftHp = 10, RightHp = 8, Cards = new long[] { 5 },
                Created = new[] { snapshot }, Updated = new[] { snapshot }, Objects = new[] { snapshot },
                Events = new[] {
                    new BattleEvent("hit", 4, 5), new BattleEvent("shock", 6, 0),
                    new BattleEvent("Beam", 4, new BEPUutilities.Vector3(1, 2, 3), (FixMath.NET.Fix64)0.25m, 2),
                    new BattleEvent("Absorb", BEPUutilities.Vector3.Zero, 5, (FixMath.NET.Fix64)0.5m)
                }
            };
            var mapper = Type.GetType("GameScene.LockstepPresentationMapper, Assembly-CSharp", true).GetMethod("Frame");
            var delta = (GameScene.Dto.FrameInfoDto)JsonCodec.Deserialize<GameScene.Dto.ServerMessage>(JsonCodec.Serialize(mapper.Invoke(null, new object[] { frame })));
            Assert.AreEqual(9, delta.updatedMana); Assert.AreEqual(5, delta.cards.added[0]); Assert.AreEqual(1, delta.objects.create.Length); Assert.AreEqual(1, delta.objects.update.Length);
            Assert.AreEqual(5, ((GameScene.Dto.Event.HitEvent)delta.events[0]).targetId); Assert.AreEqual(6, ((GameScene.Dto.Event.ShockEvent)delta.events[1]).actorId);
            var beam = delta.objects.projectile[0]; Assert.AreEqual(2, beam.width); Assert.AreEqual(0.25f, beam.duration);
            Assert.AreEqual(4, ((GameScene.Dto.Projectile.ReferenceProjectileTarget)beam.start).id); Assert.AreEqual(3, ((GameScene.Dto.Projectile.PositionProjectileTarget)beam.end).z);
            Assert.IsInstanceOf<GameScene.Dto.Projectile.PositionProjectileTarget>(delta.objects.projectile[1].start);
            Assert.AreEqual(5, ((GameScene.Dto.Projectile.ReferenceProjectileTarget)delta.objects.projectile[1].end).id);
            frame.IsSync = true;
            var sync = (GameScene.Dto.SyncFrameInfo)JsonCodec.Deserialize<GameScene.Dto.ServerMessage>(JsonCodec.Serialize(mapper.Invoke(null, new object[] { frame })));
            Assert.AreEqual(20, sync.tickRate); Assert.AreEqual(2, sync.snapshotResponseDto.frame); Assert.AreEqual(5, sync.snapshotResponseDto.myCards[0]);
            Assert.AreEqual(2, sync.projectileDtos.Count); Assert.AreEqual(2, sync.events.Count); Assert.AreEqual(owner.Id, sync.snapshotResponseDto.objects[0].id);
        }
        [Test] public void OutputDefersChangesAfterBeginAndHidesSpectatorHand() {
            var context = new SimulationContext(); context.RegisterPrefab("FireShot", _ => { });
            context.Players[Master.LeftPlayer] = new PlayerData(new ManaCharger(100)) { Mana = 15 };
            context.Players[Master.LeftPlayer].AddCard(7);
            var economy = new BattleEconomySystem(); economy.Update(context);
            BattlePresentationFrame received = null;
            using (var output = new BattlePresentationOutput(null, frame => received = frame)) {
                output.Initialize(context, economy, 180000); context.FrameNumber = 1;
                output.BeginFrame(context);
                var owner = context.Spawn(Master.LeftPlayer, "FireShot", BEPUutilities.Vector3.Zero);
                context.AddEvent(new BattleEvent("hit", owner.Id, 9)); context.FlushObjects();
                output.BuildSnapshot(context); output.EndFrame(context);
                Assert.IsTrue(received.IsSync); Assert.AreEqual(1, received.Objects.Length); Assert.IsEmpty(received.Created); Assert.IsEmpty(received.Events);
                Assert.AreEqual(0, received.Mana); Assert.IsEmpty(received.Cards);
                context.FrameNumber = 2; output.BeginFrame(context); output.EndFrame(context);
                Assert.IsFalse(received.IsSync); Assert.AreEqual(1, received.Created.Length); Assert.AreEqual(1, received.Events.Length);
                context.FrameNumber = 3; output.BeginFrame(context); output.EndFrame(context);
                Assert.IsEmpty(received.Created); Assert.IsEmpty(received.Updated); Assert.IsEmpty(received.Events);
            }
        }
        [Test] public void RepeatedUpdateRetainsGaugeRemovedDuringSameFrameOnly() {
            var context = new SimulationContext(); BattlePresentationFrame received = null;
            using (var output = new BattlePresentationOutput(null, frame => received = frame)) {
                output.Initialize(context, new BattleEconomySystem(), 180000);
                var owner = new SimObject(context, Master.LeftPlayer, "FireShot", BEPUutilities.Vector3.Zero);
                var bar = new Bar(owner, GaugeCategory.Hp); owner.AddComponentImmediately(bar); owner.ApplyUpdate();
                owner.RemoveComponentImmediately(bar); owner.ApplyUpdate(); output.BeginFrame(context); output.EndFrame(context);
                Assert.AreEqual(1, received.Updated[0].Gauges.Count); Assert.AreEqual(GaugeCategory.Hp, received.Updated[0].Gauges[0].Category);
                owner.ApplyUpdate(); output.BeginFrame(context); output.EndFrame(context);
                Assert.IsEmpty(received.Updated[0].Gauges);
            }
        }
        private sealed class Bar : SimComponent, IGaugeComponent {
            private readonly GaugeCategory category;
            public Bar(SimObject owner, GaugeCategory category) : base(owner) { this.category = category; }
            public ArcaneCasters.Simulation.Gauge GetGauge() => new ArcaneCasters.Simulation.Gauge(2, 10, category);
        }
        [Test] public void MapsFixedSnapshotToExistingSceneContract() {
            var owner = new SimObject(new SimulationContext(7), Master.RightPlayer, "FireShot", new BEPUutilities.Vector3(1, 2, 3));
            owner.SetStatus(ObjectStatus.Move); owner.AddEffect(VisualEffect.Burn); owner.DrawCircle(BEPUutilities.Vector3.Zero, 2, GizmoCategory.Collider);
            owner.AddComponentImmediately(new Bar(owner, GaugeCategory.Hp)); owner.AddComponentImmediately(new Bar(owner, GaugeCategory.Ttl));
            var snapshot = new ObjectSnapshot(owner); owner.SetPositionInPlace(BEPUutilities.Vector3.Zero);
            var mapper = Type.GetType("GameScene.LockstepPresentationMapper, Assembly-CSharp", true);
            var mapped = mapper.GetMethod("Object").Invoke(null, new object[] { snapshot });
            var dto = JObject.Parse(JsonCodec.Serialize(mapped));
            Assert.AreEqual(owner.Id, dto.Value<int>("id")); Assert.AreEqual("RightPlayer", dto.Value<string>("master")); Assert.AreEqual("Move", dto.Value<string>("status"));
            Assert.AreEqual(1f, dto.Value<float>("x")); Assert.AreEqual(2f, dto.Value<float>("y")); Assert.AreEqual(3f, dto.Value<float>("z"));
            Assert.AreEqual("Burn", (string)dto["effects"][0]); Assert.AreEqual("Circle", (string)dto["gizmos"][0]["type"]);
            Assert.AreEqual("HP", (string)dto["gauges"][0]["category"]); Assert.AreEqual("TTL", (string)dto["gauges"][1]["category"]);
            var update = new GameScene.Dto.UpdatedObjectDto((GameScene.Dto.SnapshotObjectDto)mapped);
            Assert.AreEqual("RightPlayer", update.master);
        }
    }
}
