using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace WordOnline.Tests
{
    public class WindBurstPrefabTests
    {
        [Test]
        public void BombImpactIsPresentedByTheServerExplosion()
        {
            var bomb = Resources.Load<GameObject>("Prefabs/BombSpriteBomb");
            Assert.IsFalse(bomb.GetComponentsInChildren<MonoBehaviour>(true)
                .Any(component => component.GetType().Name == "OnDestroySpawner"));
        }

        [TestCase("WindExplode", .037f, 2, 4, 5, 3)]
        [TestCase("BombSpriteExplosion", .09f, 1, 1, 1, 1)]
        public void WindBurstUsesTheExistingRendererWithItsOwnTiming(
            string prefabName, float interval, int first, int second, int third, int fourth)
        {
            const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var prefab = Resources.Load<GameObject>($"Prefabs/{prefabName}");
            var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.IsFalse(components.Any(component => component.GetType().Name == "PopSelfController"));
            var owner = components.Single(component => component.GetType().Name == "ServedObject");
            Assert.IsTrue((bool)owner.GetType().GetField("_keepUprightOnSpawn", Fields).GetValue(owner));
            var sequence = components.Single(component => component.GetType().Name == "SpriteFrameAnimator");
            var renderer = (SpriteRenderer)sequence.GetType().GetField("spriteRenderer", Fields).GetValue(sequence);
            Assert.AreSame(owner.GetType().GetField("_spriteRenderer", Fields).GetValue(owner), renderer);
            Assert.That(sequence.GetType().GetField("frameInterval", Fields).GetValue(sequence), Is.EqualTo(interval));
            Assert.IsFalse((bool)sequence.GetType().GetField("loop", Fields).GetValue(sequence));

            var slots = new[] { first, second, third, fourth, 1, 1 };
            var expected = slots.SelectMany((count, index) => Enumerable.Repeat($"WindBurst{index + 1:00}", count));
            var frames = (Sprite[])sequence.GetType().GetField("frames", Fields).GetValue(sequence);
            CollectionAssert.AreEqual(expected, frames.Select(frame => frame.name));
            Assert.AreSame(frames[0], renderer.sprite);
            Assert.AreEqual(Vector3.one, renderer.transform.localScale);
            Assert.That(renderer.color.a, Is.EqualTo(.70f).Within(.001f));

            var master = Resources.Load<Sprite>("Game/explode/wind_explode");
            foreach (Sprite frame in frames.Distinct())
            {
                Assert.AreEqual(master.rect, frame.rect);
                Assert.AreEqual(master.pivot, frame.pivot);
                Assert.AreEqual(master.pixelsPerUnit, frame.pixelsPerUnit);
            }
        }
    }
}
