using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace WordOnline.Tests
{
    /// <summary>
    /// 폭발 prefab 세 개는 Editor 없이 손으로 고친 YAML 이다. script guid 나 m_AddedComponents
    /// 항목이 어긋나면 component 가 통째로 사라지는데 build 는 그대로 통과하므로, prefab 을
    /// 실제로 불러와서 확인한다. 이 test assembly 는 GameScene assembly 를 참조하지 않아서
    /// type 을 이름으로 찾는다.
    /// </summary>
    public class BlastRadiusScalerPrefabTests
    {
        private const string ScalerTypeName = "BlastRadiusScaler";

        private static readonly string[] BlastPrefabNames =
        {
            "MagmaExplosion",
            "ElectricExplode",
            "ShockOverload",
        };

        [Test]
        public void EveryBlastPrefabCarriesBlastRadiusScalerOnItsRoot()
        {
            foreach (string prefabName in BlastPrefabNames)
            {
                GameObject prefab = Resources.Load<GameObject>($"Prefabs/{prefabName}");
                Assert.IsNotNull(prefab, $"Resources/Prefabs/{prefabName}.prefab must be loadable.");

                MonoBehaviour scaler = FindScaler(prefab);
                Assert.IsNotNull(scaler,
                    $"{prefabName} must carry {ScalerTypeName} on its root, next to ServedObject.");

                FieldInfo referenceRadiusField = scaler.GetType().GetField(
                    "referenceRadius",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(referenceRadiusField,
                    $"{ScalerTypeName} must serialize referenceRadius.");

                float referenceRadius = (float)referenceRadiusField.GetValue(scaler);
                Assert.Greater(referenceRadius, 0f,
                    $"{prefabName} must serialize a positive referenceRadius; " +
                    "0 keeps the prefab scale and the picture stops following the server radius.");
            }
        }

        [Test]
        public void AbstractExplodeDoesNotCarryBlastRadiusScaler()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Abstract/AbstractExplode");
            Assert.IsNotNull(prefab,
                "Resources/Prefabs/Abstract/AbstractExplode.prefab must be loadable.");
            Assert.IsNull(FindScaler(prefab),
                "AbstractExplode has 14 variants including MagmaFist and FireworkShell, " +
                $"so {ScalerTypeName} belongs on the three blast prefabs only.");
        }

        private static MonoBehaviour FindScaler(GameObject prefab)
        {
            return prefab.GetComponents<MonoBehaviour>()
                .FirstOrDefault(component =>
                    component != null && component.GetType().Name == ScalerTypeName);
        }
    }
}
