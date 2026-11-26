using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WordOnline.Tests
{
    public class BubbleShotFlightPrefabTests
    {
        [Test]
        public void BubbleShotUsesFourFramesAndTheSharedEffectKeepsItsSprite()
        {
            var prefab = Resources.Load<GameObject>("Projectiles/BubbleShot");
            var components = prefab.GetComponents<MonoBehaviour>();
            var projectile = new SerializedObject(components.Single(component => component.GetType().Name == "DefaultProjectile"));
            var animator = components.Single(component => component.GetType().Name == "SpriteFrameAnimator");
            Assert.AreSame(animator, projectile.FindProperty("flightFrames").objectReferenceValue);

            var body = (Transform)projectile.FindProperty("actualObject").objectReferenceValue;
            var renderer = body.GetComponent<SpriteRenderer>();
            var original = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Images/Effect/Bubble.png");
            Assert.AreSame(original, renderer.sprite);
            Assert.AreEqual(new Vector2(59.5f, 59.5f), original.pivot);
            var effect = Resources.Load<GameObject>("Prefabs/Effects/Bubble");
            Assert.That(effect.GetComponentsInChildren<Image>(true).Any(image => image.sprite == original), Is.True);

            var animatorData = new SerializedObject(animator);
            Assert.AreSame(renderer, animatorData.FindProperty("spriteRenderer").objectReferenceValue);
            Assert.IsFalse(animatorData.FindProperty("loop").boolValue);
            var frames = animatorData.FindProperty("frames");
            Assert.AreEqual(4, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var frame = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.IsNotNull(frame);
                Assert.AreEqual($"BubbleShotFlight{index + 1:00}", frame.name);
                Assert.AreEqual(original.rect, frame.rect);
                Assert.AreEqual(original.pivot, frame.pivot);
                Assert.AreEqual(original.pixelsPerUnit, frame.pixelsPerUnit);
            }
        }
    }
}
