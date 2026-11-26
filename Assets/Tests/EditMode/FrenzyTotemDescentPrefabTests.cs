using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class FrenzyTotemDescentPrefabTests
    {
        [Test]
        public void DescentUsesTheServedRendererAndSixGroundAlignedSprites()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/FrenzyTotem");
            var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            var owner = new SerializedObject(components.Single(component => component.GetType().Name == "ServedObject"));
            var presentation = new SerializedObject(components.Single(component => component.GetType().Name == "DropPoseController"));
            var renderer = (SpriteRenderer)presentation.FindProperty("targetRenderer").objectReferenceValue;
            Assert.AreSame(renderer, owner.FindProperty("_spriteRenderer").objectReferenceValue);
            Assert.IsTrue(owner.FindProperty("_keepUprightOnSpawn").boolValue);
            Assert.IsFalse(components.Any(component => component.GetType().Name == "SpawnPopInEffect"));
            Assert.IsTrue(components.Any(component => component.GetType().Name == "OnDestroySpawner"));
            Assert.AreEqual(10f, presentation.FindProperty("initialHeight").floatValue);
            Assert.AreEqual("FrenzyTotemDescent01", renderer.sprite.name);
            Assert.AreEqual(new Vector2(81.5f, 0), renderer.sprite.pivot);
            Assert.AreEqual(new Vector2(163, 256), renderer.sprite.rect.size);
            Assert.AreEqual(133.333333f, renderer.sprite.pixelsPerUnit);

            var frames = presentation.FindProperty("descentFrames");
            Assert.AreEqual(6, frames.arraySize);
            Assert.AreSame(renderer.sprite, frames.GetArrayElementAtIndex(0).objectReferenceValue);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var sprite = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.AreEqual($"FrenzyTotemDescent{index + 1:00}", sprite.name);
                Assert.AreEqual(renderer.sprite.rect, sprite.rect);
                Assert.AreEqual(renderer.sprite.pivot, sprite.pivot);
                Assert.AreEqual(renderer.sprite.pixelsPerUnit, sprite.pixelsPerUnit);
            }
        }
    }
}
