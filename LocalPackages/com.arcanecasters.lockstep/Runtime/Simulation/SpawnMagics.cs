using System;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public static class SpawnMagics {
        public static int Quantity(SimulationContext context, string key) => context.GameParameters.TryGetValue(key + ".quantity", out var value) ? (int)(value.RawValue / (1L << 32)) : 1;
        public static MagicImplementation Create(string prefab, string key, Fix64 height = default, Func<Master, Master> getMaster = null) => new MagicImplementation((context, master, origin, position) => {
            int quantity = Quantity(context, key); var side = getMaster == null ? master : getMaster(master);
            for (int i = 0; i < quantity; i++) {
                var x = quantity == 1 ? Fix64.Zero : context.Random.NextFloatFraction() * 2 - 1;
                var z = quantity == 1 ? Fix64.Zero : context.Random.NextFloatFraction() * 2 - 1;
                context.Spawn(side, prefab, new Vector3(position.X + x, height, position.Z + z));
            }
        }, key, height >= 2, prefab, true);
        public static void Register(MagicRegistry registry) {
            registry.Register("aqua_archer", Create("AquaArcher", "aqua_archer"));
            registry.Register("arcane_infantry", Create("ArcaneInfantry", "arcane_infantry"));
            registry.Register("bean_shooter", Create("BeanShooter", "bean_shooter"));
            registry.Register("bomb_sprite", Create("BombSprite", "bomb_sprite", 3));
            registry.Register("bubble_spirit", Create("BubbleSpirit", "bubble_spirit", 3));
            registry.Register("cloud_dragon", Create("CloudDragon", "cloud_dragon", 3));
            registry.Register("crater_carrier", Create("CraterCarrier", "crater_carrier"));
            registry.Register("dimension_toad", Create("DimensionToad", "dimension_toad"));
            registry.Register("ember_spirit_swarm", Create("EmberSpirit", "ember_spirit"));
            registry.Register("evil_ent", Create("EvilEnt", "evil_ent"));
            registry.Register("fire_lord_spirit", Create("FireLordSpirit", "fire_lord_spirit", 3));
            registry.Register("fire_spirit", Create("FireSpirit", "fire_spirit"));
            registry.Register("magma_spirit", Create("MagmaSpirit", "magma_spirit"));
            registry.Register("mini_rock_swarm", Create("MiniRock", "mini_rock"));
            registry.Register("rock_golem", Create("RockGolem", "rock_golem"));
            registry.Register("rock_mage", Create("RockMage", "rock_mage"));
            registry.Register("sea_serpent", Create("SeaSerpent", "sea_serpent"));
            registry.Register("seed_spirit_swarm", Create("SeedSpirit", "seed_spirit"));
            registry.Register("storm_rider", Create("StormRider", "storm_rider"));
            registry.Register("storm_stag", Create("StormStag", "storm_stag"));
            registry.Register("thunder_bird_swarm", Create("ThunderBird", "thunder_bird", 3));
            registry.Register("thunder_spirit", Create("ThunderSpirit", "thunder_spirit", 3));
            registry.Register("tree_golem", Create("TreeGolem", "tree_golem"));
            registry.Register("vine_spirit", Create("VineSpirit", "vine_spirit"));
            registry.Register("wall_golem", Create("WallGolem", "wall_golem"));
            registry.Register("water_slime_swarm", Create("WaterSlime", "water_slime"));
            registry.Register("wind_spirit", Create("WindSpirit", "wind_spirit", 3));
            registry.Register("zap_mouse", Create("ZapMouse", "zap_mouse"));
            registry.Register("tornado_strike", new MagicImplementation((c, side, origin, p) => c.Spawn(side, "TornadoStrike", p)));
            registry.Register("earth_call", new MagicImplementation((c, side, origin, p) => {
                c.Spawn(side, "EarthCall", p);
                foreach (var remnant in SimulationQueries.OverlapSphereAll(c, p, c.GameParameters["earth_call.radius"])) {
                    var size = RockRemnantSizes.FromPrefab(remnant.Prefab); if (!remnant.IsActive || !size.HasValue) continue;
                    var position = remnant.Position; remnant.Destroy(); c.Spawn(side, size.Value.SummonedPrefab(), position);
                }
            }));
        }
    }
}
