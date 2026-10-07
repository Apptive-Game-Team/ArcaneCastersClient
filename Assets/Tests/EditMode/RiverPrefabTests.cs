using NUnit.Framework;
using UnityEngine;

namespace WordOnline.Tests
{
    /// <summary>
    /// RiverWater 와 RiverBridge prefab 은 Editor 없이 손으로 쓴 YAML 이다. script guid 가 어긋나면 component 가
    /// 통째로 사라지지만 build 는 통과하므로, prefab 을 실제로 불러와 확인한다. 이 test assembly 는
    /// GameScene assembly 를 참조하지 않아서 type 을 이름으로 찾는다.
    /// </summary>
    public class RiverPrefabTests
    {
        [Test]
        public void WaterPrefabIsOneByOneBlueTileBelowUnitsWithBlockingCell()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/RiverWater");
            Assert.IsNotNull(prefab, "Resources/Prefabs/RiverWater.prefab must be loadable.");

            AssertOneByOneTile(prefab.GetComponent<SpriteRenderer>());
            Assert.Less(prefab.GetComponent<SpriteRenderer>().sortingOrder, 0);
            Assert.IsNotNull(FindByTypeName(prefab, "ServedObject"));
            Assert.IsNotNull(FindByTypeName(prefab, "GroundDecalLift"));

            MonoBehaviour cell = FindByTypeName(prefab, "GroundBlockingCell");
            Assert.IsNotNull(cell, "RiverWater must carry GroundBlockingCell.");
            Assert.AreEqual(0.5f, (float)cell.GetType().GetProperty("HalfSize").GetValue(cell), 0.0001f);
        }

        [Test]
        public void BridgePrefabHasWaterTileAndPlanksAboveItButNoBlockingCell()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/RiverBridge");
            Assert.IsNotNull(prefab, "Resources/Prefabs/RiverBridge.prefab must be loadable.");

            SpriteRenderer water = prefab.GetComponent<SpriteRenderer>();
            AssertOneByOneTile(water);
            Assert.Less(water.sortingOrder, 0);
            Assert.IsNotNull(FindByTypeName(prefab, "ServedObject"));
            Assert.IsNull(FindByTypeName(prefab, "GroundBlockingCell"), "A bridge must not block summons.");

            SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>();
            Assert.Greater(renderers.Length, 1, "RiverBridge needs plank renderers on top of the water tile.");
            foreach (SpriteRenderer renderer in renderers)
            {
                Assert.IsNotNull(renderer.sprite);
                Assert.Less(renderer.sortingOrder, 0, "Planks must stay below units.");
                if (renderer != water)
                {
                    Assert.Greater(renderer.sortingOrder, water.sortingOrder, "Planks must draw above the water.");
                }
            }
        }

        private static void AssertOneByOneTile(SpriteRenderer renderer)
        {
            Assert.IsNotNull(renderer);
            Assert.IsNotNull(renderer.sprite);
            Vector3 size = Vector3.Scale(renderer.sprite.bounds.size, renderer.transform.lossyScale);
            Assert.AreEqual(1f, size.x, 0.0001f);
            Assert.AreEqual(1f, size.y, 0.0001f);
        }

        private static MonoBehaviour FindByTypeName(GameObject prefab, string typeName)
        {
            foreach (MonoBehaviour behaviour in prefab.GetComponents<MonoBehaviour>())
            {
                if (behaviour != null && behaviour.GetType().Name == typeName)
                {
                    return behaviour;
                }
            }

            return null;
        }
    }
}
