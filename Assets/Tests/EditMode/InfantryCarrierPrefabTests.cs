using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class InfantryCarrierPrefabTests
    {
        [TestCase("ArcaneInfantry")]
        [TestCase("CraterCarrier")]
        [TestCase("CraterShell")]
        [TestCase("CraterExplosion")]
        public void ServerBodiesResolveWithValidScriptsAndSprites(string name)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/" + name);
            Assert.NotNull(prefab);
            Assert.AreEqual(name, prefab.name);
            Assert.IsTrue(prefab.GetComponentsInChildren<MonoBehaviour>(true)
                .All(component => component != null), "Missing script on " + name);
            Assert.IsTrue(prefab.GetComponentsInChildren<SpriteRenderer>(true)
                .Any(renderer => renderer.sprite != null), "Missing sprite on " + name);
        }

        [Test]
        public void CarrierOnlyUsesTheServerPhasePresenterAndShellDoesNotSpawnADuplicateImpact()
        {
            GameObject carrier = Resources.Load<GameObject>("Prefabs/CraterCarrier");
            string[] components = carrier.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(component => component != null).Select(component => component.GetType().Name).ToArray();
            CollectionAssert.Contains(components, "CraterCarrierPresenter");
            CollectionAssert.DoesNotContain(components, "AttackSpriteSwapController");
            CollectionAssert.DoesNotContain(components, "HoppingMotionController");
            GameObject shell = Resources.Load<GameObject>("Prefabs/CraterShell");
            Assert.IsFalse(shell.GetComponents<MonoBehaviour>().Any(component =>
                component != null && component.GetType().Name == "OnDestroySpawner"));
        }

        [Test]
        public void NetworkStatusesDriveBoundCarrierIncludingReconnectAndDamageRecovery()
        {
            GameObject source = Resources.Load<GameObject>("Prefabs/CraterCarrier");
            MonoBehaviour authored = source.GetComponents<MonoBehaviour>()
                .Single(component => component != null && component.GetType().Name == "CraterCarrierPresenter");
            Type ownerType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("GameScene.ServedObjectComponent.ServedObject"))
                .First(type => type != null);
            GameObject instance = new GameObject("carrier-phase-test");
            try
            {
                Component owner = instance.AddComponent(ownerType);
                SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
                Component presenter = instance.AddComponent(authored.GetType());
                Set(presenter, "targetRenderer", renderer);
                foreach (string name in new[] { "carriedSprite", "deployedSprite", "recoveringSprite" })
                    Set(presenter, name, Get(authored, name));
                presenter.GetType().GetMethod("Bind").Invoke(presenter, new object[] { owner });
                MethodInfo update = ownerType.GetMethod("UpdateObject");
                Type dtoType = update.GetParameters()[0].ParameterType;
                Action<string> status = value =>
                {
                    object dto = Activator.CreateInstance(dtoType);
                    dtoType.GetField("status").SetValue(dto, value);
                    foreach (string field in new[] { "effects", "gauges" })
                    {
                        FieldInfo entry = dtoType.GetField(field);
                        entry.SetValue(dto, Activator.CreateInstance(entry.FieldType));
                    }
                    update.Invoke(owner, new[] { dto });
                };

                // An initial full snapshot can begin already deployed or already packing.
                status("Deployed");
                Assert.AreSame(Get(authored, "deployedSprite"), renderer.sprite);
                status("Attack");
                Assert.AreSame(Get(authored, "deployedSprite"), renderer.sprite);
                status("Damaged");
                status("Recovering");
                Assert.AreSame(Get(authored, "recoveringSprite"), renderer.sprite);
                status("Idle");
                Assert.AreSame(Get(authored, "carriedSprite"), renderer.sprite);
                status("Deploying");
                Assert.AreSame(Get(authored, "recoveringSprite"), renderer.sprite);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [TestCase("ArcaneInfantry", "ArcaneInfantryAttacking")]
        [TestCase("CraterCarrier", "CraterCarrierDeployed")]
        [TestCase("CraterCarrier", "CraterCarrierRecovering")]
        public void PoseImportsKeepTheSameCanvasScaleAndFootPivot(string idle, string other)
        {
            Sprite a = Resources.Load<Sprite>("Game/sprites/" + idle);
            Sprite b = Resources.Load<Sprite>("Game/sprites/" + other);
            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.AreEqual(a.rect.size, b.rect.size);
            Assert.AreEqual(a.pixelsPerUnit, b.pixelsPerUnit);
            Assert.AreEqual(a.pivot, b.pivot);
            Assert.AreEqual(0, a.pivot.y);
        }

        private static object Get(object target, string field) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
