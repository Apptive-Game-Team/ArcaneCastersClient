using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class NatureShotLeafPrefabTests
    {
        [Test]
        public void NatureShotUsesFourRegisteredFramesAndTheSharedLeafShotKeepsItsSprite()
        {
            var prefab = Resources.Load<GameObject>("Projectiles/NatureShot");
            var components = prefab.GetComponents<MonoBehaviour>();
            var projectile = new SerializedObject(components.Single(component => component.GetType().Name == "DefaultProjectile"));
            var animator = components.Single(component => component.GetType().Name == "SpriteFrameAnimator");
            Assert.AreSame(animator, projectile.FindProperty("flightFrames").objectReferenceValue);

            var body = (Transform)projectile.FindProperty("actualObject").objectReferenceValue;
            var renderer = body.GetComponent<SpriteRenderer>();
            var original = Resources.Load<Sprite>("Game/shoot/leaf_shoot");
            Assert.AreSame(original, renderer.sprite);
            Assert.AreEqual(new Vector2(96f, 0f), original.pivot);
            var leafShot = Resources.Load<GameObject>("Prefabs/LeafShot");
            Assert.That(leafShot.GetComponentsInChildren<SpriteRenderer>(true).Any(part => part.sprite == original), Is.True);

            var animatorData = new SerializedObject(animator);
            Assert.AreSame(renderer, animatorData.FindProperty("spriteRenderer").objectReferenceValue);
            Assert.IsFalse(animatorData.FindProperty("loop").boolValue);
            var frames = animatorData.FindProperty("frames");
            Assert.AreEqual(4, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var frame = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.IsNotNull(frame);
                Assert.AreEqual($"NatureShotLeaf{index + 1:00}", frame.name);
                Assert.AreEqual(original.rect, frame.rect);
                Assert.AreEqual(original.pivot, frame.pivot);
                Assert.AreEqual(original.pixelsPerUnit, frame.pixelsPerUnit);
            }
        }
    }
}
