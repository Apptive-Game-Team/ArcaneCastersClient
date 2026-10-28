using System;

namespace ArcaneCasters.Simulation {
    public static class BuildMagics {
        public static MagicImplementation Create(string prefab, string parameterKey, bool grounded = false, Func<Master, Master> getMaster = null) => new MagicImplementation((context, master, origin, position) => {
            context.Spawn(getMaster == null ? master : getMaster(master), prefab, grounded ? GroundVector.Of(position) : position);
        }, parameterKey, summonedPrefab: prefab);
        public static void Register(MagicRegistry registry) {
            registry.Register("cannon", Create("GroundCannon", "ground_cannon", true));
            registry.Register("fire_slime_nest", Create("FireSummon", "fire_summon"));
            registry.Register("healing_totem", Create("HealingTotem", "healing_totem"));
            registry.Register("lightning_slime_nest", Create("ElectricSummon", "electric_summon"));
            registry.Register("mana_well", Create("ManaWell", "mana_well"));
            registry.Register("nature_slime_nest", Create("PveNatureSlimeNest", "pve_nature_slime_nest"));
            registry.Register("pve_water_slime_nest", Create("PveWaterSlimeNest", "pve_water_slime_nest"));
            registry.Register("repair_totem", Create("RepairTotem", "repair_totem"));
            registry.Register("rock_slime_nest", Create("RockSummon", "rock_summon"));
            registry.Register("seed_nest", Create("SeedNest", "seed_nest"));
            registry.Register("titan_remnant", Create("TitanRemnant", "titan_remnant"));
            registry.Register("tower", Create("GroundTower", "ground_tower", true));
            registry.Register("towerback", Create("Towerback", "towerback"));
            registry.Register("bubble_generator", Create("BubbleGenerator", "bubble_generator"));
            registry.Register("crater", Create("Crater", "crater"));
            registry.Register("dragon_tower", Create("DragonTower", "dragon_tower", true));
            registry.Register("electric_tower", Create("ElectricTower", "electric_tower"));
            registry.Register("firework_tower", Create("FireworkTower", "firework_tower"));
            registry.Register("grass_generator", Create("GrassGenerator", "grass_generator"));
            registry.Register("life_tree", Create("LifeTree", "life_tree"));
            registry.Register("rock_turret", Create("RockTurret", "rock_turret"));
            registry.Register("shock_trap", Create("ShockTrap", "shock_trap"));
            registry.Register("wind_totem", Create("WindTotem", "wind_totem"));
            registry.Register("vine_colony", Create("VineColony", "vine_colony"));
            registry.Register("wind_slime_nest", Create("WindSummon", "wind_summon"));
        }
    }
}
