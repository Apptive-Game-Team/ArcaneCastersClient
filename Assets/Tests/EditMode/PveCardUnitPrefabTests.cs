using System.Linq;
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
        public void OnlyTheCardDimensionToadUsesTravelFrames()
        {
            GameObject pve = Resources.Load<GameObject>("Prefabs/PveDimensionToad");
            GameObject card = Resources.Load<GameObject>("Prefabs/DimensionToad");

            Assert.IsNotNull(pve);
            Assert.IsNotNull(card);
            Assert.IsFalse(HasComponentNamed(pve, "WaddleMotionController"),
                "A stationary gate keeper must not rock in place.");
            Assert.IsFalse(HasComponentNamed(card, "WaddleMotionController"));
            Assert.IsTrue(HasComponentNamed(card, "UnitSpriteMotionController"));
        }

        [Test]
        public void EvilEntUsesAttackFramesAndOnlyThePvpUnitWalks()
        {
            GameObject pve = Resources.Load<GameObject>("Prefabs/PveEvilEnt");
            GameObject pvp = Resources.Load<GameObject>("Prefabs/EvilEnt");

            Assert.IsNotNull(pve);
            Assert.IsNotNull(pvp);
            Assert.IsFalse(HasComponentNamed(pve, "HoppingMotionController"),
                "A stationary boss must not hop in place.");
            Assert.IsFalse(HasComponentNamed(pvp, "HoppingMotionController"));
            foreach (GameObject unit in new[] { pve, pvp })
            {
                Component motion = unit.GetComponentsInChildren<Component>(true)
                    .Single(component => component != null && component.GetType().Name == "UnitSpriteMotionController");
                var serialized = new UnityEditor.SerializedObject(motion);
                Assert.AreEqual(6, serialized.FindProperty("attackFrames").arraySize);
                Assert.AreEqual(unit == pve ? 0 : 8, serialized.FindProperty("moveFrames").arraySize);
            }
        }

        [Test]
        public void PveBrickGolemStaysPlantedAndPlaysItsAttackFrames()
        {
            GameObject golem = Resources.Load<GameObject>("Prefabs/PveBrickGolem");

            Assert.IsNotNull(golem);
            Assert.IsFalse(HasComponentNamed(golem, "HoppingMotionController"),
                "The golem rises out of the ground and must not hop.");
            Assert.IsFalse(HasComponentNamed(golem, "WaddleMotionController"),
                "The golem rises out of the ground and must not rock in place.");
            Assert.IsFalse(HasComponentNamed(golem, "AttackSpriteSwapController"));
            Component motion = golem.GetComponentsInChildren<Component>(true)
                .Single(component => component != null && component.GetType().Name == "UnitSpriteMotionController");
            var serialized = new UnityEditor.SerializedObject(motion);
            Assert.AreEqual(0, serialized.FindProperty("moveFrames").arraySize);
            Assert.AreSame(Resources.Load<Sprite>("Game/sprites/PveBrickGolem2"),
                serialized.FindProperty("attackFrames").GetArrayElementAtIndex(0).objectReferenceValue);
        }

        [TestCase("BrickGolemSlamWarning")]
        [TestCase("BrickGolemSlam")]
        [TestCase("BrickGolemSweep")]
        public void BrickGolemProjectionResolvesToAProjectile(string projectionType)
        {
            GameObject prefab = Resources.Load<GameObject>("Projectiles/" + projectionType);

            Assert.IsNotNull(prefab, $"Resources/Projectiles/{projectionType}.prefab must be loadable.");
            // IProjectile lives in Assembly-CSharp, which this test assembly cannot reference.
            Assert.IsTrue(prefab.GetComponents<MonoBehaviour>()
                    .Any(component => component != null && component.GetType().GetInterface("IProjectile") != null),
                "ProjectileSpawner calls Init on the root IProjectile.");
        }

        private static bool HasComponentNamed(GameObject root, string typeName)
        {
            return root.GetComponentsInChildren<MonoBehaviour>(true)
                .Any(component => component != null && component.GetType().Name == typeName);
        }
    }
}
