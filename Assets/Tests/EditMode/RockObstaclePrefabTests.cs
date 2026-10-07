using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace WordOnline.Tests
{
    /// <summary>
    /// RockObstacle prefab 은 Editor 없이 손으로 쓴 YAML 이다. script guid 가 어긋나면 component 가
    /// 통째로 사라지지만 build 는 통과하므로, prefab 을 실제로 불러와 확인한다. 이 test assembly 는
    /// GameScene assembly 를 참조하지 않아서 type 을 이름으로 찾는다.
    /// </summary>
    public class RockObstaclePrefabTests
    {
        [Test]
        public void PrefabLoadsWithServedObjectSpriteAndObstacleRadius()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/RockObstacle");
            Assert.IsNotNull(prefab, "Resources/Prefabs/RockObstacle.prefab must be loadable.");

            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer);
            Assert.IsNotNull(renderer.sprite);

            MonoBehaviour obstacle = FindByTypeName(prefab, "PlacementObstacle");
            Assert.IsNotNull(obstacle, "RockObstacle must carry PlacementObstacle.");
            PropertyInfo radius = obstacle.GetType().GetProperty("Radius");
            Assert.IsNotNull(radius);
            Assert.AreEqual(0.6f, (float)radius.GetValue(obstacle), 0.0001f);

            Assert.IsNotNull(FindByTypeName(prefab, "ServedObject"));
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
