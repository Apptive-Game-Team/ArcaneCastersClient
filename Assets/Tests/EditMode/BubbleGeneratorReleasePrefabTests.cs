using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class BubbleGeneratorReleasePrefabTests
    {
        [Test]
        public void ReleaseUsesTheServedRendererAndPreservesTheIdleSprite()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/BubbleGenerator");
            var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.IsFalse(components.Any(component => component.GetType().Name == "AttackSpriteSwapController"));

            var owner = new SerializedObject(components.Single(component => component.GetType().Name == "ServedObject"));
            var presentation = new SerializedObject(components.Single(component => component.GetType().Name == "GroundedUnitPoseController"));
            var renderer = (SpriteRenderer)presentation.FindProperty("targetRenderer").objectReferenceValue;
            Assert.AreSame(renderer, owner.FindProperty("_spriteRenderer").objectReferenceValue);
            Assert.IsFalse(owner.FindProperty("_swingOnAttack").boolValue);
            Assert.IsFalse(presentation.FindProperty("attackOnHit").boolValue);
            Assert.That(presentation.FindProperty("attackDuration").floatValue, Is.EqualTo(.44f));
            Assert.AreEqual(0, presentation.FindProperty("walkFrames").arraySize);
            Assert.AreEqual("BubbleGenerator", renderer.sprite.name);
            Assert.AreEqual(0, presentation.FindProperty("spawnFrames").arraySize);
            Assert.AreEqual(0, presentation.FindProperty("projectileAttackFrames").arraySize);
            Assert.IsFalse(owner.FindProperty("_keepUprightOnSpawn").boolValue);

            var frames = presentation.FindProperty("attackFrames");
            Assert.AreEqual(4, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var sprite = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.AreEqual($"BubbleGeneratorRelease{index + 2:00}", sprite.name);
                Assert.AreEqual(renderer.sprite.rect, sprite.rect);
                Assert.AreEqual(renderer.sprite.pivot, sprite.pivot);
                Assert.AreEqual(renderer.sprite.pixelsPerUnit, sprite.pixelsPerUnit);
            }
            Assert.AreEqual(new Vector2(87.5f, 0), renderer.sprite.pivot);
        }
    }
}
