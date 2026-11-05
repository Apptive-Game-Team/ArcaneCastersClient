using GameScene.Dto;
using Global.Serialization;
using NUnit.Framework;

namespace WordOnline.Tests
{
    /// <summary>
    /// The <c>pveSync</c> request and the <c>seq</c> rule that keeps a replayed script event from showing twice.
    /// </summary>
    public class PveSyncTests
    {
        [Test]
        public void RequestCarriesTypeAndLastEventSeq()
        {
            Assert.AreEqual("{\"type\":\"pveSync\",\"lastEventSeq\":0}", JsonCodec.Serialize(new PveSyncInput(0)));
            Assert.AreEqual("{\"type\":\"pveSync\",\"lastEventSeq\":7}", JsonCodec.Serialize(new PveSyncInput(7)));
        }

        [Test]
        public void ScriptEventReadsSeq()
        {
            const string json = @"{""type"":""pveScriptEvent"",""key"":""intro"",""speakerObjectId"":0,
                ""lines"":[""hi""],""seq"":3}";

            ServerMessage message = JsonCodec.Deserialize<ServerMessage>(json);

            Assert.IsInstanceOf<PveScriptEventInfo>(message);
            Assert.AreEqual(3, ((PveScriptEventInfo)message).seq);
        }

        [Test]
        public void ScriptEventWithoutSeqReadsZero()
        {
            ServerMessage message = JsonCodec.Deserialize<ServerMessage>(
                @"{""type"":""pveScriptEvent"",""key"":""intro"",""lines"":[""hi""]}");

            Assert.AreEqual(0, ((PveScriptEventInfo)message).seq);
        }

        [Test]
        public void NewerSeqIsShownAndRemembered()
        {
            PveSyncState state = new PveSyncState();

            Assert.IsTrue(state.ShouldShow(1));
            Assert.IsTrue(state.ShouldShow(2));
            Assert.AreEqual(2, state.LastEventSeq);
        }

        [Test]
        public void SeenOrOlderSeqIsIgnored()
        {
            PveSyncState state = new PveSyncState();
            state.ShouldShow(3);

            Assert.IsFalse(state.ShouldShow(3));
            Assert.IsFalse(state.ShouldShow(2));
            Assert.AreEqual(3, state.LastEventSeq);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void UnnumberedEventIsAlwaysShownAndKeepsLastSeq(int seq)
        {
            PveSyncState state = new PveSyncState();
            state.ShouldShow(4);

            Assert.IsTrue(state.ShouldShow(seq));
            Assert.IsTrue(state.ShouldShow(seq));
            Assert.AreEqual(4, state.LastEventSeq);
        }

        [Test]
        public void ResetLetsTheNextMatchShowItsFirstEvent()
        {
            PveSyncState state = new PveSyncState();
            state.ShouldShow(5);

            state.Reset();

            Assert.AreEqual(0, state.LastEventSeq);
            Assert.IsTrue(state.ShouldShow(1));
        }

        [Test]
        public void StateReadsChannelValueAndNullValue()
        {
            ServerMessage message = JsonCodec.Deserialize<ServerMessage>(
                "{\"type\":\"pveState\",\"channel\":\"bgm\",\"value\":null,\"seq\":4}");

            var info = (PveStateInfo)message;
            Assert.AreEqual("bgm", info.channel);
            Assert.IsNull(info.value);
            Assert.AreEqual(4, info.seq);
        }

        [Test]
        public void StateAppliesOnlyWhenSeqIsNewerPerChannel()
        {
            var state = new PveSyncState();

            Assert.IsTrue(state.ShouldApplyState("bgm", 2));
            Assert.IsFalse(state.ShouldApplyState("bgm", 2));
            Assert.IsFalse(state.ShouldApplyState("bgm", 1));
            Assert.IsTrue(state.ShouldApplyState("other", 1));
            Assert.IsTrue(state.ShouldApplyState("bgm", 5));

            state.Reset();
            Assert.IsTrue(state.ShouldApplyState("bgm", 1));
        }
    }
}
