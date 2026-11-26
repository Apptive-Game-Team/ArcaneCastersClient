using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class BrickGolemSweepArcPrefabTests
    {
        [Test]
        public void SweepVariantPlaysFourRegisteredFramesOnTheInheritedRenderer()
        {
            var prefab = Resources.Load<GameObject>("Projectiles/BrickGolemSweep");
            var parts = prefab.GetComponents<MonoBehaviour>();
            var projectile = new SerializedObject(parts.Single(component => component.GetType().Name == "DefaultProjectile"));
            var animator = parts.Single(component => component.GetType().Name == "SpriteFrameAnimator");
            Assert.AreSame(animator, projectile.FindProperty("flightFrames").objectReferenceValue);

            var body = (Transform)projectile.FindProperty("actualObject").objectReferenceValue;
            var renderer = body.GetComponent<SpriteRenderer>();
            var original = Resources.Load<Sprite>("Game/shoot/brick_golem_sweep");
            Assert.AreSame(original, renderer.sprite);
            Assert.AreEqual(new Vector2(128f, 72f), original.pivot);
            Assert.AreEqual(.8f, body.localPosition.y);

            var animatorData = new SerializedObject(animator);
            Assert.AreSame(renderer, animatorData.FindProperty("spriteRenderer").objectReferenceValue);
            Assert.IsFalse(animatorData.FindProperty("loop").boolValue);
            var frames = animatorData.FindProperty("frames");
            Assert.AreEqual(4, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var frame = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.IsNotNull(frame);
                Assert.AreEqual($"BrickGolemSweepArc{index + 1:00}", frame.name);
                Assert.AreEqual(original.rect, frame.rect);
                Assert.AreEqual(original.pivot, frame.pivot);
                Assert.AreEqual(original.pixelsPerUnit, frame.pixelsPerUnit);
            }
        }
    }
}
