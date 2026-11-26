using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class ShockTrapBurstPrefabTests
    {
        [Test]
        public void BurstKeepsItsBoundsThroughoutTheOneShotSequence()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/Effects/ShockBurst");
            var renderer = prefab.GetComponent<SpriteRenderer>();
            var components = prefab.GetComponents<MonoBehaviour>();
            var animation = new SerializedObject(components.Single(component => component.GetType().Name == "SpriteFrameAnimator"));
            var timer = new SerializedObject(components.Single(component => component.GetType().Name == "TimedDestroyer"));
            Assert.AreSame(renderer, animation.FindProperty("spriteRenderer").objectReferenceValue);
            Assert.That(animation.FindProperty("frameInterval").floatValue, Is.EqualTo(.04f));
            Assert.IsFalse(animation.FindProperty("loop").boolValue);
            Assert.That(timer.FindProperty("lifetime").floatValue, Is.EqualTo(.26f));
            Assert.AreEqual(Vector3.one, prefab.transform.localScale);

            var slots = animation.FindProperty("frames");
            Assert.AreEqual(6, slots.arraySize);
            Assert.AreSame(renderer.sprite, slots.GetArrayElementAtIndex(0).objectReferenceValue);
            for (int index = 0; index < slots.arraySize; index++)
            {
                var sprite = (Sprite)slots.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.AreEqual($"ShockTrapBurst{index + 1:00}", sprite.name);
                Assert.AreEqual(new Vector2(256, 155), sprite.rect.size);
                Assert.AreEqual(new Vector2(128, 0), sprite.pivot);
                Assert.That(sprite.bounds.size.x, Is.EqualTo(1.92f).Within(.001f));
            }
        }
    }
}
