using GameScene.Dto;
using Global.Serialization;
using NUnit.Framework;

namespace WordOnline.Tests
{
    /// <summary>
    /// The server pushes every STOMP payload with a <c>type</c> discriminator; one pass has to land on
    /// the right message class.
    /// </summary>
    public class ServerMessageTests
    {
        [Test]
        public void FrameMessageResolvesToFrameInfoDto()
        {
            const string json = @"{""type"":""frame"",""remainingTime"":118,""updatedMana"":7,
                ""leftPlayerHp"":100,""rightPlayerHp"":92,""cards"":{""added"":[19]},
                ""objects"":{""create"":[],""update"":[],""projectile"":[]}}";

            ServerMessage message = JsonCodec.Deserialize<ServerMessage>(json);

            Assert.IsInstanceOf<FrameInfoDto>(message);
            FrameInfoDto frame = (FrameInfoDto)message;
            Assert.AreEqual("frame", frame.type);
            Assert.AreEqual(118, frame.remainingTime);
            Assert.AreEqual(7, frame.updatedMana);
            Assert.AreEqual(92, frame.rightPlayerHp);
            Assert.AreEqual(new long[] { 19 }, frame.cards.added);
        }

        [Test]
        public void SyncMessageResolvesToSyncFrameInfo()
        {
            const string json = @"{""type"":""sync"",""remainingTime"":90,""updatedMana"":3,
                ""leftPlayerHp"":80,""rightPlayerHp"":70,
                ""snapshotResponseDto"":{""frame"":180,""objects"":[],""myCards"":[20]},
                ""projectileDtos"":[]}";

            ServerMessage message = JsonCodec.Deserialize<ServerMessage>(json);

            Assert.IsInstanceOf<SyncFrameInfo>(message);
            SyncFrameInfo sync = (SyncFrameInfo)message;
            Assert.AreEqual(180, sync.snapshotResponseDto.frame);
            Assert.AreEqual(new long[] { 20 }, sync.snapshotResponseDto.myCards);
            Assert.IsEmpty(sync.projectileDtos);
        }

        [Test]
        public void MagicValidMessageResolvesToMagicValidInfo()
        {
            const string json = @"{""type"":""magicValid"",""valid"":false,""resultCode"":""NOT_ENOUGH_MANA"",
                ""message"":""mana"",""updatedMana"":2,""id"":11,""magicId"":90071992547409}";

            ServerMessage message = JsonCodec.Deserialize<ServerMessage>(json);

            Assert.IsInstanceOf<MagicValidInfo>(message);
            MagicValidInfo magicValid = (MagicValidInfo)message;
            Assert.IsFalse(magicValid.valid);
            Assert.AreEqual("NOT_ENOUGH_MANA", magicValid.resultCode);
            Assert.AreEqual(90071992547409L, magicValid.magicId);
        }

        [Test]
        public void ResultMessageResolvesToResultInfo()
        {
            const string json = @"{""type"":""result"",""leftPlayer"":""alice"",""rightPlayer"":""bob"",
                ""lastLeftPlayerMmr"":1200,""lastRightPlayerMmr"":1180,
                ""newLeftPlayerMmr"":1215,""newRightPlayerMmr"":1165}";

            ServerMessage message = JsonCodec.Deserialize<ServerMessage>(json);

            Assert.IsInstanceOf<ResultInfo>(message);
            ResultInfo result = (ResultInfo)message;
            Assert.AreEqual("alice", result.leftPlayer);
            Assert.AreEqual(1215, result.newLeftPlayerMmr);
        }

        [Test]
        public void BotThoughtMessageResolvesWithDecisionContract()
        {
            const string json = @"{""type"":""botThought"",""botSide"":""RightPlayer"",
                ""ruleId"":""combo.mob-cluster"",""reason"":""three mobs"",
                ""cards"" :[""Water"",""Explode""],""target"":{""x"":5.0,""y"":0.0,""z"":6.0}}";

            ServerMessage message = JsonCodec.Deserialize<ServerMessage>(json);

            Assert.IsInstanceOf<BotThoughtInfo>(message);
            BotThoughtInfo thought = (BotThoughtInfo)message;
            Assert.AreEqual("RightPlayer", thought.botSide);
            Assert.AreEqual("combo.mob-cluster", thought.ruleId);
            Assert.AreEqual("three mobs", thought.reason);
            Assert.AreEqual(new[] { "Water", "Explode" }, thought.cards);
            Assert.AreEqual(new UnityEngine.Vector3(5f, 0f, 6f), thought.target);
        }

        [TestCase("pveScript")]
        [TestCase("pveScriptEvent")]
        public void BothPveScriptTypesResolveToPveScriptEventInfo(string type)
        {
            string json = $@"{{""type"":""{type}"",""key"":""intro"",""speakerObjectId"":4,""lines"":[""hi""]}}";

            ServerMessage message = JsonCodec.Deserialize<ServerMessage>(json);

            Assert.IsInstanceOf<PveScriptEventInfo>(message);
            PveScriptEventInfo pveScript = (PveScriptEventInfo)message;
            Assert.AreEqual(4, pveScript.speakerObjectId);
            Assert.AreEqual(new[] { "hi" }, pveScript.lines);
        }

        [Test]
        public void UnknownTypeResolvesToNull()
        {
            const string json = @"{""type"":""somethingNew"",""value"":1}";

            Assert.IsNull(JsonCodec.Deserialize<ServerMessage>(json));
        }

        [Test]
        public void MissingTypeResolvesToNull()
        {
            const string json = @"{""key"":""intro"",""lines"":[""hi""]}";

            Assert.IsNull(JsonCodec.Deserialize<ServerMessage>(json));
        }

        [Test]
        public void MalformedPayloadIsReportedInsteadOfThrowing()
        {
            Assert.IsFalse(JsonCodec.TryDeserialize("{not json", out ServerMessage message));
            Assert.IsNull(message);
        }

        [Test]
        public void EmptyPayloadIsReportedInsteadOfThrowing()
        {
            Assert.IsFalse(JsonCodec.TryDeserialize("   ", out ServerMessage message));
            Assert.IsNull(message);
        }

        [Test]
        public void FieldsTheClientDoesNotModelAreIgnored()
        {
            const string json = @"{""type"":""result"",""leftPlayer"":""alice"",""serverOnlyField"":{""a"":1}}";

            ResultInfo result = (ResultInfo)JsonCodec.Deserialize<ServerMessage>(json);

            Assert.AreEqual("alice", result.leftPlayer);
        }

        /// <summary>
        /// PVE 스크립트 이벤트는 <c>type</c> 없이 오기도 한다. 구체 타입으로 직접 요청하면 판별자를 건너뛰어야 한다.
        /// </summary>
        [Test]
        public void ConcreteSubtypeRequestSkipsDiscriminator()
        {
            const string json = @"{""key"":""intro"",""speakerObjectId"":9,""lines"":[""line-a"",""line-b""]}";

            PveScriptEventInfo payload = JsonCodec.Deserialize<PveScriptEventInfo>(json);

            Assert.IsNotNull(payload);
            Assert.AreEqual(9, payload.speakerObjectId);
            Assert.AreEqual(2, payload.lines.Count);
        }
    }
}
