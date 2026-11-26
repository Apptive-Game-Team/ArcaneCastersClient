using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class OvergrowthRisePrefabTests
    {
        [Test]
        public void OvergrowthGrowsOnTheExistingPopRendererAndEndsOnTheOriginalSprite()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/Overgrowth");
            var owner = prefab.GetComponents<MonoBehaviour>().Single(component => component.GetType().Name == "ServedObject");
            var animator = new SerializedObject(prefab.GetComponent("SpriteFrameAnimator"));
            var renderer = (SpriteRenderer)new SerializedObject(owner).FindProperty("_spriteRenderer").objectReferenceValue;
            var original = Resources.Load<Sprite>("Game/sprites/Overgrowth");

            Assert.AreSame(renderer, animator.FindProperty("spriteRenderer").objectReferenceValue);
            Assert.AreSame(original, renderer.sprite);
            Assert.IsNotNull(renderer.GetComponent("PopSelfController"));
            Assert.AreEqual(.12f, animator.FindProperty("frameInterval").floatValue);
            Assert.IsFalse(animator.FindProperty("loop").boolValue);
            Assert.IsNull(Resources.Load<GameObject>("Prefabs/Abstract/AbstractExplode").GetComponent("SpriteFrameAnimator"));

            var frames = animator.FindProperty("frames");
            Assert.AreEqual(5, frames.arraySize);
            for (int index = 0; index < frames.arraySize - 1; index++)
            {
                var frame = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.IsNotNull(frame);
                Assert.AreEqual($"OvergrowthRise{index + 1:00}", frame.name);
                Assert.AreEqual(original.rect, frame.rect);
                Assert.AreEqual(original.pixelsPerUnit, frame.pixelsPerUnit);
                Assert.AreEqual(original.pivot, frame.pivot);
            }
            Assert.AreSame(original, frames.GetArrayElementAtIndex(4).objectReferenceValue);
        }
    }
}