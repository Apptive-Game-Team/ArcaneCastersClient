using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class TidalImpactPrefabTests
    {
        [TestCase("TidalWarhead", "TidalWarheadFlight")]
        [TestCase("GroundTidalWarhead", "GroundTidalWarheadFlight")]
        public void FlightUsesTheServerImpact(string prefabName, string sequenceName)
        {
            var flight = Resources.Load<GameObject>($"Prefabs/{prefabName}");
            var components = flight.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.IsFalse(components.Any(component => component.GetType().Name == "OnDestroySpawner"));
            var animation = new SerializedObject(components.Single(component => component.GetType().Name == "SpriteFrameAnimator"));
            Assert.That(animation.FindProperty("frameInterval").floatValue, Is.EqualTo(.11f));
            Assert.IsTrue(animation.FindProperty("loop").boolValue);
            var frames = animation.FindProperty("frames");
            Assert.AreEqual(6, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
                Assert.AreEqual($"{sequenceName}{index + 1:00}", frames.GetArrayElementAtIndex(index).objectReferenceValue.name);
        }

        [Test]
        public void ImpactPlaysItsSixRegisteredFramesOnce()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/TidalWarheadExplosion");
            var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.IsFalse(components.Any(component => component.GetType().Name == "PopSelfController"));
            var owner = new SerializedObject(components.Single(component => component.GetType().Name == "ServedObject"));
            var animation = new SerializedObject(components.Single(component => component.GetType().Name == "SpriteFrameAnimator"));
            var renderer = (SpriteRenderer)animation.FindProperty("spriteRenderer").objectReferenceValue;
            Assert.AreSame(owner.FindProperty("_spriteRenderer").objectReferenceValue, renderer);
            Assert.IsTrue(owner.FindProperty("_keepUprightOnSpawn").boolValue);
            Assert.IsFalse(animation.FindProperty("loop").boolValue);
            Assert.That(animation.FindProperty("frameInterval").floatValue, Is.EqualTo(.09f));
            var frames = animation.FindProperty("frames");
            Assert.AreEqual(6, frames.arraySize);
            Assert.AreSame(renderer.sprite, frames.GetArrayElementAtIndex(0).objectReferenceValue);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var sprite = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.AreEqual($"TidalImpact{index + 1:00}", sprite.name);
                Assert.AreEqual(new Vector2(256, 144), sprite.rect.size);
                Assert.That(sprite.pivot.x, Is.EqualTo(128).Within(.01f));
                Assert.That(sprite.pivot.y, Is.EqualTo(4).Within(.01f));
                Assert.AreEqual(100, sprite.pixelsPerUnit);
            }
        }
    }
}
