using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BombSpriteParityTests {
        private const string Source = "game/domain/object/component/magic/bomb_sprite_bomb.rs";
        private static SimulationContext Context() {
            var c = new SimulationContext { DeltaTime = (Fix64)0.25m }; c.InitializationFailed += (o, e) => throw e; c.RegisterPrefab("Object", _ => { }); BombSpritePrefabs.Register(c);
            c.GameParameters["bomb_sprite_bomb.damage"] = 40; c.GameParameters["bomb_sprite_bomb.speed"] = 10; c.GameParameters["bomb_sprite_bomb.radius"] = 3; return c;
        }
        private static DummyMob Target(SimulationContext c, Master side, Vector3 position) {
            var o = c.Spawn(side, "Object", position); o.AddCollider(new CircleCollider(o, (Fix64)0.25m)); var mob = new DummyMob(o, 100); o.AddComponentImmediately(mob); return mob;
        }
        [PortedTest(Source)] public static void explodes_at_ground_target_and_damages_enemies_only() {
            var c = Context(); var bomb = c.Spawn(Master.LeftPlayer, "BombSpriteBomb", new Vector3(0, 2, 0)); var enemy = Target(c, Master.RightPlayer, new Vector3(1, 0, 0)); var ally = Target(c, Master.LeftPlayer, new Vector3(1, 0, 1)); c.FlushObjects();
            var flight = bomb.GetComponent<BombSpriteBomb>(); flight.SetTarget(Vector3.Zero); var before = c.PendingObjects.Length; flight.Update();
            Assert.Equal(60, enemy.Hp); Assert.Equal(100, ally.Hp); Assert.True(bomb.IsDestroyed); Assert.Equal(before + 1, c.PendingObjects.Length); Assert.Equal("BombSpriteExplosion", c.PendingObjects.Last().Prefab);
        }
        public static void CheckActualBomber() {
            var c = Context(); c.GameParameters["bomb_sprite_bomb.speed"] = 4;
            c.GameParameters["bomb_sprite.mass"] = 1; c.GameParameters["bomb_sprite.radius"] = (Fix64)0.5m; c.GameParameters["bomb_sprite.hp"] = 100; c.GameParameters["bomb_sprite.speed"] = 2;
            c.GameParameters["bomb_sprite.attack_interval"] = 1; c.GameParameters["bomb_sprite.attack_range"] = 2;
            var registry = new MagicRegistry(); SpawnMagics.Register(registry); registry.GetBean("bomb_sprite").Bind(1, "bomb_sprite", Element.Wind).Run(c, Master.RightPlayer, null, new Vector3(5, 0, 5));
            var owner = c.PendingObjects.Single(); var mob = owner.GetComponent<BombSpriteMob>(); Assert.Equal((Fix64)3, owner.Position.Y); Assert.Equal(1, mob.TargetMask); Assert.True(mob.VerticalRangeIgnored && owner.GetComponent<ZPhysics>().CanHover); Assert.True(owner.Elements.Has(Element.Wind) && owner.HasComponent<CommonEffectReceiver>());
            var target = Target(c, Master.LeftPlayer, new Vector3(5, 0, 5)); var air = Target(c, Master.LeftPlayer, new Vector3(5, 2, 5)); var ally = Target(c, Master.RightPlayer, new Vector3(5, 0, 5)); var far = Target(c, Master.LeftPlayer, new Vector3(15, 0, 5)); var dead = Target(c, Master.LeftPlayer, new Vector3(5, 0, 5)); c.FlushObjects(); dead.Owner.Destroy();
            var second = new DummyMob(target.Owner, 100); target.Owner.AddComponentImmediately(second); Assert.True(!mob.Behavior(dead.Owner)); Assert.Equal(0, c.PendingObjects.Length); Assert.True(mob.Behavior(target.Owner)); Assert.Equal(ObjectStatus.Attack, owner.Status);
            var bomb = c.PendingObjects.Single(); var flight = bomb.GetComponent<BombSpriteBomb>(); Assert.Equal(Master.RightPlayer, bomb.Master); Assert.Equal(0, bomb.Colliders.Length); owner.Destroy();
            flight.Update(); Assert.Near((Fix64)2, bomb.Position.Y, Fix64.FromRaw(2)); Assert.Equal(100, target.Hp); flight.Update(); Assert.Near((Fix64)1, bomb.Position.Y, Fix64.FromRaw(2)); Assert.True(!bomb.IsDestroyed); flight.Update(); Assert.True(bomb.IsDestroyed); Assert.Equal(new Vector3(5, 0, 5), bomb.Position);
            Assert.Equal(60, target.Hp); Assert.Equal(60, second.Hp); Assert.Equal(60, air.Hp); Assert.Equal(100, ally.Hp); Assert.Equal(100, far.Hp); Assert.Equal(100, dead.Hp);
            var hits = c.DrainEvents().Where(e => e.Type == "hit").ToArray(); Assert.Equal(3, hits.Length); foreach (var hit in hits) Assert.Equal(bomb.Id, hit.SourceId);
            flight.Update(); Assert.Equal(60, target.Hp); Assert.Equal(1, c.PendingObjects.Count(o => o.Prefab == "BombSpriteExplosion")); var display = c.PendingObjects.Single(o => o.Prefab == "BombSpriteExplosion"); Assert.Equal(0, display.Colliders.Length); Assert.Equal(Master.RightPlayer, display.Master); c.DeltaTime = (Fix64)0.6m; display.Update(); Assert.True(display.IsDestroyed); Assert.Equal(60, target.Hp);
            var minimum = c.Spawn(Master.RightPlayer, "BombSpriteBomb", new Vector3(12, 0, 5)); var minimumFlight = minimum.GetComponent<BombSpriteBomb>(); c.DeltaTime = (Fix64)0.05m; minimumFlight.Update(); Assert.True(!minimum.IsDestroyed);
            minimumFlight.SetTarget(new Vector3(12, 8, 5)); minimumFlight.Update(); Assert.True(!minimum.IsDestroyed); c.DeltaTime = (Fix64)0.1m - c.DeltaTime; minimumFlight.Update(); Assert.True(minimum.IsDestroyed); Assert.Equal(new Vector3(12, 0, 5), minimum.Position);
        }
    }
}
