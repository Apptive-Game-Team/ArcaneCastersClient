using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class SeedNestGrowthPrefabTests
    {
        [Test]
        public void GrowthEndsWithTheDefaultSpriteOnTheServedRenderer()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/SeedNest");
            var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            var owner = new SerializedObject(components.Single(component => component.GetType().Name == "ServedObject"));
            var presentation = new SerializedObject(components.Single(component => component.GetType().Name == "GroundedUnitPoseController"));
            var renderer = (SpriteRenderer)presentation.FindProperty("targetRenderer").objectReferenceValue;
            Assert.AreSame(renderer, owner.FindProperty("_spriteRenderer").objectReferenceValue);
            Assert.IsTrue(owner.FindProperty("_keepUprightOnSpawn").boolValue);
            Assert.AreEqual(0, presentation.FindProperty("attackFrames").arraySize);
            Assert.AreEqual(0, presentation.FindProperty("walkFrames").arraySize);
            Assert.That(presentation.FindProperty("spawnDuration").floatValue, Is.EqualTo(.66f));
            Assert.AreEqual("SeedNest", renderer.sprite.name);
            Assert.AreEqual(new Vector2(128, 0), renderer.sprite.pivot);
            Assert.AreEqual(new Vector2(256, 179), renderer.sprite.rect.size);
            Assert.AreEqual(100f, renderer.sprite.pixelsPerUnit);

            var frames = presentation.FindProperty("spawnFrames");
            Assert.AreEqual(6, frames.arraySize);
            Assert.AreSame(renderer.sprite, frames.GetArrayElementAtIndex(5).objectReferenceValue);
            for (int index = 0; index < 5; index++)
            {
                var sprite = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.AreEqual($"SeedNestGrowth{index + 1:00}", sprite.name);
                Assert.AreEqual(renderer.sprite.rect, sprite.rect);
                Assert.AreEqual(renderer.sprite.pivot, sprite.pivot);
                Assert.AreEqual(renderer.sprite.pixelsPerUnit, sprite.pixelsPerUnit);
            }
        }
    }
}
