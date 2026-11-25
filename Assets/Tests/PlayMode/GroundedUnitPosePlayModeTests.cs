using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class GroundedUnitPosePlayModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int RenderRate = 120;
        private const float WalkSpeed = 0.5f;

        [UnityTest]
        public IEnumerator TreeGolemWalksAtBothTickRatesAndShowsTheHitBeforeRecovery() => CheckBothTickRates("TreeGolem", 8);

        [UnityTest]
        public IEnumerator RockGolemWalksAtBothTickRatesAndShowsTheHitBeforeRecovery() => CheckBothTickRates("RockGolem", 9);

        [UnityTest]
        public IEnumerator BeanShooterWalksAtBothTickRatesAndFiresWithTheBean() =>
            CheckBothTickRates("BeanShooter", 8, "BeanShooterAttack", 3, 0.6f, 0.08f, "Bean");

        [UnityTest]
        public IEnumerator EvilEntWalksWithBothRootsAndBracesAtBothTickRates() =>
            CheckBothTickRates("EvilEnt", 8, "EvilEnt2", 3, 2f, 0.08f);

        [UnityTest]
        public IEnumerator AquaArcherWalksAndRecoversAtBothTickRates() =>
            CheckBothTickRates("AquaArcher", 8, "AquaArcherAttack", 3, 1f, 0.08f);

        [UnityTest]
        public IEnumerator WallGolemWalksAtProductionSpeedAndPunchesAtBothTickRates() =>
            CheckBothTickRates("WallGolem", 8, "WallGolemPunch01", 4, 0.3f, 0.08f, walkSize: new Vector2(256, 254));

        [UnityTest]
        public IEnumerator TowerbackWalksAtProductionSpeedAndPunchesAtBothTickRates() =>
            CheckBothTickRates("Towerback", 8, "TowerbackPunch01", 4, 0.325f, 0.08f,
                walkSize: new Vector2(256, 256), walkSeconds: 4);

        [UnityTest]
        public IEnumerator SeaSerpentGlidesAtProductionSpeedAtBothTickRates() =>
            CheckBothTickRates("SeaSerpent", 8, "SeaSerpentFire01", 4, .55f, .0875f, walkSeconds: 4,
                idleSprite: "SeaSerpentHead", walkSize: new Vector2(220, 214));

        [UnityTest]
        public IEnumerator MiniRockWalksAtBothTickRatesAndFacesItsHitDuringRecovery() =>
            CheckBothTickRates("MiniRock", 8, "MiniRockPunch01", 4, .5f, .08f,
                walkSize: new Vector2(288, 256), idleSprite: "MiniRockSwarm");

        [UnityTest]
        public IEnumerator StormRiderGlidesAtBothTickRatesAndFacesItsHitDuringRecovery() =>
            CheckBothTickRates("StormRider", 8, "StormRiderThrust01", 5, 3f, .08f,
                walkSize: new Vector2(288, 232));

        [UnityTest]
        public IEnumerator EmberSpiritTrotsAndRecoversAtBothTickRates() =>
            CheckBothTickRates("EmberSpirit", 8, "EmberSpiritSwipe01", 4, .8f, .08f,
                walkSize: new Vector2(304, 176), idleSprite: "EmberSpiritSwarm");

        [UnityTest]
        public IEnumerator SeedSpiritStepsAndRecoversAtBothTickRates() =>
            CheckBothTickRates("SeedSpirit", 8, "SeedSpiritLeafHit01", 4, .8f, .08f,
                walkSize: new Vector2(304, 240), idleSprite: "SeedSpiritSwarm");

        [UnityTest]
        public IEnumerator ZapMouseRunsAndRecoversAtBothTickRates() =>
            CheckBothTickRates("ZapMouse", 8, "ZapMouseSpikeHit01", 4, 1.2f, .08f,
                walkSize: new Vector2(304, 184));

        [UnityTest]
        public IEnumerator FireTadpoleWalksAtItsDefaultSpeedAndFacesTheHitAtBothTickRates() =>
            CheckBothTickRates("FireTadpole", 8, "FireTadpoleStomp01", 4, 1.15f, .08f,
                walkSize: new Vector2(288, 152), idleSprite: "FireTadpoleIdle");

        [UnityTest]
        public IEnumerator LightningTadpoleWalksAtItsDefaultSpeedAndFacesTheHitAtBothTickRates() =>
            CheckBothTickRates("LightningTadpole", 8, "LightningTadpoleZap01", 4, 1.25f, .08f,
                walkSize: new Vector2(224, 280), idleSprite: "LightningTadpoleIdle");

        [UnityTest]
        public IEnumerator ElectricSlimeMovesOnTheGroundAndRecoilsAtBothTickRates() =>
            CheckBothTickRates("ElectricSlime", 8, "ElectricSlimeBump01", 4, .8f, .08f,
                walkSize: new Vector2(128, 111), idleSprite: "ElectricSlimeIdle");

        [UnityTest]
        public IEnumerator FireSlimeMovesOnTheGroundAndRecoilsAtBothTickRates() =>
            CheckBothTickRates("FireSlime", 8, "FireSlimeBump01", 4, .8f, .08f,
                walkSize: new Vector2(128, 108), idleSprite: "FireSlimeIdle");

        [UnityTest]
        public IEnumerator LeafSlimeMovesOnTheGroundAndRecoilsAtBothTickRates() =>
            CheckBothTickRates("LeafSlime", 8, "LeafSlimeBump01", 4, .8f, .08f,
                walkSize: new Vector2(128, 108), idleSprite: "LeafSlimeIdle");

        [UnityTest]
        public IEnumerator RockSlimeMovesOnTheGroundAndRecoilsAtBothTickRates() =>
            CheckBothTickRates("RockSlime", 8, "RockSlimeBump01", 4, .8f, .08f,
                walkSize: new Vector2(128, 105), idleSprite: "RockSlimeIdle");

        [UnityTest]
        public IEnumerator WindSlimeMovesOnTheGroundAndRecoilsAtBothTickRates() =>
            CheckBothTickRates("WindSlime", 8, "WindSlimeBump01", 4, .8f, .08f,
                walkSize: new Vector2(128, 65), idleSprite: "WindSlimeIdle");

        [UnityTest]
        public IEnumerator WaterSlimeMovesAndRecoversFromSpittingAtBothTickRates() =>
            CheckBothTickRates("WaterSlime", 8, "WaterSlimeSpit01", 4, .8f, .08f,
                walkSize: new Vector2(320, 224));

        [UnityTest]
        public IEnumerator VineSpiritStepsWithBothRootsAtBothTickRates() =>
            CheckBothTickRates("VineSpirit", 8, "VineSpiritRelease01", 4, .5f, .08f,
                walkSize: new Vector2(180, 256));

        [UnityTest]
        public IEnumerator MagmaSpiritWalksAndRecoversAtBothTickRates() =>
            CheckBothTickRates("MagmaSpirit", 8, "MagmaSpiritCast01", walkSize: new Vector2(256, 253));

        [UnityTest]
        public IEnumerator FireSpiritWalksAndFinishesSprayingAtBothTickRates() =>
            CheckBothTickRates("FireSpirit", 8, "FireSpiritSpray01", walkSpeed: .6f, secondsPerAttackPose: .125f,
                walkSize: new Vector2(256, 190), walkSeconds: 4);

        [UnityTest]
        public IEnumerator RockMageWalksAndFinishesCastingAtBothTickRates() =>
            CheckBothTickRates("RockMage", 8, "RockMageCast01", walkSpeed: 1f, secondsPerAttackPose: .125f,
                walkSize: new Vector2(234, 256), walkSeconds: 4);

        [UnityTest]
        public IEnumerator RockMageTwinsShareOneMuzzlePerMageAndPerPacket()
        {
            var root = new GameObject("RockMage packet test");
            int oldRate = Time.captureFramerate;
            Time.captureFramerate = RenderRate;
            try
            {
                var world = root.AddComponent(Runtime("GameScene.Object.PresentationWorld"));
                var find = world.GetType().GetMethod("FindById");
                ApplyObjects(world, @"{""create"":[
                    {""id"":41,""type"":""RockMage"",""master"":""LeftPlayer"",""position"":{""x"":4,""z"":5}},
                    {""id"":42,""type"":""RockMage"",""master"":""LeftPlayer"",""position"":{""x"":4,""z"":6}},
                    {""id"":43,""type"":""ElectricSlime"",""master"":""RightPlayer"",""position"":{""x"":7,""z"":5}},
                    {""id"":44,""type"":""ElectricSlime"",""master"":""RightPlayer"",""position"":{""x"":1,""z"":5}}]}");
                var first = (Component)find.Invoke(world, new object[] { 41 });
                var second = (Component)find.Invoke(world, new object[] { 42 });
                Assert.That(first.GetComponent("HoppingMotionController"), Is.Null);
                Assert.That(first.GetComponent("AttackSpriteSwapController"), Is.Null);
                Transform[] Shots() => world.GetComponentsInChildren<Transform>()
                    .Where(child => child.name == "RockShot(Clone)").ToArray();

                ApplyObjects(world, "{\"projectile\":[" + string.Join(",", RockShot(41, 43), RockShot(41, 44),
                    RockShot(42, 44), RockShot(42, 44)) + "]}");
                var rocks = Shots();
                Assert.That(rocks, Has.Length.EqualTo(4));
                Vector3 rightStart = AssertMageMuzzle(first, rocks.Take(2).ToArray(), false);
                Vector3 otherStart = AssertMageMuzzle(second, rocks.Skip(2).ToArray(), true);

                ApplyObjects(world, "{\"projectile\":[" + RockShot(41, 999) + "," + RockShot(41, 44) + "]}");
                var secondPacket = Shots().Skip(4).ToArray();
                Assert.That(secondPacket, Has.Length.EqualTo(2));
                Vector3 leftStart = AssertMageMuzzle(first, secondPacket, true);
                Assert.That(leftStart.x, Is.LessThan(rightStart.x));
                Assert.That(Body(second).flipX, Is.True);

                yield return new WaitForSeconds(.17f);
                Assert.That(rocks[0].position.x, Is.GreaterThan(rightStart.x));
                Assert.That(rocks[1].position.x, Is.LessThan(rightStart.x));
                Assert.That(rocks[2].position.x, Is.LessThan(otherStart.x));
                Assert.That(rocks[3].position, Is.EqualTo(rocks[2].position));
                Assert.That(secondPacket[1].position.x, Is.LessThan(leftStart.x));
            }
            finally
            {
                Time.captureFramerate = oldRate;
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator RockMageSnapshotRegistersTheCasterAndPrefersTheSurvivingTarget()
        {
            var root = new GameObject("RockMage snapshot test");
            var container = root.AddComponent(Runtime("GameScene.Object.ObjectContainer"));
            Transform[] rocks = Array.Empty<Transform>();
            try
            {
                root.AddComponent(Runtime("GameScene.Object.ObjectSpawner"));
                root.AddComponent(Runtime("GameScene.Object.ObjectUpdater"));
                var syncer = root.AddComponent(Runtime("GameScene.Object.ObjectSyncer"));
                Type snapshots = Runtime("GameScene.Dto.SnapshotObjectDto").MakeArrayType();
                var sync = syncer.GetType().GetMethod("Sync");
                object first = Decode(snapshots, @"[
                    {""id"":61,""prefab"":""ElectricSlime"",""master"":""RightPlayer"",""x"":1,""z"":5}]");
                sync.Invoke(syncer, new[] { first, null });
                object current = Decode(snapshots, @"[
                    {""id"":62,""prefab"":""RockMage"",""master"":""LeftPlayer"",""x"":4,""z"":5,""status"":""Attack""},
                    {""id"":63,""prefab"":""ElectricSlime"",""master"":""RightPlayer"",""x"":7,""z"":5}]");
                Type objects = Runtime("GameScene.Dto.ObjectsInfo");
                object envelope = Decode(objects, "{\"projectile\":[" + RockShot(62, 61) + "," + RockShot(62, 63) + "]}");
                object pending = Runtime("GameScene.Object.ProjectileSpawner").GetMethod("SpawnBeforeSnapshotUpdates")
                    .Invoke(null, new[] { objects.GetField("projectile").GetValue(envelope), current });
                Assert.That(((IList)pending).Count, Is.EqualTo(2));
                sync.Invoke(syncer, new[] { current, pending });

                rocks = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                    .Where(child => child.name == "RockShot(Clone)").ToArray();
                Assert.That(rocks, Has.Length.EqualTo(2));
                var find = container.GetType().GetMethod("FindById");
                var mage = (Component)find.Invoke(container, new object[] { 62 });
                Assert.That(find.Invoke(container, new object[] { 61 }), Is.Null);
                AssertMageMuzzle(mage, rocks, false);
            }
            finally
            {
                container.GetType().GetMethod("Clear").Invoke(container, null);
                foreach (Transform shot in rocks) Object.Destroy(shot.gameObject);
                Object.Destroy(root);
            }
            yield return null;
        }

        private static Vector3 AssertMageMuzzle(Component mage, Transform[] rocks, bool flipped)
        {
            SpriteRenderer body = Body(mage);
            Assert.That(body.sprite.name, Is.EqualTo("RockMageCast01"));
            Assert.That(body.flipX, Is.EqualTo(flipped));
            var pose = mage.GetComponent("GroundedUnitPoseController");
            var local = (Vector2)pose.GetType().GetField("projectileMuzzle", Private).GetValue(pose);
            Vector3 expected = body.transform.TransformPoint(new Vector3(flipped ? -local.x : local.x, local.y, 0f));
            foreach (Transform rock in rocks)
                Assert.That(Vector3.Distance(expected, rock.position), Is.LessThan(.002f));
            return expected;
        }

        private static string RockShot(int source, int target) =>
            $"{{\"type\":\"RockShot\",\"duration\":0.5,\"start\":{{\"targetType\":\"reference\",\"id\":{source}}}," +
            $"\"end\":{{\"targetType\":\"reference\",\"id\":{target}}}}}";

        [UnityTest]
        public IEnumerator FireSpiritFollowsItsOwnSprayEvenWhenTargetsDisagree()
        {
            var root = new GameObject("FireSpirit spray test");
            try
            {
                var world = root.AddComponent(Runtime("GameScene.Object.PresentationWorld"));
                var find = world.GetType().GetMethod("FindById");
                ApplyObjects(world, @"{""create"":[
                    {""id"":41,""type"":""FireSpirit"",""master"":""LeftPlayer"",""position"":{""x"":4,""z"":5}},
                    {""id"":42,""type"":""FireSpirit"",""master"":""LeftPlayer"",""position"":{""x"":4,""z"":6}},
                    {""id"":43,""type"":""ElectricSlime"",""master"":""RightPlayer"",""position"":{""x"":7,""z"":5}}]}");
                var first = (Component)find.Invoke(world, new object[] { 41 });
                var second = (Component)find.Invoke(world, new object[] { 42 });
                var left = Body(first);
                var right = Body(second);
                Assert.That(first.GetComponent("HoppingMotionController"), Is.Null);
                Assert.That(first.GetComponent("AttackSpriteSwapController"), Is.Null);
                Assert.That(first.GetComponentsInChildren<SpriteRenderer>().Any(sprite => sprite.name == "FireIdleAura"), Is.True);

                ApplyObjects(world, "{\"projectile\":[" + SprayedFlame(null, 5, 6) + "]}");
                Assert.That(left.sprite.name, Is.EqualTo("FireSpirit"));
                Assert.That(right.sprite.name, Is.EqualTo("FireSpirit"));
                Assert.That(world.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("SprayedFlame")), Is.EqualTo(1));
                ApplyObjects(world, "{\"projectile\":[" + SprayedFlame(41, 3, 2) + "," + SprayedFlame(42, 5, 6) +
                    "],\"update\":[{\"id\":43,\"status\":\"Destroyed\",\"master\":\"RightPlayer\",\"position\":{\"x\":7,\"z\":5}}]}",
                    @"[{""type"":""hit"",""actorId"":41,""targetId"":43},{""type"":""hit"",""actorId"":41,""targetId"":41}]");
                Assert.That(left.sprite.name, Is.EqualTo("FireSpiritSpray01"));
                Assert.That(right.sprite.name, Is.EqualTo("FireSpiritSpray01"));
                yield return null;
                Assert.That(left.flipX, Is.True);
                Assert.That(right.flipX, Is.False);
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator FireSpiritCanFirstAppearWithItsOwnSprayInASnapshot()
        {
            var root = new GameObject("FireSpirit snapshot test");
            var container = root.AddComponent(Runtime("GameScene.Object.ObjectContainer"));
            GameObject spawnedSpray = null;
            try
            {
                root.AddComponent(Runtime("GameScene.Object.ObjectSpawner"));
                root.AddComponent(Runtime("GameScene.Object.ObjectUpdater"));
                var syncer = root.AddComponent(Runtime("GameScene.Object.ObjectSyncer"));
                object snapshot = Decode(Runtime("GameScene.Dto.SnapshotObjectDto").MakeArrayType(), @"[
                    {""id"":51,""prefab"":""FireSpirit"",""master"":""LeftPlayer"",""x"":4,""z"":5},
                    {""id"":52,""prefab"":""FireSpirit"",""master"":""LeftPlayer"",""x"":4,""z"":6}]");
                Type objectsType = Runtime("GameScene.Dto.ObjectsInfo");
                object objects = Decode(objectsType, "{\"projectile\":[" + SprayedFlame(52, 3, 2) + "]}");
                object pending = Runtime("GameScene.Object.ProjectileSpawner").GetMethod("SpawnBeforeSnapshotUpdates")
                    .Invoke(null, new[] { objectsType.GetField("projectile").GetValue(objects), snapshot });
                Assert.That(((IList)pending).Count, Is.EqualTo(1));
                syncer.GetType().GetMethod("Sync").Invoke(syncer, new[] { snapshot, pending });
                spawnedSpray = GameObject.Find("SprayedFlame(Clone)");
                Assert.That(spawnedSpray, Is.Not.Null);
                var find = container.GetType().GetMethod("FindById");
                var first = Body((Component)find.Invoke(container, new object[] { 51 }));
                var second = Body((Component)find.Invoke(container, new object[] { 52 }));
                Assert.That(first.sprite.name, Is.EqualTo("FireSpirit"));
                Assert.That(second.sprite.name, Is.EqualTo("FireSpiritSpray01"));
                Assert.That(second.flipX, Is.True);
            }
            finally
            {
                container.GetType().GetMethod("Clear").Invoke(container, null);
                if (spawnedSpray != null) Object.Destroy(spawnedSpray);
                Object.Destroy(root);
            }
            yield return null;
        }

        private static void ApplyObjects(Component world, string objects, string events = "[]")
        {
            Type eventsType = typeof(List<>).MakeGenericType(Runtime("GameScene.Dto.Event.GameEvent"));
            Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply").Invoke(null, new[]
            {
                Decode(Runtime("GameScene.Dto.ObjectsInfo"), objects), Decode(eventsType, events), world
            });
        }

        private static string SprayedFlame(int? source, int startX, int endX) =>
            "{\"type\":\"SprayedFlame\",\"duration\":0.5," + (source.HasValue ? $"\"sourceId\":{source.Value}," : "") +
            $"\"start\":{{\"targetType\":\"position\",\"x\":{startX},\"y\":0,\"z\":5}}," +
            $"\"end\":{{\"targetType\":\"position\",\"x\":{endX},\"y\":0,\"z\":5}}}}";

        [UnityTest]
        public IEnumerator MagmaSpiritEmergesAndOnlyTheSummonerChangesPoseInAReplay()
        {
            var root = new GameObject("MagmaSpirit replay test");
            int oldRenderRate = Time.captureFramerate;
            Time.captureFramerate = RenderRate;
            try
            {
                var world = root.AddComponent(Runtime("GameScene.Object.PresentationWorld"));
                var find = world.GetType().GetMethod("FindById");
                var apply = Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply");
                Type objectsType = Runtime("GameScene.Dto.ObjectsInfo");
                Type eventsType = typeof(List<>).MakeGenericType(Runtime("GameScene.Dto.Event.GameEvent"));
                void Apply(string objects, string events = "[]") => apply.Invoke(null, new[]
                {
                    Decode(objectsType, objects),
                    Decode(eventsType, events),
                    world
                });

                Apply(@"{""create"":[
                    {""id"":51,""type"":""MagmaSpirit"",""master"":""LeftPlayer"",""position"":{""x"":4,""z"":5}},
                    {""id"":52,""type"":""MagmaSpirit"",""master"":""LeftPlayer"",""position"":{""x"":4,""z"":6}}]}");
                var first = (Component)find.Invoke(world, new object[] { 51 });
                var second = (Component)find.Invoke(world, new object[] { 52 });
                var firstBody = Body(first);
                var secondBody = Body(second);
                Assert.That(first.GetComponent("SpawnSpriteSwapController"), Is.Null);
                Assert.That(first.GetComponent("AttackSpriteSwapController"), Is.Null);
                Assert.That(first.GetComponentsInChildren<SpriteRenderer>().Any(sprite => sprite.name == "FireIdleAura"), Is.True);
                Assert.That(firstBody.sprite.name, Is.EqualTo("MagmaSpiritSpawn01"));

                var pose = first.GetComponent("GroundedUnitPoseController");
                var frames = (Sprite[])pose.GetType().GetField("spawnFrames", Private).GetValue(pose);
                AssertFrames(frames, 4, Resources.Load<Sprite>("Game/sprites/MagmaSpirit"));
                var seen = new List<Sprite>();
                for (int frame = 0; frame < 37; frame++)
                {
                    if (frames.Contains(firstBody.sprite) && !seen.Contains(firstBody.sprite)) seen.Add(firstBody.sprite);
                    yield return null;
                }
                CollectionAssert.AreEqual(frames, seen);
                Assert.That(firstBody.sprite.name, Is.EqualTo("MagmaSpirit"));
                Assert.That(secondBody.sprite, Is.SameAs(firstBody.sprite));

                Apply(@"{""create"":[{""id"":53,""type"":""MagmaFist"",""master"":""LeftPlayer"",""position"":{""x"":7,""z"":5}}],
                    ""update"":[{""id"":51,""status"":""Damaged"",""master"":""LeftPlayer"",""position"":{""x"":4,""z"":5}}]}",
                    @"[{""type"":""summonAttack"",""actorId"":51,""targetId"":53},
                       {""type"":""summonAttack"",""actorId"":52,""targetId"":999}]");
                Assert.That(firstBody.sprite.name, Is.EqualTo("MagmaSpiritCast01"));
                Assert.That(secondBody.sprite.name, Is.EqualTo("MagmaSpirit"));

                Apply(@"{""create"":[
                    {""id"":54,""type"":""MagmaSpirit"",""master"":""LeftPlayer"",""position"":{""x"":4,""z"":7}},
                    {""id"":55,""type"":""MagmaFist"",""master"":""LeftPlayer"",""position"":{""x"":7,""z"":7}}]}",
                    @"[{""type"":""summonAttack"",""actorId"":54,""targetId"":55}]");
                var appearingBody = Body((Component)find.Invoke(world, new object[] { 54 }));
                Assert.That(appearingBody.sprite.name, Is.EqualTo("MagmaSpiritCast01"));
                yield return new WaitForSeconds(.235f);
                Assert.That(appearingBody.sprite.name, Is.EqualTo("MagmaSpirit"));
            }
            finally
            {
                Time.captureFramerate = oldRenderRate;
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator MagmaSpiritReceivesItsSummonWhenTheFistFirstAppearsInASync()
        {
            var root = new GameObject("MagmaSpirit sync test");
            var container = root.AddComponent(Runtime("GameScene.Object.ObjectContainer"));
            var config = Runtime("GameScene.GameConfig");
            int oldRate = (int)config.GetProperty("TickRate").GetValue(null);
            try
            {
                root.AddComponent(Runtime("GameScene.Object.ObjectSpawner"));
                root.AddComponent(Runtime("GameScene.Object.ObjectUpdater"));
                root.AddComponent(Runtime("GameScene.Object.ObjectSyncer"));
                root.AddComponent(Runtime("GameScene.GameSceneUIController"));
                var timer = root.AddComponent(Runtime("GameScene.UI.TimerController"));
                var timerText = new GameObject("Timer", typeof(RectTransform));
                timerText.transform.SetParent(root.transform, false);
                timer.GetType().GetField("timerText", Private)
                    .SetValue(timer, timerText.AddComponent(Runtime("TMPro.TextMeshProUGUI")));
                var handler = Activator.CreateInstance(Runtime("GameScene.Handler.SyncFrameHandler"));
                var apply = handler.GetType().GetMethod("Handler");
                var find = container.GetType().GetMethod("FindById");
                Type syncType = Runtime("GameScene.Dto.SyncFrameInfo");
                const string first = @"{""id"":51,""prefab"":""MagmaSpirit"",""master"":""LeftPlayer"",""x"":4,""z"":5}";
                const string second = @"{""id"":52,""prefab"":""MagmaSpirit"",""master"":""LeftPlayer"",""x"":4,""z"":6}";
                const string fist = @"{""id"":53,""prefab"":""MagmaFist"",""master"":""LeftPlayer"",""x"":7,""z"":5}";
                void Sync(string objects, string events = "[]") => apply.Invoke(handler, new[] {
                    Decode(syncType,
                        "{\"tickRate\":20,\"snapshotResponseDto\":{\"myCards\":[],\"objects\":[" + objects +
                        "]},\"events\":" + events + "}")
                });

                Sync(first + "," + second);
                var firstBody = Body((Component)find.Invoke(container, new object[] { 51 }));
                var secondBody = Body((Component)find.Invoke(container, new object[] { 52 }));
                Assert.That(firstBody.sprite.name, Is.EqualTo("MagmaSpirit"));
                Assert.That(secondBody.sprite, Is.SameAs(firstBody.sprite));
                Assert.That(find.Invoke(container, new object[] { 53 }), Is.Null);

                Sync(first + "," + second + "," + fist, @"[{""type"":""summonAttack"",""actorId"":52,""targetId"":53}]");
                Assert.That(find.Invoke(container, new object[] { 53 }), Is.Not.Null);
                Assert.That(firstBody.sprite.name, Is.EqualTo("MagmaSpirit"));
                Assert.That(secondBody.sprite.name, Is.EqualTo("MagmaSpiritCast01"));
            }
            finally
            {
                config.GetMethod("SetTickRate").Invoke(null, new object[] { oldRate });
                container.GetType().GetMethod("Clear").Invoke(container, null);
                Object.Destroy(root);
            }
            yield return null;
        }

        private static SpriteRenderer Body(Component owner) =>
            (SpriteRenderer)owner.GetType().GetField("_spriteRenderer", Private).GetValue(owner);

        private static object Decode(Type type, string json) =>
            Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize").MakeGenericMethod(type)
                .Invoke(null, new object[] { json });

        [UnityTest]
        public IEnumerator PveEvilEntBracesAndReturnsToItsSharedIdleWithoutMoving()
        {
            var root = new GameObject("PveEvilEnt test");
            int previousRenderRate = Time.captureFramerate;
            Time.captureFramerate = RenderRate;
            try
            {
                var world = root.AddComponent(Runtime("GameScene.Object.PresentationWorld"));
                Type createdType = Runtime("GameScene.Dto.CreatedObjectDto");
                object created = Activator.CreateInstance(createdType);
                createdType.GetField("id").SetValue(created, 42);
                createdType.GetField("type").SetValue(created, "PveEvilEnt");
                createdType.GetField("master").SetValue(created, "RightPlayer");
                createdType.GetField("position").SetValue(created, new Vector3(14f, 0f, 5f));
                var boss = (Component)Runtime("GameScene.Object.ObjectSpawner").GetMethod("SpawnShared")
                    .Invoke(null, new[] { created, world, false });
                var pose = boss.GetComponents<MonoBehaviour>()
                    .Single(component => component.GetType().Name == "GroundedUnitPoseController");
                var renderer = (SpriteRenderer)pose.GetType().GetField("targetRenderer", Private).GetValue(pose);
                Vector3 stationary = boss.transform.position;
                Quaternion upright = renderer.transform.localRotation;
                Assert.That(renderer.sprite.name, Is.EqualTo("EvilEnt"));
                boss.GetType().GetMethod("PlayAttackPresentation").Invoke(boss, null);
                Assert.That(renderer.sprite.name, Is.EqualTo("EvilEnt2"));
                yield return new WaitForSeconds(.10f);
                Assert.That(renderer.sprite.name, Is.EqualTo("EvilEntBrace02"));
                yield return new WaitForSeconds(.08f);
                Assert.That(renderer.sprite.name, Is.EqualTo("EvilEntBrace03"));
                yield return new WaitForSeconds(.10f);
                Assert.That(renderer.sprite.name, Is.EqualTo("EvilEnt"));
                Assert.That(renderer.transform.localRotation, Is.EqualTo(upright));
                Assert.That(boss.transform.position, Is.EqualTo(stationary));
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator CorruptedVineCrawlsOnItsRootAndLashesAtBothTickRates()
        {
            const string prefabName = "PveCorruptedVineSpirit";
            GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
            Assert.That(prefab.GetComponents<MonoBehaviour>()
                .Any(component => component.GetType().Name == "HoppingMotionController"), Is.False);
            Type servedObject = Runtime("GameScene.ServedObjectComponent.ServedObject");
            Assert.That((bool)servedObject.GetField("_swingOnAttack", Private).GetValue(prefab.GetComponent(servedObject)), Is.False);
            yield return CheckBothTickRates(prefabName, 8, prefabName + "Lash01", secondsPerAttackPose: 0.06f);
        }

        private static IEnumerator CheckBothTickRates(string prefabName, int walkPoseCount,
            string attackSprite = null, int attackPoseCount = 4, float walkSpeed = WalkSpeed,
            float secondsPerAttackPose = 0.05f, string shotPrefab = null, Vector2? walkSize = null,
            int walkSeconds = 3, string idleSprite = null)
        {
            var gameConfig = Runtime("GameScene.GameConfig");
            var setTickRate = gameConfig.GetMethod("SetTickRate");
            int previousTickRate = (int)gameConfig.GetProperty("TickRate").GetValue(null);
            int previousRenderRate = Time.captureFramerate;
            try
            {
                Time.captureFramerate = RenderRate;
                foreach (int tickRate in new[] { 20, 60 })
                {
                    setTickRate.Invoke(null, new object[] { tickRate });
                    yield return CheckAtTickRate(prefabName, walkPoseCount, tickRate,
                        attackSprite ?? prefabName + "2", attackPoseCount, walkSpeed, secondsPerAttackPose, shotPrefab,
                        walkSeconds, idleSprite ?? prefabName, walkSize);
                }
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
                setTickRate.Invoke(null, new object[] { previousTickRate });
            }
        }

        private static IEnumerator CheckAtTickRate(string prefabName, int walkPoseCount, int tickRate,
            string attackSprite, int attackPoseCount, float walkSpeed, float secondsPerAttackPose, string shotPrefab,
            int walkSeconds, string idleSprite, Vector2? walkSize)
        {
            int direction = tickRate == 60 ? -1 : 1;
            var root = new GameObject($"{prefabName}-{tickRate}Hz");
            try
            {
                // 재생용 월드가 축소되어도 서버 좌표 거리로 보행 위상을 계산한다.
                root.transform.localScale = Vector3.one * 0.4f;
                var world = root.AddComponent(Runtime("GameScene.Object.PresentationWorld"));
                Type createdType = Runtime("GameScene.Dto.CreatedObjectDto");
                object created = Activator.CreateInstance(createdType);
                createdType.GetField("id").SetValue(created, 42);
                createdType.GetField("type").SetValue(created, prefabName);
                createdType.GetField("master").SetValue(created, "LeftPlayer");
                var spawner = Runtime("GameScene.Object.ObjectSpawner");
                var unit = (Component)spawner.GetMethod("SpawnShared").Invoke(null, new[] { created, world, false });
                var presenter = unit.GetComponents<MonoBehaviour>()
                    .Single(component => component.GetType().Name == "GroundedUnitPoseController");
                Assert.That(unit.GetComponents<MonoBehaviour>()
                    .Any(component => component.GetType().Name == "OnAttackSpriteSwapper"), Is.False);
                var inheritedSwap = unit.GetComponents<MonoBehaviour>()
                    .SingleOrDefault(component => component.GetType().Name == "AttackSpriteSwapController");
                if (inheritedSwap != null)
                {
                    Assert.That(inheritedSwap.GetType().BaseType.GetField("swapSprite", Private).GetValue(inheritedSwap), Is.Null);
                }
                Assert.That(unit.GetComponent("WaddleMotionController"), Is.Null);
                Assert.That(unit.GetComponent("HoppingMotionController"), Is.Null);
                Assert.That(unit.GetComponent("CrawlMotionController"), Is.Null);
                Assert.That(unit.GetType().GetField("_swingOnAttack", Private).GetValue(unit), Is.False);
                var renderer = (SpriteRenderer)presenter.GetType().GetField("targetRenderer", Private).GetValue(presenter);
                Transform body = renderer.transform;
                Vector3 bodyOffset = unit.transform.InverseTransformPoint(body.position);
                Quaternion bodyFacing = Quaternion.Inverse(unit.transform.rotation) * body.rotation;
                void AssertStableBody()
                {
                    Vector3 offset = unit.transform.InverseTransformPoint(body.position);
                    Quaternion facing = Quaternion.Inverse(unit.transform.rotation) * body.rotation;
                    Assert.That(Vector3.Distance(offset, bodyOffset), Is.LessThan(.001f));
                    Assert.That(Quaternion.Angle(facing, bodyFacing), Is.LessThan(.1f));
                }

                var walk = (Sprite[])presenter.GetType().GetField("walkFrames", Private).GetValue(presenter);
                bool isSprayer = prefabName == "FireSpirit";
                bool isRockMage = prefabName == "RockMage";
                bool usesProjectilePose = isSprayer || isRockMage;
                var attack = (Sprite[])presenter.GetType().GetField(usesProjectilePose ? "projectileAttackFrames" : "attackFrames", Private).GetValue(presenter);
                void ApplyProjectilePose()
                {
                    string projection = isSprayer ? SprayedFlame(42, direction * 5, direction * 6)
                        : $"{{\"type\":\"RockShot\",\"duration\":0.5,\"start\":{{\"targetType\":\"reference\",\"id\":42}}," +
                          $"\"end\":{{\"targetType\":\"position\",\"x\":{direction * 6},\"y\":0,\"z\":0}}}}";
                    ApplyObjects(world, "{\"projectile\":[" + projection + "]}");
                }
                Sprite idle = renderer.sprite;
                Sprite hit = Resources.Load<Sprite>($"Game/sprites/{attackSprite}");
                Assert.That(idle.name, Is.EqualTo(idleSprite));
                Assert.That(attack[0], Is.SameAs(hit));
                AssertFrames(walk, walkPoseCount, idle, walkSize);
                AssertFrames(attack, attackPoseCount, hit);
                float attackDuration = secondsPerAttackPose * attackPoseCount;
                Assert.That((float)presenter.GetType().GetField("attackDuration", Private).GetValue(presenter),
                    Is.EqualTo(attackDuration).Within(0.0001f));

                Type updatedType = Runtime("GameScene.Dto.UpdatedObjectDto");
                object frame = Activator.CreateInstance(updatedType);
                FieldInfo position = updatedType.GetField("position");
                FieldInfo status = updatedType.GetField("status");
                MethodInfo update = unit.GetType().GetMethod("UpdateObject");
                var target = Vector3.zero;
                var seenWalk = new List<Sprite>();
                yield return null;
                // 기존 방향 전환 대기 0.5초 뒤에도 보행 주기가 온전히 지나야 한다.
                for (int tick = 0; tick < tickRate * walkSeconds; tick++)
                {
                    target.x += direction * walkSpeed / tickRate;
                    position.SetValue(frame, target);
                    update.Invoke(unit, new[] { frame });
                    for (int render = 0; render < RenderRate / tickRate; render++)
                    {
                        yield return null;
                        if (renderer.sprite != seenWalk.LastOrDefault())
                            seenWalk.Add(renderer.sprite);
                    }
                    AssertStableBody();
                }

                CollectionAssert.IsSubsetOf(walk.Distinct().ToArray(), seenWalk,
                    $"{tickRate}Hz 보행이 모든 고유 자세를 지나야 한다.");
                if (prefabName is "FireSlime" or "VineSpirit" or "MagmaSpirit" or "FireSpirit" or "RockMage")
                {
                    var cycle = walk.Append(walk[0]).ToArray();
                    bool completedCycle = Enumerable.Range(0, Mathf.Max(0, seenWalk.Count - walk.Length))
                        .Any(start => seenWalk.Skip(start).Take(cycle.Length).SequenceEqual(cycle));
                    Assert.That(completedCycle, Is.True, $"{tickRate}Hz의 이동 순서와 반복");
                }
                Assert.That(renderer.flipX, Is.EqualTo(direction < 0));
                status.SetValue(frame, "Attack");
                bool hitOnly = (bool)presenter.GetType().GetField("attackOnHit", Private).GetValue(presenter);
                Action playHit = null;
                if (hitOnly)
                {
                    update.Invoke(unit, new[] { frame });
                    Assert.That(renderer.sprite, Is.Not.SameAs(attack[0]));
                    object defense = Activator.CreateInstance(createdType);
                    createdType.GetField("id").SetValue(defense, 44);
                    createdType.GetField("type").SetValue(defense, "ElectricSlime");
                    createdType.GetField("master").SetValue(defense, "RightPlayer");
                    createdType.GetField("position").SetValue(defense, target - direction * Vector3.right);
                    spawner.GetMethod("SpawnShared").Invoke(null, new[] { defense, world, false });
                    playHit = RecordedHit(world, 42, 44);
                }

                playHit?.Invoke();
                if (usesProjectilePose)
                {
                    ApplyProjectilePose();
                }
                else if (shotPrefab == null)
                {
                    update.Invoke(unit, new[] { frame });
                }
                else
                {
                    ApplyShotFrame(shotPrefab, target, world, frame);
                    Assert.That((Component)world.GetType().GetMethod("FindById").Invoke(world, new object[] { 43 }), Is.Not.Null);
                }
                Assert.That(renderer.sprite, Is.SameAs(attack[0]));
                int nearFirstPoseEnd = Mathf.FloorToInt(secondsPerAttackPose * RenderRate) - 1;
                for (int render = 0; render < nearFirstPoseEnd; render++)
                {
                    yield return null;
                    Assert.That(renderer.sprite, Is.SameAs(attack[0]));
                    AssertStableBody();
                    if (hitOnly) Assert.That(renderer.flipX, Is.EqualTo(direction > 0));
                }
                int secondPoseSample = Mathf.CeilToInt(secondsPerAttackPose * RenderRate) + 2;
                for (int render = nearFirstPoseEnd; render < secondPoseSample; render++)
                {
                    yield return null;
                }
                AssertStableBody();
                Assert.That(renderer.sprite, Is.SameAs(attack[1]));
                playHit?.Invoke();
                update.Invoke(unit, new[] { frame });
                if (usesProjectilePose)
                    ApplyProjectilePose();
                Assert.That(renderer.sprite, Is.SameAs(attack[0]));

                var seenAttack = new List<Sprite>();
                status.SetValue(frame, null);
                int framesThroughRecovery = Mathf.CeilToInt((attackDuration + 0.04f) * RenderRate);
                for (int render = 0; render < framesThroughRecovery; render++)
                {
                    if (render % (RenderRate / tickRate) == 0)
                    {
                        target.x += direction * walkSpeed / tickRate;
                        position.SetValue(frame, target);
                        update.Invoke(unit, new[] { frame });
                    }
                    if (attack.Contains(renderer.sprite) && !seenAttack.Contains(renderer.sprite))
                    {
                        seenAttack.Add(renderer.sprite);
                    }
                    yield return null;
                }
                CollectionAssert.AreEqual(attack, seenAttack);
                Assert.That(walk.Contains(renderer.sprite), Is.True);
                if (hitOnly) Assert.That(renderer.flipX, Is.EqualTo(direction < 0));

                // 20Hz에서는 마지막 보간 0.05초와 이동 유예 0.10초에 렌더 갱신 여유를 더한다.
                yield return new WaitForSeconds(0.18f);
                Assert.That(renderer.sprite, Is.SameAs(idle));
                float walkingHeightTolerance = (float)presenter.GetType().GetField("walkingHeightTolerance", Private).GetValue(presenter);
                if (walkingHeightTolerance > 0f)
                {
                    // 공중에서도 유효한 수평 패킷은 도착하지만 착지 전에는 대기 자세를 쓴다.
                    for (int packet = 0; packet < 4; packet++)
                    {
                        target.x += direction * walkSpeed / tickRate;
                        position.SetValue(frame, target + .16f * Vector3.up);
                        update.Invoke(unit, new[] { frame });
                        yield return new WaitForSeconds(1f / tickRate);
                        Assert.That(renderer.sprite, Is.SameAs(idle));
                    }
                    position.SetValue(frame, target);
                    update.Invoke(unit, new[] { frame });
                    yield return new WaitForSeconds(1f / tickRate);
                    Assert.That(renderer.sprite, Is.SameAs(idle));
                    for (int packet = 0; packet < 4; packet++)
                    {
                        target.x += direction * walkSpeed / tickRate;
                        position.SetValue(frame, target);
                        update.Invoke(unit, new[] { frame });
                        yield return new WaitForSeconds(1f / tickRate);
                    }
                    Assert.That(walk.Contains(renderer.sprite), Is.True);
                }
                float maximumGroundSpeed = (float)presenter.GetType().GetField("maximumGroundSpeed", Private).GetValue(presenter);
                if (maximumGroundSpeed > 0f)
                {
                    // 서버 좌표를 쓰는 프리팹은 큰 위치 갱신이 보간되는 내내 대기 자세를 유지한다.
                    position.SetValue(frame, target + 3f * Vector3.right + Vector3.up);
                    update.Invoke(unit, new[] { frame });
                }
                else
                {
                    unit.transform.position += 3f * Vector3.right + Vector3.up;
                }

                for (int render = 0; render < RenderRate / tickRate + 2; render++)
                {
                    yield return null;
                    Assert.That(renderer.sprite, Is.SameAs(idle));
                }
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        private static Action RecordedHit(Component world, int source, int target)
        {
            Type hitType = Runtime("GameScene.Dto.Event.HitEvent");
            object hit = Activator.CreateInstance(hitType);
            hitType.GetField("actorId").SetValue(hit, source);
            hitType.GetField("targetId").SetValue(hit, target);
            var events = (IList)Activator.CreateInstance(typeof(List<>)
                .MakeGenericType(Runtime("GameScene.Dto.Event.GameEvent")));
            events.Add(hit);
            MethodInfo apply = Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply");
            return () => apply.Invoke(null, new object[] { null, events, world });
        }

        private static void ApplyShotFrame(string shotPrefab, Vector3 position, Component world, object update)
        {
            Type createdType = Runtime("GameScene.Dto.CreatedObjectDto");
            object shot = Activator.CreateInstance(createdType);
            createdType.GetField("id").SetValue(shot, 43);
            createdType.GetField("type").SetValue(shot, shotPrefab);
            createdType.GetField("master").SetValue(shot, "LeftPlayer");
            createdType.GetField("position").SetValue(shot, position);
            update.GetType().GetField("id").SetValue(update, 42);
            update.GetType().GetField("master").SetValue(update, "LeftPlayer");

            Type objectsType = Runtime("GameScene.Dto.ObjectsInfo");
            object objects = Activator.CreateInstance(objectsType);
            Array created = Array.CreateInstance(createdType, 1);
            created.SetValue(shot, 0);
            Array updated = Array.CreateInstance(update.GetType(), 1);
            updated.SetValue(update, 0);
            objectsType.GetField("create").SetValue(objects, created);
            objectsType.GetField("update").SetValue(objects, updated);
            Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply")
                .Invoke(null, new[] { objects, null, world });
        }

        private static void AssertFrames(Sprite[] frames, int count, Sprite reference, Vector2? expectedSize = null)
        {
            Vector2 size = expectedSize ?? reference.rect.size;
            Vector2 pivot = reference.pivot;
            pivot.x += (size.x - reference.rect.width) * 0.5f;

            Assert.That(frames.Distinct().Count(), Is.EqualTo(count));
            foreach (Sprite frame in frames)
            {
                Assert.That(frame, Is.Not.Null);
                Assert.That(frame.rect.size, Is.EqualTo(size));
                Assert.That(frame.pivot, Is.EqualTo(pivot));
                Assert.That(frame.pixelsPerUnit, Is.EqualTo(reference.pixelsPerUnit));
            }
        }

        private static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name)).First(type => type != null);
    }
}
