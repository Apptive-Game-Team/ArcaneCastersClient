using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class LeafairDescentPrefabTests
    {
        [Test]
        public void LeafairUsesTheInheritedBodyRendererForAllDescentPoses()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/Leafair");
            var components = prefab.GetComponents<MonoBehaviour>();
            var owner = components.Single(component => component.GetType().Name == "ServedObject");
            var renderer = (SpriteRenderer)new SerializedObject(owner).FindProperty("_spriteRenderer").objectReferenceValue;
            var motion = new SerializedObject(components.Single(component => component.GetType().Name == "DropPoseController"));
            var original = Resources.Load<Sprite>("Game/sprites/Leafair");

            Assert.AreSame(original, renderer.sprite);
            Assert.AreSame(renderer, motion.FindProperty("targetRenderer").objectReferenceValue);
            Assert.AreSame(owner, motion.FindProperty("servedObject").objectReferenceValue);
            Assert.AreEqual(10f, motion.FindProperty("initialHeight").floatValue);
            Assert.IsNull(Resources.Load<GameObject>("Prefabs/FireDrop").GetComponent("DropPoseController"));

            var frames = motion.FindProperty("descentFrames");
            Assert.AreEqual(4, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var frame = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.IsNotNull(frame);
                Assert.AreEqual($"LeafairDescent{index + 1:00}", frame.name);
                Assert.AreEqual(original.rect, frame.rect);
                Assert.AreEqual(original.pixelsPerUnit, frame.pixelsPerUnit);
                Assert.AreEqual(new Vector2(frame.rect.width / 2f, 0f), frame.pivot);
                Assert.AreEqual(original.pivot, frame.pivot);
            }
        }
    }
}