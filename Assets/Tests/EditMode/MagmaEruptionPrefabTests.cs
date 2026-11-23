using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class MagmaEruptionPrefabTests
    {
        [Test]
        public void ServerCountdownRaisesTheGroundWithoutPlayingTheBurstEarly()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/MagmaExplosion");
            Assert.IsNotNull(prefab);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                MonoBehaviour visual = instance.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "MagmaEruptionPresentation");
                MonoBehaviour owner = instance.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "ServedObject");
                visual.GetType().GetMethod("Bind").Invoke(visual, new object[] {owner});
                MethodInfo handle = visual.GetType().GetMethod("HandleGauge", BindingFlags.NonPublic | BindingFlags.Instance);
                Type gaugeType = handle.GetParameters()[0].ParameterType;
                object gauge = Activator.CreateInstance(gaugeType);
                gaugeType.GetField("category").SetValue(gauge, "TTL");
                gaugeType.GetField("maxValue").SetValue(gauge, 3f);
                gaugeType.GetField("value").SetValue(gauge, 3f);
                handle.Invoke(visual, new[] {gauge});
                MeshFilter mound = instance.GetComponentsInChildren<MeshFilter>().Single(f => f.name == "SwellingGround");
                float startHeight = mound.sharedMesh.vertices[0].y;
                gaugeType.GetField("value").SetValue(gauge, 0.1f);
                handle.Invoke(visual, new[] {gauge});
                Assert.Greater(mound.sharedMesh.vertices[0].y, startHeight * 5f);
                Assert.AreEqual(21, instance.GetComponentsInChildren<LineRenderer>().Length);
                SpriteRenderer eruption = (SpriteRenderer)visual.GetType().GetField("eruptionRenderer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(visual);
                Assert.IsFalse(eruption.enabled);
                Assert.IsTrue(instance.GetComponentsInChildren<MonoBehaviour>().Where(c => c.GetType().Name == "PopSelfController" || c.GetType().Name == "SpriteFrameAnimator").All(c => !c.enabled));
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
