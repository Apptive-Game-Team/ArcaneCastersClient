using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class RallyingTotemDescentPrefabTests
    {
        [Test]
        public void RallyingTotemChangesOnlyItsBodyWhileDescending()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/RallyingTotem");
            var components = prefab.GetComponents<MonoBehaviour>();
            var owner = components.Single(component => component.GetType().Name == "ServedObject");
            var renderer = (SpriteRenderer)new SerializedObject(owner).FindProperty("_spriteRenderer").objectReferenceValue;
            var motion = new SerializedObject(components.Single(component => component.GetType().Name == "DropPoseController"));
            var original = Resources.Load<Sprite>("Game/sprites/RallyingTotem");
            var ring = prefab.GetComponentsInChildren<SpriteRenderer>(true).Single(item => item.name == "RallyZone");
            var ringAnimation = new SerializedObject(ring.GetComponent("SpriteFrameAnimator"));

            Assert.AreSame(original, renderer.sprite);
            Assert.AreSame(renderer, motion.FindProperty("targetRenderer").objectReferenceValue);
            Assert.AreSame(owner, motion.FindProperty("servedObject").objectReferenceValue);
            Assert.AreEqual(10f, motion.FindProperty("initialHeight").floatValue);
            Assert.AreSame(ring, ringAnimation.FindProperty("spriteRenderer").objectReferenceValue);
            Assert.AreEqual(8, ringAnimation.FindProperty("frames").arraySize);
            Assert.AreEqual(.22f, ringAnimation.FindProperty("frameInterval").floatValue);
            Assert.IsTrue(ringAnimation.FindProperty("loop").boolValue);

            var frames = motion.FindProperty("descentFrames");
            Assert.AreEqual(5, frames.arraySize);
            for (int index = 0; index < frames.arraySize - 1; index++)
            {
                var frame = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.IsNotNull(frame);
                Assert.AreEqual($"RallyingTotemDescent{index + 1:00}", frame.name);
                Assert.AreEqual(original.rect, frame.rect);
                Assert.AreEqual(original.pixelsPerUnit, frame.pixelsPerUnit);
                Assert.AreEqual(original.pivot, frame.pivot);
            }
            Assert.AreSame(original, frames.GetArrayElementAtIndex(4).objectReferenceValue);
        }
    }
}