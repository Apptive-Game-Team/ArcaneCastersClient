using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class BrickGolemSlamFistPrefabTests
    {
        [Test]
        public void SlamFramesShareTheOriginalContactAnchor()
        {
            var prefab = Resources.Load<GameObject>("Projectiles/BrickGolemSlam");
            var renderer = prefab.transform.Find("Fist").GetComponent<SpriteRenderer>();
            var presenter = prefab.GetComponents<MonoBehaviour>().Single(component =>
                component.GetType().Name == "FallingFistProjectile");
            var original = Resources.Load<Sprite>("Game/sprites/TitanFist");
            Assert.AreSame(original, renderer.sprite);
            Assert.IsTrue(renderer.flipY);
            Assert.AreEqual(new Vector2(90, 0), original.pivot);

            var serialized = new SerializedObject(presenter);
            Assert.AreSame(renderer, serialized.FindProperty("fistRenderer").objectReferenceValue);
            var frames = serialized.FindProperty("fistFrames");
            Assert.AreEqual(4, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var frame = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.IsNotNull(frame);
                Assert.AreEqual($"BrickGolemSlamFist{index + 1:00}", frame.name);
                Assert.AreEqual(original.rect, frame.rect);
                Assert.AreEqual(original.pivot, frame.pivot);
                Assert.AreEqual(original.pixelsPerUnit, frame.pixelsPerUnit);
            }
        }
    }
}
