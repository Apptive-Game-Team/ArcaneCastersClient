using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class CraterLavaPrefabTests
    {
        [Test]
        public void LavaCyclesOnTheExistingCraterRenderer()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/Crater");
            var components = prefab.GetComponents<MonoBehaviour>();
            var owner = new SerializedObject(components.Single(component => component.GetType().Name == "ServedObject"));
            var animation = new SerializedObject(components.Single(component => component.GetType().Name == "SpriteFrameAnimator"));
            var renderer = (SpriteRenderer)animation.FindProperty("spriteRenderer").objectReferenceValue;
            var original = Resources.Load<Sprite>("Game/sprites/Crater");
            Assert.AreSame(owner.FindProperty("_spriteRenderer").objectReferenceValue, renderer);
            Assert.AreSame(original, renderer.sprite);
            Assert.AreEqual(new Vector2(128, 0), original.pivot);
            Assert.IsFalse(owner.FindProperty("_keepUprightOnSpawn").boolValue);
            Assert.IsTrue(animation.FindProperty("loop").boolValue);
            Assert.AreEqual(.18f, animation.FindProperty("frameInterval").floatValue);

            var frames = animation.FindProperty("frames");
            Assert.AreEqual(6, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var frame = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.AreEqual($"CraterLava{index + 1:00}", frame.name);
                Assert.AreEqual(original.rect, frame.rect);
                Assert.AreEqual(original.pivot, frame.pivot);
                Assert.AreEqual(original.pixelsPerUnit, frame.pixelsPerUnit);
            }
        }
    }
}
