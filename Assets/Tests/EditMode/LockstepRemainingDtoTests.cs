using System;
using System.Reflection;
using System.Linq;
using Global.Serialization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using Property = NUnit.Framework.PropertyAttribute;

namespace WordOnline.Tests {
    public class LockstepRemainingDtoTests {
        private static object Read(string typeName, string json) => typeof(JsonCodec).GetMethod("Deserialize")
            .MakeGenericMethod(AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(typeName, false)).Single(type => type != null)).Invoke(null, new object[] { json });
        private static object Field(object target, string field) => target.GetType().GetField(field).GetValue(target);

        [Property("OriginalSimulationTest", "game/util/java_math.rs::matches_java_at_the_edges")]
        [Test] public void FixedInputBoundaryRejectsNonFiniteValuesAndCanonicalizesNegativeZero() {
            typeof(LockstepSceneInputTests).GetMethod("WithInputConnection", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { (Action<Type, Component, ArcaneCasters.Simulation.LockstepLocalInputs>)((type, connector, local) => {
                    var input = Read("Data.CardUseInput", "{\"magicId\":34,\"id\":7}");
                    var position = input.GetType().GetField("position");
                    foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity }) {
                        position.SetValue(input, new Vector3(invalid, 0, 5));
                        Assert.IsFalse((bool)type.GetMethod("TrySendCardUse").Invoke(connector, new[] { input }));
                        position.SetValue(input, new Vector3(5, 0, invalid));
                        Assert.IsFalse((bool)type.GetMethod("TrySendCardUse").Invoke(connector, new[] { input }));
                    }
                    Assert.IsEmpty(local.Prepare(1, 0).commands); local.Clear();
                    position.SetValue(input, new Vector3(-0.0f, 0, -0.0f));
                    Assert.IsTrue((bool)type.GetMethod("TrySendCardUse").Invoke(connector, new[] { input }));
                    var command = local.Prepare(1, 0).commands[0];
                    Assert.AreEqual(0L, command.xRaw); Assert.AreEqual(0L, command.zRaw);

                    Assert.AreEqual((FixMath.NET.Fix64)(-1), FixMath.NET.Fix64.Sign(-3));
                }) });
        }
        [Property("OriginalSimulationTest", "game/dto/tests.rs::magic_info")]
        [Test] public void MagicInfoRetainsLegacyIdNameAndElement() {
            const string original = "{\"id\":34,\"name\":\"Fire Shot\",\"element\":\"FIRE\"}";
            var dto = Read("Data.Magic.MagicInfoDto", original);
            Assert.AreEqual(34L, Field(dto, "id"));
            Assert.AreEqual("Fire Shot", Field(dto, "name"));
            Assert.AreEqual("FIRE", Field(dto, "element"));
            var copy = Read("Data.Magic.MagicInfoDto", JsonCodec.Serialize(dto));
            Assert.AreEqual(34L, Field(copy, "id"));
            Assert.AreEqual("Fire Shot", Field(copy, "name"));
            Assert.AreEqual("FIRE", Field(copy, "element"));
        }

        [Property("OriginalSimulationTest", "game/dto/tests.rs::float_formatting_matches_jackson_for_ordinary_values")]
        [Test] public void VectorBoundaryReadsBothJacksonExponentAndDecimalNotation() {
            var expected = new Vector3(0.1f, 100f, 1e-5f);
            var decimalValue = JsonCodec.Deserialize<Vector3>("{\"x\":0.1,\"y\":100.0,\"z\":0.00001}");
            var exponentValue = JsonCodec.Deserialize<Vector3>("{\"x\":0.1,\"y\":100.0,\"z\":1.0E-5}");
            Assert.AreEqual(expected, decimalValue);
            Assert.AreEqual(decimalValue, exponentValue);
            string json = JsonCodec.Serialize(expected);
            var axes = JObject.Parse(json);
            Assert.AreEqual(3, axes.Count);
            Assert.AreEqual(0.1f, axes["x"].Value<float>());
            Assert.AreEqual(100f, axes["y"].Value<float>());
            Assert.AreEqual(1e-5f, axes["z"].Value<float>());
            Assert.AreEqual(expected, JsonCodec.Deserialize<Vector3>(json));
        }

        [Property("OriginalSimulationTest", "game/dto/tests.rs::input_tolerates_unknown_missing_and_null_fields")]
        [Test] public void LegacyInputNullsKeepDefaultsAndInvalidEmptyCastCannotEnterLockstep() {
            var sync = Read("GameScene.Dto.PveSyncInput", "{\"type\":\"pveSync\",\"lastEventSeq\":null,\"extra\":{\"a\":1},\"magicId\":null}");
            Assert.AreEqual("pveSync", Field(sync, "type"));
            Assert.AreEqual(0, Field(sync, "lastEventSeq"));
            var emote = Read("Data.EmoteInput", "{\"type\":\"emote\",\"emote\":\"Cry\"}");
            Assert.AreEqual("Cry", Field(emote, "emote"));
            // DTO의 기본 type은 송신 편의용이다. 빈 입력도 실제 큐 진입점에서 거부되어야 한다.
            typeof(LockstepSceneInputTests).GetMethod("WithInputConnection", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { (Action<Type, Component, ArcaneCasters.Simulation.LockstepLocalInputs>)((type, connector, local) => {
                    var input = Read("Data.CardUseInput", "{}");
                    Assert.IsFalse((bool)type.GetMethod("TrySendCardUse").Invoke(connector, new[] { input }));
                    Assert.IsEmpty(local.Prepare(1, 0).commands);
                }) });
        }
    }
}
