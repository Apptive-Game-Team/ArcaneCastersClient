using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class FireworkTowerMuzzlePrefabTests
    {
        [Test]
        public void FireworkTowerUsesProjectilePosesOnTheExistingBody()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/FireworkTower");
            var components = prefab.GetComponents<MonoBehaviour>();
            var owner = components.Single(component => component.GetType().Name == "ServedObject");
            var serializedOwner = new SerializedObject(owner);
            var motion = new SerializedObject(components.Single(component => component.GetType().Name == "GroundedUnitPoseController"));
            var renderer = (SpriteRenderer)serializedOwner.FindProperty("_spriteRenderer").objectReferenceValue;
            var original = Resources.Load<Sprite>("Game/sprites/FireworkTower");

            Assert.AreSame(original, renderer.sprite);
            Assert.AreSame(renderer, motion.FindProperty("targetRenderer").objectReferenceValue);
            Assert.AreSame(owner, motion.FindProperty("servedObject").objectReferenceValue);
            Assert.IsFalse(serializedOwner.FindProperty("_swingOnAttack").boolValue);
            Assert.AreEqual(.4f, motion.FindProperty("attackDuration").floatValue);
            Assert.AreEqual(0, motion.FindProperty("attackFrames").arraySize);
            Assert.AreEqual(0, motion.FindProperty("walkFrames").arraySize);

            var frames = motion.FindProperty("projectileAttackFrames");
            Assert.AreEqual(4, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var frame = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.IsNotNull(frame);
                Assert.AreEqual($"FireworkTowerMuzzle{index + 1:00}", frame.name);
                Assert.AreEqual(original.rect, frame.rect);
                Assert.AreEqual(original.pixelsPerUnit, frame.pixelsPerUnit);
                Assert.AreEqual(original.pivot, frame.pivot);
            }
        }
    }
}