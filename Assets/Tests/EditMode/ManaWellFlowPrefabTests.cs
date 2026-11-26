using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class ManaWellFlowPrefabTests
    {
        [Test]
        public void SurfaceFramesLoopOnTheServedRenderer()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/ManaWell");
            var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            var owner = new SerializedObject(components.Single(component => component.GetType().Name == "ServedObject"));
            var presentation = new SerializedObject(components.Single(component => component.GetType().Name == "SpriteFrameAnimator"));
            var renderer = (SpriteRenderer)presentation.FindProperty("spriteRenderer").objectReferenceValue;
            Assert.AreSame(renderer, owner.FindProperty("_spriteRenderer").objectReferenceValue);
            Assert.IsFalse(owner.FindProperty("_keepUprightOnSpawn").boolValue);
            Assert.IsTrue(presentation.FindProperty("loop").boolValue);
            Assert.That(presentation.FindProperty("frameInterval").floatValue, Is.EqualTo(.14f));
            Assert.AreEqual("ManaWellFlow01", renderer.sprite.name);
            Assert.AreEqual(new Vector2(128, 0), renderer.sprite.pivot);
            Assert.AreEqual(new Vector2(256, 179), renderer.sprite.rect.size);
            Assert.AreEqual(133.333333f, renderer.sprite.pixelsPerUnit);

            var frames = presentation.FindProperty("frames");
            Assert.AreEqual(6, frames.arraySize);
            Assert.AreSame(renderer.sprite, frames.GetArrayElementAtIndex(0).objectReferenceValue);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var sprite = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.AreEqual($"ManaWellFlow{index + 1:00}", sprite.name);
                Assert.AreEqual(renderer.sprite.rect, sprite.rect);
                Assert.AreEqual(renderer.sprite.pivot, sprite.pivot);
                Assert.AreEqual(renderer.sprite.pixelsPerUnit, sprite.pixelsPerUnit);
            }
        }
    }
}
