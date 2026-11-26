using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class SeaSerpentHydroFlowPrefabTests
    {
        [Test]
        public void SlicedFlowFramesShareTheOriginalMouthAndTipBorders()
        {
            var prefab = Resources.Load<GameObject>("Projectiles/SeaSerpentHydroPump");
            var components = prefab.GetComponents<MonoBehaviour>();
            var beam = new SerializedObject(components.Single(c => c.GetType().Name == "BeamProjectile"));
            var animator = components.Single(c => c.GetType().Name == "SpriteFrameAnimator");
            var renderer = prefab.GetComponent<SpriteRenderer>();
            var original = Resources.Load<Sprite>("Game/sprites/SeaSerpentHydroPump");

            Assert.That(renderer.drawMode, Is.EqualTo(SpriteDrawMode.Sliced));
            Assert.AreSame(original, renderer.sprite);
            Assert.AreSame(renderer, beam.FindProperty("beamRenderer").objectReferenceValue);
            Assert.AreSame(animator, beam.FindProperty("beamFrames").objectReferenceValue);
            Assert.AreEqual(new Vector2(0f, 39.5f), original.pivot);
            Assert.AreEqual(new Vector4(68f, 0f, 39f, 0f), original.border);

            var animation = new SerializedObject(animator);
            Assert.AreSame(renderer, animation.FindProperty("spriteRenderer").objectReferenceValue);
            Assert.IsFalse(animation.FindProperty("loop").boolValue);
            var frames = animation.FindProperty("frames");
            Assert.AreEqual(4, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var frame = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.IsNotNull(frame);
                Assert.AreEqual($"SeaSerpentHydroFlow{index + 1:00}", frame.name);
                Assert.AreEqual(original.rect, frame.rect);
                Assert.AreEqual(original.pivot, frame.pivot);
                Assert.AreEqual(original.border, frame.border);
                Assert.AreEqual(original.pixelsPerUnit, frame.pixelsPerUnit);
                Assert.AreEqual(4, frame.vertices.Length);
            }
        }
    }
}
