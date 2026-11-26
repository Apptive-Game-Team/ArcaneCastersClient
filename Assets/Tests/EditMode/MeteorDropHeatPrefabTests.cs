using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class MeteorDropHeatPrefabTests
    {
        [Test]
        public void DropPosesUseTheExistingAfterImageRenderer()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/MeteorDrop");
            var components = prefab.GetComponents<MonoBehaviour>();
            var owner = components.Single(c => c.GetType().Name == "ServedObject");
            var landing = new SerializedObject(components.Single(c => c.GetType().Name == "DropPoseController"));
            var trail = new SerializedObject(components.Single(c => c.GetType().Name == "AfterImageSpawner"));
            var renderer = (SpriteRenderer)new SerializedObject(owner).FindProperty("_spriteRenderer").objectReferenceValue;
            var original = Resources.Load<Sprite>("Game/sprites/MeteorShower");

            Assert.AreSame(renderer, landing.FindProperty("targetRenderer").objectReferenceValue);
            Assert.AreSame(owner, landing.FindProperty("servedObject").objectReferenceValue);
            Assert.AreSame(renderer, trail.FindProperty("sourceRenderer").objectReferenceValue);
            Assert.AreSame(original, renderer.sprite);
            Assert.AreEqual(10f, landing.FindProperty("initialHeight").floatValue);

            var frames = landing.FindProperty("descentFrames");
            Assert.AreEqual(4, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var frame = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.IsNotNull(frame);
                Assert.AreEqual($"MeteorDropHeat{index + 1:00}", frame.name);
                Assert.AreEqual(original.rect, frame.rect);
                Assert.AreEqual(original.pivot, frame.pivot);
                Assert.AreEqual(original.pixelsPerUnit, frame.pixelsPerUnit);
            }

            var shower = Resources.Load<GameObject>("Prefabs/MeteorShower");
            Assert.IsNull(shower.GetComponent("DropPoseController"));
            Assert.AreSame(original, shower.GetComponentInChildren<SpriteRenderer>(true).sprite);
        }
    }
}
