using System.Linq;
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
        public void WaterPrefabCutsItsCellFromTheWaterStripAndHasBlockingCell()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/RiverWater");
            Assert.IsNotNull(prefab, "Resources/Prefabs/RiverWater.prefab must be loadable.");

            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer);
            Assert.Less(renderer.sortingOrder, 0);
            Assert.IsNotNull(FindByTypeName(prefab, "ServedObject"));
            Assert.IsNotNull(FindByTypeName(prefab, "GroundDecalLift"));
            AssertStripArt(FindByTypeName(prefab, "RiverCellArt"), renderer);

            MonoBehaviour cell = FindByTypeName(prefab, "GroundBlockingCell");
            Assert.IsNotNull(cell, "RiverWater must carry GroundBlockingCell.");
            Assert.AreEqual(0.5f, (float)cell.GetType().GetProperty("HalfSize").GetValue(cell), 0.0001f);
        }

        [Test]
        public void BridgePrefabHasWaterCellAndDeckAboveItButNoBlockingCell()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/RiverBridge");
            Assert.IsNotNull(prefab, "Resources/Prefabs/RiverBridge.prefab must be loadable.");

            SpriteRenderer water = prefab.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(water);
            Assert.Less(water.sortingOrder, 0);
            Assert.IsNotNull(FindByTypeName(prefab, "ServedObject"));
            Assert.IsNull(FindByTypeName(prefab, "GroundBlockingCell"), "A bridge must not block summons.");

            MonoBehaviour[] arts = prefab.GetComponentsInChildren<MonoBehaviour>()
                .Where(behaviour => behaviour != null && behaviour.GetType().Name == "RiverCellArt").ToArray();
            Assert.AreEqual(2, arts.Length, "RiverBridge needs one art component for the water and one for the deck.");

            SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>();
            Assert.AreEqual(2, renderers.Length);
            foreach (SpriteRenderer renderer in renderers)
            {
                Assert.Less(renderer.sortingOrder, 0, "River art must stay below units.");
                if (renderer != water)
                {
                    Assert.Greater(renderer.sortingOrder, water.sortingOrder, "The deck must draw above the water.");
                }
            }

            foreach (MonoBehaviour art in arts)
            {
                AssertStripArt(art, art.GetComponent<SpriteRenderer>());
            }
        }

        private static void AssertStripArt(MonoBehaviour art, SpriteRenderer expectedTarget)
        {
            Assert.IsNotNull(art, "The prefab must carry RiverCellArt.");
            var serialized = new UnityEditor.SerializedObject(art);
            Assert.AreSame(expectedTarget, serialized.FindProperty("target").objectReferenceValue);
            Sprite strip = serialized.FindProperty("strip").objectReferenceValue as Sprite;
            Assert.IsNotNull(strip, "RiverCellArt needs its strip sprite.");
            // The strip is 2 columns by 10 rows of cells, one cell being pixelsPerUnit pixels wide.
            Assert.AreEqual(2f, strip.bounds.size.x, 0.0001f);
            Assert.AreEqual(10f, strip.bounds.size.y, 0.0001f);
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
