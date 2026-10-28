using System.Linq;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public interface IBossMagicParser {
        IBossMagic ParseBossMagic(string name);
        IVineFan ParseVineFan(string name);
    }
    public sealed class DatabaseBossMagicParser : IBossMagicParser {
        private readonly DatabaseMagicParser parser;
        public DatabaseBossMagicParser(DatabaseMagicParser parser) { this.parser = parser; }
        public IBossMagic ParseBossMagic(string name) => parser.ParseMagicForBotByName(name);
        public IVineFan ParseVineFan(string name) { var magic = parser.ParseMagicForBotByName(name); return magic != null && magic.IsVineFan ? magic : null; }
    }
    public readonly struct BossSpawnConfig {
        public readonly string Prefab;
        public readonly Fix64 Interval;
        public readonly int Count;
        public BossSpawnConfig(string prefab, Fix64 interval, int count) { Prefab = prefab; Interval = interval; Count = count; }
    }
    public sealed class SimplePveBossInitializer {
        private readonly string parameterKey;
        private readonly Element element;
        private readonly BossSpawnConfig[] spawns;
        private readonly string[] magicNames;
        private readonly bool witch;
        public Fix64 BossSpeed { get; }
        public Fix64 BossAttackInterval { get; }
        public Fix64 BossAttackRange { get; }
        public int TargetMask => 1;
        public SimplePveBossInitializer(string key, Element element, BossSpawnConfig[] spawns, string[] magicNames, bool witch = false, Fix64? speed = null, Fix64? interval = null, Fix64? range = null) {
            parameterKey = key; this.element = element; this.spawns = (BossSpawnConfig[])spawns.Clone(); this.magicNames = (string[])magicNames.Clone(); this.witch = witch;
            BossSpeed = speed ?? Fix64.Zero; BossAttackInterval = interval ?? (Fix64)2.5m; BossAttackRange = range ?? (Fix64)7;
        }
        public void Initialize(SimObject owner) {
            var c = owner.Context; var p = c.GameParameters; int Integer(string name) => (int)(p[parameterKey + "." + name].RawValue / (1L << 32));
            owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddComponent(new ZPhysics(owner)); owner.AddCollider(new CircleCollider(owner, p[parameterKey + ".radius"], true)); owner.Elements.AddNative(element);
            owner.AddComponent(new CommonEffectReceiver(owner));
            var magics = magicNames.Select(name => c.BossMagicParser?.ParseBossMagic(name)).Where(magic => magic != null).ToArray();
            PveBossMob mob = witch ? new VineWitchMob(owner, Integer("hp"), BossSpeed, TargetMask, BossAttackInterval, BossAttackRange, magics, c.BossMagicParser?.ParseVineFan("vine_fan"))
                : new PveBossMob(owner, Integer("hp"), BossSpeed, TargetMask, BossAttackInterval, BossAttackRange, magics);
            // 피해와 HP 게이지가 첫 Mob인 보스에 도달하도록 Spawner보다 먼저 추가한다.
            owner.AddComponent(mob);
            if (spawns.Length == 0) return;
            var spawn = spawns[c.Random.NextBound(spawns.Length)]; owner.AddComponent(new Spawner(owner, 0, spawn.Prefab, spawn.Interval, false, spawn.Count));
        }
    }
    public static class SimplePveBossPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("PveWaterSlimeNest", new SimplePveBossInitializer("pve_water_slime_nest", Element.Water, new[] { new BossSpawnConfig("WaterSlime", 10, 1) }, new string[0]).Initialize);
            context.RegisterPrefab("PveNatureSlimeNest", new SimplePveBossInitializer("pve_nature_slime_nest", Element.Nature, new[] { new BossSpawnConfig("LeafSlime", 10, 1) }, new string[0]).Initialize);
            context.RegisterPrefab("PveVineColony", new SimplePveBossInitializer("vine_colony", Element.Nature, Configs(10), new[] { "vine_toss" }).Initialize);
            context.RegisterPrefab("PveVineWitch", new SimplePveBossInitializer("pve_vine_witch", Element.Nature, Configs(8), new[] { "vine_toss", "vine_colony" }, true, interval: 8).Initialize);
        }
        private static BossSpawnConfig[] Configs(Fix64 interval) => new[] { new BossSpawnConfig("LeafSlime", interval, 2), new BossSpawnConfig("WaterSlime", interval, 2), new BossSpawnConfig("VineSpirit", interval, 2) };
    }
}
