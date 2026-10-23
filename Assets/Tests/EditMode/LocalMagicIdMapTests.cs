using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace WordOnline.Tests {
    public class LocalMagicIdMapTests {
        private static object Map() {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Global.LocalMagicIdMap")).First(t=>t!=null);
            var map=Activator.CreateInstance(type);
            type.GetMethod("Add").Invoke(map,new object[]{93L,88L});
            type.GetMethod("Add").Invoke(map,new object[]{92L,89L});
            type.GetMethod("Add").Invoke(map,new object[]{57L,57L});
            return map;
        }
        private static string Run(object map,string method,string json) => (string)map.GetType().GetMethod(method).Invoke(map,new object[]{json});
        [Test] public void CastSelectionAndResponseRoundTripWithoutChangingRequestOrObjectIds() {
            var map=Map();
            foreach (var kind in new[]{"useMagic","selectCard","unselectCard"}) {
                var input="{\"type\":\""+kind+"\",\"magicId\":93,\"id\":93,\"position\":{\"x\":1}}";
                var outgoing=Run(map,"Outgoing",input);
                StringAssert.Contains("\"magicId\":88",outgoing);
                StringAssert.Contains("\"id\":93",outgoing);
                Assert.AreEqual(input,Run(map,"Incoming",outgoing));
            }
            Assert.AreEqual("{\"magicId\":0}",Run(map,"Incoming","{\"magicId\":0}"));
        }
        [Test] public void SnapshotDeltaAndPublicEventsTranslateOnlyMagicIdentityFields() {
            var result=Run(Map(),"Incoming","{\"snapshotResponseDto\":{\"myCards\":[88,89,57],\"objects\":[{\"id\":88}]},\"cards\":{\"added\":[88]},\"events\":[{\"magicId\":89}]}" );
            StringAssert.Contains("\"myCards\":[93,92,57]",result);
            StringAssert.Contains("\"added\":[93]",result);
            StringAssert.Contains("\"magicId\":92",result);
            StringAssert.Contains("\"objects\":[{\"id\":88}]",result);
        }
        [Test] public void UnknownOrAmbiguousIdsFailClosed() {
            var map=Map();
            Assert.Throws<TargetInvocationException>(()=>Run(map,"Incoming","{\"cards\":{\"added\":[999]}}"));
            Assert.Throws<TargetInvocationException>(()=>map.GetType().GetMethod("Add").Invoke(map,new object[]{94L,88L}));
        }
    }
}
