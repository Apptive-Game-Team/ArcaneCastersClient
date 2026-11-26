using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class FireworkBurstPrefabTests
    {
        [Test]
        public void BurstUsesTheExistingImpactRendererAndEndsWithAClearFrame()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/FireworkShell");
            var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.IsFalse(components.Any(component => component.GetType().Name == "PopSelfController"));
            var owner = new SerializedObject(components.Single(component => component.GetType().Name == "ServedObject"));
            var animation = new SerializedObject(components.Single(component => component.GetType().Name == "SpriteFrameAnimator"));
            var body = (SpriteRenderer)animation.FindProperty("spriteRenderer").objectReferenceValue;
            Assert.AreSame(owner.FindProperty("_spriteRenderer").objectReferenceValue, body);
            Assert.IsTrue(owner.FindProperty("_keepUprightOnSpawn").boolValue);
            Assert.AreEqual("FireworkExplosion", body.sprite.name);
            Assert.AreEqual(.8f, body.color.a);
            Assert.AreEqual(.04f, animation.FindProperty("frameInterval").floatValue);
            Assert.IsFalse(animation.FindProperty("loop").boolValue);

            int[] holds = { 1, 2, 3, 5, 4, 5, 1 };
            var slots = animation.FindProperty("frames");
            Assert.AreEqual(holds.Sum(), slots.arraySize);
            int slot = 0;
            for (int index = 0; index < holds.Length; index++)
            {
                for (int hold = 0; hold < holds[index]; hold++)
                {
                    var frame = (Sprite)slots.GetArrayElementAtIndex(slot++).objectReferenceValue;
                    Assert.AreEqual($"FireworkBurst{index + 1:00}", frame.name);
                    Assert.AreEqual(body.sprite.rect, frame.rect);
                    Assert.AreEqual(body.sprite.pivot, frame.pivot);
                    Assert.AreEqual(body.sprite.pixelsPerUnit, frame.pixelsPerUnit);
                }
            }
        }
    }
}
