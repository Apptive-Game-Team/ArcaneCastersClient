using GameScene.Dto;
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
        public void WaterPrefabHasLogicComponentsAndNoArtOfItsOwn()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/RiverWater");
            Assert.IsNotNull(prefab, "Resources/Prefabs/RiverWater.prefab must be loadable.");

            AssertNoArt(prefab);
            Assert.IsNotNull(FindByTypeName(prefab, "ServedObject"));
            Assert.IsNotNull(FindByTypeName(prefab, "GroundDecalLift"));
            Assert.IsNotNull(FindByTypeName(prefab, "RiverOverlayMember"));

            MonoBehaviour cell = FindByTypeName(prefab, "GroundBlockingCell");
            Assert.IsNotNull(cell, "RiverWater must carry GroundBlockingCell.");
            Assert.AreEqual(0.5f, (float)cell.GetType().GetProperty("HalfSize").GetValue(cell), 0.0001f);
        }

        [Test]
        public void BridgePrefabHasLogicComponentsButNoBlockingCellAndNoArt()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/RiverBridge");
            Assert.IsNotNull(prefab, "Resources/Prefabs/RiverBridge.prefab must be loadable.");

            AssertNoArt(prefab);
            Assert.IsNotNull(FindByTypeName(prefab, "ServedObject"));
            Assert.IsNotNull(FindByTypeName(prefab, "RiverOverlayMember"));
            Assert.IsNull(FindByTypeName(prefab, "GroundBlockingCell"), "A bridge must not block summons.");
        }

        [Test]
        public void OverlayPrefabHoldsTheWaterAndBridgeSpritesAtTheLayoutScale()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/RiverOverlay");
            Assert.IsNotNull(prefab, "Resources/Prefabs/RiverOverlay.prefab must be loadable.");

            MonoBehaviour overlay = FindByTypeName(prefab, "RiverOverlay");
            Assert.IsNotNull(overlay);
            var serialized = new UnityEditor.SerializedObject(overlay);

            Sprite water = serialized.FindProperty("waterSprite").objectReferenceValue as Sprite;
            Assert.IsNotNull(water, "RiverOverlay needs its water sprite.");
            Assert.AreEqual(RiverOverlayLayout.WaterWidth, water.bounds.size.x, 0.0001f);
            Assert.AreEqual(RiverOverlayLayout.WaterDepth, water.bounds.size.y, 0.0001f);
            // 물 sprite 의 pivot 은 왼쪽 아래라서 bounds 가 (0, 0) 에서 시작한다.
            Assert.AreEqual(0f, water.bounds.min.x, 0.0001f);
            Assert.AreEqual(0f, water.bounds.min.y, 0.0001f);

            Sprite bridge = serialized.FindProperty("bridgeSprite").objectReferenceValue as Sprite;
            Assert.IsNotNull(bridge, "RiverOverlay needs its bridge sprite.");
            Assert.AreEqual(RiverOverlayLayout.BridgeSize, bridge.bounds.size.x, 0.0001f);
            Assert.AreEqual(RiverOverlayLayout.BridgeSize, bridge.bounds.size.y, 0.0001f);
            // 다리 sprite 의 pivot 은 가운데라서 bounds 가 0 을 가운데에 둔다.
            Assert.AreEqual(0f, bridge.bounds.center.x, 0.0001f);
            Assert.AreEqual(0f, bridge.bounds.center.y, 0.0001f);
        }

        /// <summary>
        /// 그림은 overlay 가 그린다. prefab 의 SpriteRenderer 는 ServedObject 와 PopupBookVisualPresenter 가
        /// 찾을 수 있게 root 에 남기되 sprite 는 비워 둔다. 자식 renderer 가 있으면 root 가 아닌 곳이 선택될 수 있다.
        /// </summary>
        private static void AssertNoArt(GameObject prefab)
        {
            SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.AreEqual(1, renderers.Length, "Only the root SpriteRenderer may remain.");
            Assert.AreSame(prefab.GetComponent<SpriteRenderer>(), renderers[0]);
            Assert.IsNull(renderers[0].sprite, "The prefab must not draw the old strips.");
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
