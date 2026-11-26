using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class SprayedFlameFlowPrefabTests
    {
        [Test]
        public void FlowFramesUseTheStaticProjectileRendererAndOriginalRegistration()
        {
            var prefab = Resources.Load<GameObject>("Projectiles/SprayedFlame");
            var renderer = prefab.GetComponent<SpriteRenderer>();
            var components = prefab.GetComponents<MonoBehaviour>();
            var projectile = components.Single(component => component.GetType().Name == "StaticProjectile");
            var animator = components.Single(component => component.GetType().Name == "SpriteFrameAnimator");
            var original = Resources.Load<Sprite>("Game/shoot/sprayed_flame");
            Assert.AreSame(original, renderer.sprite);
            Assert.AreEqual(new Vector2(96, 0), original.pivot);
            Assert.AreSame(animator, new SerializedObject(projectile).FindProperty("projectileFrames").objectReferenceValue);

            var animatorData = new SerializedObject(animator);
            Assert.AreSame(renderer, animatorData.FindProperty("spriteRenderer").objectReferenceValue);
            Assert.IsFalse(animatorData.FindProperty("loop").boolValue);
            var frames = animatorData.FindProperty("frames");
            Assert.AreEqual(4, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var frame = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.IsNotNull(frame);
                Assert.AreEqual($"SprayedFlameFlow{index + 1:00}", frame.name);
                Assert.AreEqual(original.rect, frame.rect);
                Assert.AreEqual(original.pivot, frame.pivot);
                Assert.AreEqual(original.pixelsPerUnit, frame.pixelsPerUnit);
            }
        }
    }
}
