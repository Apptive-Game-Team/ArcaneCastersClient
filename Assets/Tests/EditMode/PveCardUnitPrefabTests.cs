using System.Linq;
using System.Reflection;
using GameScene.Dto.Projectile;
using NUnit.Framework;
using UnityEngine;

namespace WordOnline.Tests
{
    public class PveCardUnitPrefabTests
    {
        [TestCase("PveDimensionToad")]
        [TestCase("PveFireTadpole")]
        [TestCase("PveLightningTadpole")]
        [TestCase("PveEvilEnt")]
        [TestCase("PveBrickGolem")]
        public void ServerTypeResolvesToPrefabOfTheSameName(string serverType)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/" + serverType);

            Assert.IsNotNull(prefab, $"Resources/Prefabs/{serverType}.prefab must be loadable.");
            Assert.AreEqual(serverType, prefab.name);
            Assert.IsTrue(HasComponentNamed(prefab, "ServedObject"));
            Assert.IsTrue(HasComponentNamed(prefab, "ServedObjectGaugeBar"),
                "The objective hp must be shown, so the gauge bar has to be nested.");
        }

        [Test]
        public void PveDimensionToadDoesNotWaddleButTheCardDoes()
        {
            GameObject pve = Resources.Load<GameObject>("Prefabs/PveDimensionToad");
            GameObject card = Resources.Load<GameObject>("Prefabs/DimensionToad");

            Assert.IsNotNull(pve);
            Assert.IsNotNull(card);
            Assert.IsFalse(HasComponentNamed(pve, "WaddleMotionController"),
                "A stationary gate keeper must not rock in place.");
            Assert.IsTrue(HasComponentNamed(card, "WaddleMotionController"),
                "The card unit prefab must keep its waddle.");
        }

        [TestCase("EvilEnt", 8)]
        [TestCase("PveEvilEnt", 0)]
        public void EvilEntVariantsHaveGroundedAttackAndOnlyTheCardWalks(string prefabName, int stepCount)
        {
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
            Assert.IsNotNull(prefab);
            Assert.IsFalse(HasComponentNamed(prefab, "HoppingMotionController"));
            Assert.IsFalse(HasComponentNamed(prefab, "AttackSpriteSwapController"));
            MonoBehaviour[] components = prefab.GetComponents<MonoBehaviour>();
            MonoBehaviour presentation = components.Single(component => component.GetType().Name == "GroundedUnitPoseController");
            MonoBehaviour unit = components.Single(component => component.GetType().Name == "ServedObject");
            Assert.IsFalse((bool)unit.GetType().GetField("_swingOnAttack", fields).GetValue(unit));
            Assert.That((Sprite[])presentation.GetType().GetField("walkFrames", fields).GetValue(presentation), Has.Length.EqualTo(stepCount));
            Assert.That((Sprite[])presentation.GetType().GetField("attackFrames", fields).GetValue(presentation), Has.Length.EqualTo(3));
        }

        [Test]
        public void PveBrickGolemStaysPlantedAndSwapsToItsAttackFrame()
        {
            GameObject golem = Resources.Load<GameObject>("Prefabs/PveBrickGolem");

            Assert.IsNotNull(golem);
            Assert.IsFalse(HasComponentNamed(golem, "HoppingMotionController"),
                "The golem rises out of the ground and must not hop.");
            Assert.IsFalse(HasComponentNamed(golem, "WaddleMotionController"),
                "The golem rises out of the ground and must not rock in place.");
            Assert.IsTrue(HasComponentNamed(golem, "BrickGolemPoseController"));
            Assert.IsFalse(HasComponentNamed(golem, "AttackSpriteSwapController"));
        }

        [TestCase("BrickGolemSlamWarning")]
        [TestCase("BrickGolemSlam")]
        [TestCase("BrickGolemSweep")]
        [TestCase("BrickGolemSweepPose")]
        public void BrickGolemProjectionResolvesToAProjectile(string projectionType)
        {
            GameObject prefab = Resources.Load<GameObject>("Projectiles/" + projectionType);

            Assert.IsNotNull(prefab, $"Resources/Projectiles/{projectionType}.prefab must be loadable.");
            // IProjectile lives in Assembly-CSharp, which this test assembly cannot reference.
            Assert.IsTrue(prefab.GetComponents<MonoBehaviour>()
                    .Any(component => component != null && component.GetType().GetInterface("IProjectile") != null),
                "ProjectileSpawner calls Init on the root IProjectile.");
        }

        [TestCase(1f)]
        [TestCase(0.8f)]
        public void BrickGolemSlamWarningUsesFourFramesWithinTheServerDuration(float duration)
        {
            GameObject prefab = Resources.Load<GameObject>("Projectiles/BrickGolemSlamWarning");
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
                MonoBehaviour warning = instance.GetComponents<MonoBehaviour>()
                    .Single(component => component.GetType().Name == "GroundWarningProjectile");
                var type = warning.GetType();
                var frames = (Sprite[])type.GetField("warningFrames", fields).GetValue(warning);
                var renderer = (SpriteRenderer)type.GetField("decalRenderer", fields).GetValue(warning);
                var dto = new ProjectileDto
                {
                    duration = duration,
                    start = new ReferenceProjectileTarget { id = 42 },
                    end = new PositionProjectileTarget { x = 5f, z = 2f }
                };
                type.GetMethod("Init").Invoke(warning, new object[] { dto });

                Assert.That(instance.transform.position, Is.EqualTo(new Vector3(5f, 0.03f, 2f)));
                Assert.That(type.GetField("duration", fields).GetValue(warning), Is.EqualTo(duration));
                Assert.That(renderer.sprite, Is.SameAs(frames[0]));
                Assert.That(frames.Distinct().Count(), Is.EqualTo(4));
                var applyProgress = type.GetMethod("ApplyProgress", fields);
                Sprite original = Resources.Load<Sprite>("Game/explode/brick_golem_crack_ring");
                for (int step = 0; step <= 4; step++)
                {
                    applyProgress.Invoke(warning, new object[] { step / 4f });
                    Sprite frame = frames[Mathf.Min(step, 3)];
                    Assert.That(renderer.sprite, Is.SameAs(frame));
                    Assert.That(frame.rect.size, Is.EqualTo(original.rect.size));
                    Assert.That(frame.pivot, Is.EqualTo(original.pivot));
                    Assert.That(frame.pixelsPerUnit, Is.EqualTo(original.pixelsPerUnit));
                }

                GameObject impact = Resources.Load<GameObject>("Projectiles/BrickGolemSlam");
                Assert.That(impact.GetComponentsInChildren<SpriteRenderer>(true)
                    .Any(item => item.sprite == original), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static bool HasComponentNamed(GameObject root, string typeName)
        {
            return root.GetComponentsInChildren<MonoBehaviour>(true)
                .Any(component => component != null && component.GetType().Name == typeName);
        }
    }
}
