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

        [Test]
        public void PveEvilEntDoesNotHopButThePvpUnitDoes()
        {
            GameObject pve = Resources.Load<GameObject>("Prefabs/PveEvilEnt");
            GameObject pvp = Resources.Load<GameObject>("Prefabs/EvilEnt");

            Assert.IsNotNull(pve);
            Assert.IsNotNull(pvp);
            Assert.IsFalse(HasComponentNamed(pve, "HoppingMotionController"),
                "A stationary boss must not hop in place.");
            Assert.IsTrue(HasComponentNamed(pvp, "HoppingMotionController"),
                "The PVP unit prefab must keep its hop.");
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
            Assert.IsTrue(HasComponentNamed(golem, "AttackSpriteSwapController"),
                "The golem shows its raised-arm frame on attack.");
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
