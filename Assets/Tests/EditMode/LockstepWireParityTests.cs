using System;
using ArcaneCasters.Simulation;
using GameScene.Dto;
using Global.Serialization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using FixMath.NET;
using Vector3 = BEPUutilities.Vector3;

namespace WordOnline.Tests {
    public class LockstepWireParityTests {
        [Property("OriginalSimulationTest", "game/dto/tests.rs::pve_script_event_has_no_type")]
        [Test] public void PveScriptEventHasNoType() {
            const string json = "{\"key\":\"intro\",\"speakerObjectId\":2,\"lines\":[\"Hello\"],\"seq\":1}";
            var payload = JsonCodec.Deserialize<PveScriptEventInfo>(json);
            Assert.IsNull(payload.type);
            Assert.AreEqual("intro", payload.key);
            Assert.AreEqual(2, payload.speakerObjectId);
            Assert.AreEqual(new[] { "Hello" }, payload.lines);
            Assert.AreEqual(1, payload.seq);
            Wire<PveScriptEventInfo>(payload, json);

            // 로컬 이벤트는 화면 분배에 쓰는 type만 추가하고 원본 대사와 순번을 유지한다.
            var mapped = (PveScriptEventInfo)Map("Script", new PveScriptEvent("intro", 2, new[] { "Hello" }, 1));
            Wire(mapped, "{\"type\":\"pveScriptEvent\",\"key\":\"intro\",\"speakerObjectId\":2,\"lines\":[\"Hello\"],\"seq\":1}");
        }
        [Property("OriginalSimulationTest", "game/dto/tests.rs::emote")]
        [Test] public void Emote() {
            var mapper = Type.GetType("GameScene.LockstepPresentationMapper, Assembly-CSharp", true);
            var dto = mapper.GetMethod("Emote").Invoke(null, new object[] { Master.LeftPlayer, ArcaneCasters.Simulation.Emote.Surprised });
            Wire(dto, "{\"type\":\"emote\",\"side\":\"LeftPlayer\",\"emote\":\"Surprised\"}");
            Assert.AreEqual(ArcaneCasters.Simulation.Emote.Taunt, SessionEmotes.Parse("Taunt"));
            Assert.IsNull(SessionEmotes.Parse("taunt"));
        }
        [Property("OriginalSimulationTest", "game/dto/tests.rs::bot_thought")]
        [Test] public void BotThought() {
            var thought = new BotThought(Master.RightPlayer, new InputDecision(34, new Vector3(5, 0, 6), "combo.mob-cluster", "Explosion targets three mobs."));
            Wire(Map("Thought", thought), "{\"type\":\"botThought\",\"botSide\":\"RightPlayer\",\"ruleId\":\"combo.mob-cluster\",\"reason\":\"Explosion targets three mobs.\",\"cards\":[34],\"target\":{\"x\":5.0,\"y\":0.0,\"z\":6.0}}");
        }
        private static object Map(string method, object value) => Type.GetType("GameScene.LockstepPresentationMapper, Assembly-CSharp", true).GetMethod(method).Invoke(null, new[] { value });
        private static void Wire(object value, string expected) {
            Wire<ServerMessage>(value, expected);
        }
        private static void Wire<T>(object value, string expected) {
            var json = JsonCodec.Serialize(value);
            Assert.IsTrue(JToken.DeepEquals(JToken.Parse(expected), JToken.Parse(json)), json);
            var restored = JsonCodec.Deserialize<T>(expected);
            Assert.IsTrue(JToken.DeepEquals(JToken.Parse(expected), JToken.Parse(JsonCodec.Serialize(restored))));
        }
        private const string ObjectsJson = "{\"create\":[{\"id\":3,\"type\":\"FireShot\",\"position\":{\"x\":5.0,\"y\":0.0,\"z\":6.0},\"master\":\"RightPlayer\",\"gizmos\":[{\"relativePosition\":{\"x\":0.0,\"y\":0.0,\"z\":0.0},\"radius\":0.5,\"boxSize\":null,\"type\":\"Circle\",\"category\":\"Collider\"}]}],\"update\":[{\"id\":4,\"status\":\"Move\",\"effects\":[\"Burn\"],\"master\":\"LeftPlayer\",\"position\":{\"x\":1.5,\"y\":0.0,\"z\":-2.25},\"gauges\":[{\"value\":7.5,\"maxValue\":10.0,\"category\":\"HP\"}]}],\"projectile\":[{\"type\":\"Beam\",\"start\":{\"targetType\":\"reference\",\"id\":2},\"end\":{\"targetType\":\"position\",\"x\":1.0,\"y\":0.0,\"z\":2.0},\"duration\":0.25,\"width\":0.0}]}";
        private static BattlePresentationFrame EmptyFrame() => new BattlePresentationFrame {
            Cards = Array.Empty<long>(), Created = Array.Empty<ObjectSnapshot>(), Updated = Array.Empty<ObjectSnapshot>(),
            Objects = Array.Empty<ObjectSnapshot>(), Events = Array.Empty<BattleEvent>()
        };
        private static BattlePresentationFrame ExampleFrame() {
            var context = new SimulationContext();
            new SimObject(context, Master.None, "unused", Vector3.Zero);
            new SimObject(context, Master.None, "unused", Vector3.Zero);
            var created = new SimObject(context, Master.RightPlayer, "FireShot", new Vector3(5,0,6));
            created.DrawCircle(Vector3.Zero, (Fix64)0.5m, GizmoCategory.Collider);
            var updated = new SimObject(context, Master.LeftPlayer, "FireShot", new Vector3((Fix64)1.5m,0,(Fix64)(-2.25m)));
            updated.SetStatus(ObjectStatus.Move); updated.AddEffect(VisualEffect.Burn);
            var frame = EmptyFrame();
            frame.RemainingTime = 30; frame.Mana = 5; frame.LeftHp = 100; frame.RightHp = 80; frame.Cards = new long[] { 34 };
            frame.Created = new[] { new ObjectSnapshot(created) };
            frame.Updated = new[] { new ObjectSnapshot(updated, new[] { new ArcaneCasters.Simulation.Gauge((Fix64)7.5m,10,GaugeCategory.Hp) }) };
            frame.Events = new[] { new BattleEvent("Beam",2,new Vector3(1,0,2),(Fix64)0.25m,0), new BattleEvent("hit",7,12) };
            return frame;
        }
        [Property("OriginalSimulationTest", "game/dto/tests.rs::objects_info")]
        [Test] public void ObjectsInfo() {
            Wire<ObjectsInfo>(((FrameInfoDto)Map("Frame", ExampleFrame())).objects, ObjectsJson);
        }
        [Property("OriginalSimulationTest", "game/dto/tests.rs::empty_objects_info")]
        [Test] public void EmptyObjectsInfo() {
            Wire<ObjectsInfo>(((FrameInfoDto)Map("Frame", EmptyFrame())).objects, "{\"create\":[],\"update\":[],\"projectile\":[]}");
        }
        [Property("OriginalSimulationTest", "game/dto/tests.rs::frame")]
        [Test] public void Frame() {
            Wire(Map("Frame", ExampleFrame()), "{\"type\":\"frame\",\"remainingTime\":30,\"updatedMana\":5,\"leftPlayerHp\":100,\"rightPlayerHp\":80,\"cards\":{\"added\":[34]},\"objects\":" + ObjectsJson + ",\"events\":[{\"type\":\"hit\",\"actorId\":7,\"targetId\":12}]}");
        }
        [Property("OriginalSimulationTest", "game/dto/tests.rs::broadcast_frame_has_no_cards_and_no_mana")]
        [Test] public void BroadcastFrame() {
            var frame = EmptyFrame(); frame.RemainingTime = 9; frame.LeftHp = 1; frame.RightHp = 2;
            frame.Events = new[] { new BattleEvent("shock",3,0) };
            Wire(Map("Frame",frame), "{\"type\":\"frame\",\"remainingTime\":9,\"updatedMana\":0,\"leftPlayerHp\":1,\"rightPlayerHp\":2,\"cards\":{\"added\":[]},\"objects\":{\"create\":[],\"update\":[],\"projectile\":[]},\"events\":[{\"type\":\"shock\",\"actorId\":3,\"targetId\":0}]}");
        }
        [Property("OriginalSimulationTest", "game/dto/tests.rs::box_gizmo_keeps_box_size")]
        [Test] public void BoxGizmoKeepsBoxSize() {
            var owner = new SimObject(new SimulationContext(),Master.None,"unused",Vector3.Zero);
            owner.DrawBox(new Vector3(0,0,1),new Vector3(2,1,2),GizmoCategory.SpawnArea);
            var dto = (SnapshotObjectDto)Map("Object",new ObjectSnapshot(owner));
            Wire<GameScene.Dto.debug.Gizmo>(dto.gizmos[0], "{\"relativePosition\":{\"x\":0.0,\"y\":0.0,\"z\":1.0},\"radius\":0.0,\"boxSize\":{\"x\":2.0,\"y\":1.0,\"z\":2.0},\"type\":\"Box\",\"category\":\"SpawnArea\"}");
        }
        [Property("OriginalSimulationTest", "game/dto/tests.rs::sync")]
        [Test] public void Sync() {
            var owner = new SimObject(new SimulationContext(),Master.None,"FireShot",new Vector3(1,0,2));
            owner.SetStatus(ObjectStatus.Idle);
            var frame = ExampleFrame(); frame.IsSync = true; frame.Frame = 10; frame.TickRate = 60; frame.Mana = 0; frame.Cards = new long[] { 34,35 };
            frame.Objects = new[] { new ObjectSnapshot(owner, new[] { new ArcaneCasters.Simulation.Gauge(1,2,GaugeCategory.Ttl) }) };
            Wire(Map("Frame",frame), "{\"type\":\"sync\",\"remainingTime\":30,\"updatedMana\":0,\"leftPlayerHp\":100,\"rightPlayerHp\":80,\"snapshotResponseDto\":{\"frame\":10,\"objects\":[{\"id\":1,\"prefab\":\"FireShot\",\"x\":1.0,\"y\":0.0,\"z\":2.0,\"master\":\"None\",\"status\":\"Idle\",\"effects\":[],\"gizmos\":[],\"gauges\":[{\"value\":1.0,\"maxValue\":2.0,\"category\":\"TTL\"}]}],\"myCards\":[34,35]},\"projectileDtos\":[{\"type\":\"Beam\",\"start\":{\"targetType\":\"reference\",\"id\":2},\"end\":{\"targetType\":\"position\",\"x\":1.0,\"y\":0.0,\"z\":2.0},\"duration\":0.25,\"width\":0.0}],\"events\":[{\"type\":\"hit\",\"actorId\":7,\"targetId\":12}],\"tickRate\":60}");
        }
        [Property("OriginalSimulationTest", "game/dto/tests.rs::result")]
        [Test] public void Result() {
            var result = new BattleResultMessage(Master.RightPlayer, new BattleResultMmr(1000,990,1010,980));
            Wire(Map("Result", result), "{\"type\":\"result\",\"leftPlayer\":\"Win\",\"rightPlayer\":\"Lose\",\"lastLeftPlayerMmr\":1000,\"lastRightPlayerMmr\":990,\"newLeftPlayerMmr\":1010,\"newRightPlayerMmr\":980}");
        }
        [Property("OriginalSimulationTest", "game/dto/tests.rs::pve_objective")]
        [Test] public void PveObjective() {
            Wire(Map("Objective", new PveObjectiveState(PveWinCondition.Survive,120,90,0,0)), "{\"type\":\"pveObjective\",\"winCondition\":\"Survive\",\"surviveSeconds\":120,\"remainingSeconds\":90,\"objectivesTotal\":0,\"objectivesRemaining\":0}");
        }
        [Property("OriginalSimulationTest", "game/dto/tests.rs::magic_valid")]
        [Test] public void MagicValid() {
            Wire(Map("Input", new InputResponse(true, InputResultCode.Success, "magic use is valid",4,7,34)), "{\"type\":\"magicValid\",\"message\":\"magic use is valid\",\"valid\":true,\"resultCode\":\"SUCCESS\",\"updatedMana\":4,\"id\":7,\"magicId\":34}");
            Wire(Map("Input", new InputResponse(false, InputResultCode.FailInsufficientMana, "magic use is not valid",0,7,34)), "{\"type\":\"magicValid\",\"message\":\"magic use is not valid\",\"valid\":false,\"resultCode\":\"FAIL_INSUFFICIENT_MANA\",\"updatedMana\":0,\"id\":7,\"magicId\":34}");
        }
        [Property("OriginalSimulationTest", "game/dto/tests.rs::result_codes_spellings")]
        [Test] public void ResultCodesSpellings() {
            var codes = new[] { InputResultCode.FailLackOfCard, InputResultCode.FailInvalidPlace, InputResultCode.FailInvalidMagic, InputResultCode.FailGameEnded };
            var names = new[] { "FAIL_LACK_OF_CARD", "FAIL_INVALID_PLACE", "FAIL_INVALID_MAGIC", "FAIL_GAME_ENDED" };
            for (int i = 0; i < codes.Length; i++) {
                var dto = Map("Input", new InputResponse(false,codes[i],"",0,7));
                Assert.AreEqual(names[i], JObject.Parse(JsonCodec.Serialize(dto)).Value<string>("resultCode"));
            }
        }
    }
}
