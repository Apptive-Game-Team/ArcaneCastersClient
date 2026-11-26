using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class LifeTreePosePrefabTests
    {
        [Test]
        public void GrowthAndHealUseRegisteredSpritesOnTheServedRenderer()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/LifeTree");
            var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.IsFalse(components.Any(component => component.GetType().Name == "AttackSpriteSwapController"));

            var owner = new SerializedObject(components.Single(component => component.GetType().Name == "ServedObject"));
            var presentation = new SerializedObject(components.Single(component => component.GetType().Name == "GroundedUnitPoseController"));
            var renderer = (SpriteRenderer)presentation.FindProperty("targetRenderer").objectReferenceValue;
            Assert.AreSame(renderer, owner.FindProperty("_spriteRenderer").objectReferenceValue);
            Assert.IsFalse(owner.FindProperty("_swingOnAttack").boolValue);
            Assert.IsTrue(owner.FindProperty("_keepUprightOnSpawn").boolValue);
            Assert.IsFalse(presentation.FindProperty("attackOnHit").boolValue);
            Assert.That(presentation.FindProperty("attackDuration").floatValue, Is.EqualTo(.4f));
            Assert.AreEqual(0, presentation.FindProperty("walkFrames").arraySize);
            Assert.That(presentation.FindProperty("spawnDuration").floatValue, Is.EqualTo(.66f));
            Assert.AreEqual("LifeTreeGrowth06", renderer.sprite.name);
            Assert.AreEqual(new Vector2(256, 207), renderer.sprite.rect.size);
            Assert.AreEqual(new Vector2(128, 0), renderer.sprite.pivot);
            AssertFrames(presentation.FindProperty("spawnFrames"), "LifeTreeGrowth", 6, renderer.sprite);
            AssertFrames(presentation.FindProperty("attackFrames"), "LifeTreePulse", 4, renderer.sprite);
        }

        private static void AssertFrames(SerializedProperty frames, string prefix, int count, Sprite idle)
        {
            Assert.AreEqual(count, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var sprite = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.AreEqual($"{prefix}{index + 1:00}", sprite.name);
                Assert.AreEqual(idle.rect, sprite.rect);
                Assert.AreEqual(idle.pivot, sprite.pivot);
                Assert.AreEqual(idle.pixelsPerUnit, sprite.pixelsPerUnit);
            }
        }
    }
}
