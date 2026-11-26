using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace WordOnline.Tests
{
    /// <summary>
    /// 이 폭발 prefab들은 손으로 고친 YAML이다. script GUID나 m_AddedComponents
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
            "RockExplode",
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
                $"so {ScalerTypeName} belongs on individual blast variants.");
        }

        [Test]
        public void RockExplodeUsesOneRegisteredSequenceOnTheServedRenderer()
        {
            const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var prefab = Resources.Load<GameObject>("Prefabs/RockExplode");
            var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.IsFalse(components.Any(component => component.GetType().Name == "PopSelfController"));

            var owner = components.Single(component => component.GetType().Name == "ServedObject");
            var sequence = components.Single(component => component.GetType().Name == "SpriteFrameAnimator");
            var scaler = FindScaler(prefab);
            Assert.AreSame(owner, scaler.GetType().BaseType.GetField("servedObject", Fields).GetValue(scaler));
            Assert.That(scaler.GetType().GetField("referenceRadius", Fields).GetValue(scaler), Is.EqualTo(.88f));
            Assert.IsTrue((bool)owner.GetType().GetField("_keepUprightOnSpawn", Fields).GetValue(owner));

            var renderer = (SpriteRenderer)sequence.GetType().GetField("spriteRenderer", Fields).GetValue(sequence);
            Assert.AreSame(owner.GetType().GetField("_spriteRenderer", Fields).GetValue(owner), renderer);
            Assert.IsFalse((bool)sequence.GetType().GetField("loop", Fields).GetValue(sequence));
            Assert.That(sequence.GetType().GetField("frameInterval", Fields).GetValue(sequence), Is.EqualTo(.02f));

            var frames = (Sprite[])sequence.GetType().GetField("frames", Fields).GetValue(sequence);
            var slots = new[] { 10, 8, 6, 4, 1, 1 };
            var expected = slots.SelectMany((count, index) => Enumerable.Repeat($"RockBurst{index + 1:00}", count));
            CollectionAssert.AreEqual(expected, frames.Select(frame => frame.name));
            Assert.AreSame(frames[0], renderer.sprite);

            foreach (Sprite frame in frames.Distinct())
            {
                Assert.AreEqual(new Vector2(192, 124), frame.rect.size);
                Assert.That(frame.pivot.x, Is.EqualTo(96).Within(.01f));
                Assert.That(frame.pivot.y, Is.EqualTo(2).Within(.01f));
                Assert.AreEqual(100, frame.pixelsPerUnit);
            }
        }

        [Test]
        public void GroundSpawnedExplodeSpriteUsesBottomCenterPivot()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Effects/Explode");
            Assert.IsNotNull(prefab, "Resources/Prefabs/Effects/Explode.prefab must be loadable.");

            SpriteRenderer renderer = prefab.GetComponentInChildren<SpriteRenderer>();
            Assert.IsNotNull(renderer, "Effects/Explode must carry a SpriteRenderer.");
            Assert.IsNotNull(renderer.sprite, "Effects/Explode must carry a sprite.");

            Assert.AreEqual(0f, renderer.sprite.pivot.y, 0.01f,
                "Effects/Explode is instantiated at a ground position. A centered pivot puts the " +
                "lower half below the battlefield, so the sprite must rise from a bottom-center pivot.");
        }

        private static MonoBehaviour FindScaler(GameObject prefab)
        {
            return prefab.GetComponents<MonoBehaviour>()
                .FirstOrDefault(component =>
                    component != null && component.GetType().Name == ScalerTypeName);
        }
    }
}
