using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class TitanFistRisePrefabTests
    {
        [Test]
        public void RiseFramesShareTheOriginalGroundAnchor()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/TitanFist");
            var renderer = prefab.GetComponent<SpriteRenderer>();
            var presenter = prefab.GetComponents<MonoBehaviour>().Single(component =>
                component.GetType().Name == "TitanFistPresenter");
            var original = Resources.Load<Sprite>("Game/sprites/TitanFist");
            Assert.AreSame(original, renderer.sprite);
            Assert.AreEqual(new Vector2(90, 0), original.pivot);

            var frames = new SerializedObject(presenter).FindProperty("riseFrames");
            Assert.AreEqual(4, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var frame = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.AreEqual($"TitanFistRise{index + 1:00}", frame.name);
                Assert.AreEqual(original.rect, frame.rect);
                Assert.AreEqual(original.pivot, frame.pivot);
                Assert.AreEqual(original.pixelsPerUnit, frame.pixelsPerUnit);
            }
        }
    }
}
