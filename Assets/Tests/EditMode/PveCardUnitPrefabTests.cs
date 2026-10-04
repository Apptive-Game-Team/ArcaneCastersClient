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

        private static bool HasComponentNamed(GameObject root, string typeName)
        {
            return root.GetComponentsInChildren<MonoBehaviour>(true)
                .Any(component => component != null && component.GetType().Name == typeName);
        }
    }
}
