using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class ShockOverloadArcsPrefabTests
    {
        [Test]
        public void PrimaryAndSecondaryShareOneRegisteredSpriteSequence()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/ShockOverload");
            var original = Resources.Load<Sprite>("Game/sprites/ShockOverload");
            var renderer = prefab.transform.Find("actual").GetComponent<SpriteRenderer>();
            var components = prefab.GetComponents<MonoBehaviour>();
            var radius = new SerializedObject(components.Single(c => c.GetType().Name == "BlastRadiusScaler"));
            Assert.That(radius.FindProperty("referenceRadius").floatValue, Is.EqualTo(.93f));
            var pop = renderer.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "PopSelfController");
            Assert.That(new SerializedObject(pop).FindProperty("duration").floatValue, Is.EqualTo(.8f));
            Assert.AreSame(original, renderer.sprite);

            var animation = new SerializedObject(components.Single(c => c.GetType().Name == "SpriteFrameAnimator"));
            Assert.AreSame(renderer, animation.FindProperty("spriteRenderer").objectReferenceValue);
            Assert.AreEqual(.16f, animation.FindProperty("frameInterval").floatValue);
            Assert.IsFalse(animation.FindProperty("loop").boolValue);
            var frames = animation.FindProperty("frames");
            Assert.AreEqual(4, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var frame = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.IsNotNull(frame);
                Assert.AreEqual($"ShockOverloadArc{index + 1:00}", frame.name);
                Assert.AreEqual(original.rect, frame.rect);
                Assert.AreEqual(original.pivot, frame.pivot);
                Assert.AreEqual(original.pixelsPerUnit, frame.pixelsPerUnit);
            }
        }
    }
}
