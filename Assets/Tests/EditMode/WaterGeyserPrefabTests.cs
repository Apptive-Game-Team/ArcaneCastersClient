using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class WaterGeyserPrefabTests
    {
        [Test]
        public void WaterExplosionUsesItsRegisteredGeyserSequence()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/WaterExplosion");
            var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.IsFalse(components.Any(component => component.GetType().Name == "PopSelfController"));

            var owner = new SerializedObject(components.Single(component => component.GetType().Name == "ServedObject"));
            var animation = new SerializedObject(components.Single(component => component.GetType().Name == "SpriteFrameAnimator"));
            var renderer = (SpriteRenderer)animation.FindProperty("spriteRenderer").objectReferenceValue;
            Assert.AreSame(owner.FindProperty("_spriteRenderer").objectReferenceValue, renderer);
            Assert.IsTrue(owner.FindProperty("_keepUprightOnSpawn").boolValue);
            Assert.IsFalse(animation.FindProperty("loop").boolValue);
            Assert.That(animation.FindProperty("frameInterval").floatValue, Is.EqualTo(.02f));

            var slots = animation.FindProperty("frames");
            var frames = Enumerable.Range(0, slots.arraySize)
                .Select(index => (Sprite)slots.GetArrayElementAtIndex(index).objectReferenceValue).ToArray();
            var expected = new[] { 10, 8, 6, 4, 1, 1 }.SelectMany((count, index) =>
                Enumerable.Repeat($"WaterGeyser{index + 1:00}", count));
            CollectionAssert.AreEqual(expected, frames.Select(frame => frame.name));
            Assert.AreSame(frames[0], renderer.sprite);

            foreach (Sprite frame in frames.Distinct())
            {
                Assert.AreEqual(new Vector2(198, 256), frame.rect.size);
                Assert.That(frame.pivot.x, Is.EqualTo(99).Within(.01f));
                Assert.That(frame.pivot.y, Is.EqualTo(3).Within(.01f));
                Assert.AreEqual(100, frame.pixelsPerUnit);
            }
        }
    }
}
