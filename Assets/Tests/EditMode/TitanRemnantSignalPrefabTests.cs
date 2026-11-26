using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class TitanRemnantSignalPrefabTests
    {
        [Test]
        public void SignalUsesTheUprightRemnantRenderer()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/TitanRemnant");
            var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.IsFalse(components.Any(component => component.GetType().Name == "AttackSpriteSwapController"));

            var owner = new SerializedObject(components.Single(component => component.GetType().Name == "ServedObject"));
            var presentation = new SerializedObject(components.Single(component => component.GetType().Name == "GroundedUnitPoseController"));
            var renderer = (SpriteRenderer)presentation.FindProperty("targetRenderer").objectReferenceValue;
            Assert.AreSame(renderer, owner.FindProperty("_spriteRenderer").objectReferenceValue);
            Assert.IsFalse(owner.FindProperty("_swingOnAttack").boolValue);
            Assert.IsFalse(presentation.FindProperty("attackOnHit").boolValue);
            Assert.That(presentation.FindProperty("attackDuration").floatValue, Is.EqualTo(.52f));
            Assert.AreEqual(0, presentation.FindProperty("walkFrames").arraySize);
            Assert.AreEqual("TitanRemnant", renderer.sprite.name);
            Assert.AreEqual(0, presentation.FindProperty("spawnFrames").arraySize);
            Assert.AreEqual(0, presentation.FindProperty("projectileAttackFrames").arraySize);
            Assert.IsTrue(owner.FindProperty("_keepUprightOnSpawn").boolValue);

            var frames = presentation.FindProperty("attackFrames");
            Assert.AreEqual(4, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var sprite = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.AreEqual($"TitanRemnantSignal{index + 1:00}", sprite.name);
                Assert.AreEqual(renderer.sprite.rect, sprite.rect);
                Assert.AreEqual(renderer.sprite.pivot, sprite.pivot);
                Assert.AreEqual(renderer.sprite.pixelsPerUnit, sprite.pixelsPerUnit);
            }
            Assert.AreEqual(new Vector2(126.5f, 0), renderer.sprite.pivot);
        }
    }
}
