using System.Linq;
using System.Reflection;
using GameScene.Dto.Projectile;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class BrickGolemSlamFistPrefabTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void GroundCracksUseCompleteFrameSequenceOrOriginal(bool complete)
        {
            var prefab = Resources.Load<GameObject>("Projectiles/BrickGolemSlam");
            var instance = Object.Instantiate(prefab);
            try
            {
                const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
                var renderer = instance.transform.Find("Cracks").GetComponent<SpriteRenderer>();
                var presenter = instance.GetComponents<MonoBehaviour>().Single(component =>
                    component.GetType().Name == "FallingFistProjectile");
                var type = presenter.GetType();
                var frames = (Sprite[])type.GetField("crackFrames", privateInstance).GetValue(presenter);
                var original = Resources.Load<Sprite>("Game/explode/brick_golem_crack_ring");
                Assert.That(renderer.sprite, Is.SameAs(original));
                Assert.That(frames, Has.Length.EqualTo(4));
                Assert.That(frames[3], Is.SameAs(original));
                for (int index = 0; index < 3; index++)
                {
                    Assert.That(frames[index].name, Is.EqualTo($"brick-golem-cracks-{index + 1:00}"));
                    Assert.That(frames[index].rect, Is.EqualTo(original.rect));
                    Assert.That(frames[index].pivot, Is.EqualTo(original.pivot));
                    Assert.That(frames[index].pixelsPerUnit, Is.EqualTo(original.pixelsPerUnit));
                }
                if (!complete)
                {
                    frames[1] = null;
                }
                type.GetMethod("Init").Invoke(presenter, new object[] { new ProjectileDto
                {
                    end = new PositionProjectileTarget { x = 5f, z = 2f },
                    duration = 0.3f,
                } });
                Assert.That(renderer.enabled, Is.False);
                var applyProgress = type.GetMethod("ApplyProgress", privateInstance);
                float[] progress = { 0.45f, 0.56f, 0.72f, 0.89f, 1f };
                for (int index = 0; index < progress.Length; index++)
                {
                    applyProgress.Invoke(presenter, new object[] { progress[index] });
                    Assert.That(renderer.enabled, Is.True);
                    Assert.That(renderer.sprite, Is.SameAs(complete ? frames[Mathf.Min(index, 3)] : original));
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

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
