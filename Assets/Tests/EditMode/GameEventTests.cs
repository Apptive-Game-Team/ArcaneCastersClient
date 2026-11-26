using GameScene.Dto;
using GameScene.Dto.Event;
using Global.Serialization;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class GameEventTests
    {
        [Test]
        public void FrameCarriesHitEvents()
        {
            const string json = @"{""type"":""frame"",""remainingTime"":50,""updatedMana"":2,
                ""leftPlayerHp"":100,""rightPlayerHp"":100,""cards"":{},""objects"":{},
                ""events"":[{""type"":""hit"",""actorId"":12,""targetId"":31}]}";

            FrameInfoDto frame = (FrameInfoDto)JsonCodec.Deserialize<ServerMessage>(json);

            Assert.AreEqual(1, frame.events.Count);
            Assert.IsInstanceOf<HitEvent>(frame.events[0]);
            HitEvent hit = (HitEvent)frame.events[0];
            Assert.AreEqual(12, hit.actorId);
            Assert.AreEqual(31, hit.targetId);
            Assert.AreEqual("hit", hit.type);
        }

        [Test]
        public void FrameCarriesShockEvents()
        {
            const string json = @"{""type"":""frame"",""remainingTime"":50,""updatedMana"":2,
                ""leftPlayerHp"":100,""rightPlayerHp"":100,""cards"":{},""objects"":{},
                ""events"":[{""type"":""shock"",""actorId"":7,""targetId"":0}]}";

            FrameInfoDto frame = (FrameInfoDto)JsonCodec.Deserialize<ServerMessage>(json);

            Assert.AreEqual(1, frame.events.Count);
            Assert.IsInstanceOf<ShockEvent>(frame.events[0]);
            ShockEvent shock = (ShockEvent)frame.events[0];
            Assert.AreEqual(7, shock.actorId);
            Assert.AreEqual("shock", shock.type);
        }

        /// <summary>
        /// 싱크 프레임이 10프레임마다 frame 메시지를 대체하므로 여기에도 이벤트가 실려야 한다.
        /// </summary>
        [Test]
        public void SyncFrameCarriesHitEvents()
        {
            const string json = @"{""type"":""sync"",""remainingTime"":50,""updatedMana"":2,
                ""leftPlayerHp"":100,""rightPlayerHp"":100,
                ""snapshotResponseDto"":{""frame"":100,""objects"":[],""myCards"":[]},
                ""projectileDtos"":[],
                ""events"":[{""type"":""hit"",""actorId"":1,""targetId"":2}]}";

            SyncFrameInfo sync = (SyncFrameInfo)JsonCodec.Deserialize<ServerMessage>(json);

            Assert.AreEqual(1, sync.events.Count);
            Assert.AreEqual(1, ((HitEvent)sync.events[0]).actorId);
        }

        [TestCase("vineWitchCast")]
        [TestCase("vineWitchFan")]
        public void FrameAndSnapshotCarryTheWitchAndHerCombatTarget(string eventType)
        {
            string events = "{\"events\":[{\"type\":\"" + eventType + "\",\"actorId\":3,\"targetId\":7}]}";
            FrameInfoDto frame = JsonCodec.Deserialize<FrameInfoDto>(events);
            SyncFrameInfo sync = JsonCodec.Deserialize<SyncFrameInfo>(events);

            foreach (var gameEvent in new[] { frame.events[0], sync.events[0] })
            {
                var cast = (VineWitchCastEvent)gameEvent;
                Assert.AreEqual(eventType, cast.type);
                Assert.AreEqual(3, cast.actorId);
                Assert.AreEqual(7, cast.targetId);
            }
        }

        /// <summary>
        /// 서버가 새 이벤트 종류를 먼저 배포해도 프레임 전체가 깨지면 안 된다.
        /// </summary>
        [Test]
        public void UnknownEventTypeBecomesNullWithoutBreakingTheFrame()
        {
            const string json = @"{""type"":""frame"",""remainingTime"":50,""updatedMana"":2,
                ""leftPlayerHp"":100,""rightPlayerHp"":100,""cards"":{},""objects"":{},
                ""events"":[{""type"":""stunned"",""actorId"":1,""targetId"":2},
                            {""type"":""hit"",""actorId"":3,""targetId"":4}]}";

            FrameInfoDto frame = (FrameInfoDto)JsonCodec.Deserialize<ServerMessage>(json);

            Assert.AreEqual(2, frame.events.Count);
            Assert.IsNull(frame.events[0]);
            Assert.AreEqual(3, ((HitEvent)frame.events[1]).actorId);
        }

        [Test]
        public void FrameWithoutEventsLeavesTheListNull()
        {
            const string json = @"{""type"":""frame"",""remainingTime"":50,""updatedMana"":2,
                ""leftPlayerHp"":100,""rightPlayerHp"":100,""cards"":{},""objects"":{}}";

            FrameInfoDto frame = (FrameInfoDto)JsonCodec.Deserialize<ServerMessage>(json);

            Assert.IsNull(frame.events);
        }
    }
}
