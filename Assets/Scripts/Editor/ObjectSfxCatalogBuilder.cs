#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Sound.Config;
using UnityEditor;
using UnityEngine;

public static class ObjectSfxCatalogBuilder
{
    private const string ConfigRoot = "Assets/Resources/Sound/Config";
    private const string ProfileRoot = ConfigRoot + "/Profiles";
    private const string CatalogPath = ConfigRoot + "/ObjectSfxCatalog.asset";

    private static readonly ProfileDefinition[] ProfileDefinitions =
    {
        new("NeutralCreature", ObjectSfxElement.Neutral, ObjectSfxArchetype.Creature),
        new("FireCreature", ObjectSfxElement.Fire, ObjectSfxArchetype.Creature),
        new("WaterCreature", ObjectSfxElement.Water, ObjectSfxArchetype.Creature),
        new("NatureCreature", ObjectSfxElement.Nature, ObjectSfxArchetype.Creature),
        new("LightningCreature", ObjectSfxElement.Lightning, ObjectSfxArchetype.Creature),
        new("RockCreature", ObjectSfxElement.Rock, ObjectSfxArchetype.Creature),
        new("WindCreature", ObjectSfxElement.Wind, ObjectSfxArchetype.Creature),
        new("OrganicBuilding", ObjectSfxElement.Nature, ObjectSfxArchetype.Building),
        new("StoneBuilding", ObjectSfxElement.Rock, ObjectSfxArchetype.Building),
        new("ArcaneDevice", ObjectSfxElement.Neutral, ObjectSfxArchetype.Building),
        new("TransientLegacy", ObjectSfxElement.Neutral, ObjectSfxArchetype.TransientSpell),
        new("TransientShot", ObjectSfxElement.Neutral, ObjectSfxArchetype.TransientSpell),
        new("TransientExplode", ObjectSfxElement.Neutral, ObjectSfxArchetype.TransientSpell),
        new("UnitShot", ObjectSfxElement.Neutral, ObjectSfxArchetype.TransientSpell),
        new("CosmeticImpact", ObjectSfxElement.Neutral, ObjectSfxArchetype.TransientSpell),
        new("WaterLaunch", ObjectSfxElement.Neutral, ObjectSfxArchetype.Creature),
        new("FireLaunch", ObjectSfxElement.Neutral, ObjectSfxArchetype.Creature),
        new("SmallAttack", ObjectSfxElement.Neutral, ObjectSfxArchetype.Creature),
        new("SwingAttack", ObjectSfxElement.Neutral, ObjectSfxArchetype.Creature),
        new("TowerAttack", ObjectSfxElement.Neutral, ObjectSfxArchetype.Creature),
        new("CreatureHit", ObjectSfxElement.Neutral, ObjectSfxArchetype.Creature, "Sound/Game/Hit/creature_onhit_1"),
        new("WoodHit", ObjectSfxElement.Neutral, ObjectSfxArchetype.Creature, "Sound/Game/Hit/wood_onhit_1"),
        new("RockHit", ObjectSfxElement.Neutral, ObjectSfxArchetype.Creature, "Sound/Game/Hit/rock_onhit_1")
    };

    private static readonly CatalogGroup[] CatalogGroups =
    {
        new("NeutralCreature", "ArcaneInfantry", "ChickenCommando", "Player"),
        new("FireCreature",
            "CraterCarrier", "EmberSpirit", "FireChildSpirit", "FireLordSpirit", "FireSlime",
            "FireSpirit", "FireTadpole", "MagmaSpirit", "PveFireTadpole"),
        new("WaterCreature", "AquaArcher", "BubbleSpirit", "SeaSerpent", "WaterSlime"),
        new("NatureCreature",
            "BeanShooter", "EvilEnt", "LeafSlime", "PveCorruptedVineSpirit", "PveEvilEnt", "PveVineWitch", "SeedSpirit",
            "TreeGolem", "VineSpirit", "WillOWisp"),
        new("LightningCreature",
            "ElectricSlime", "LightningTadpole", "PveLightningTadpole", "StormRider",
            "StormStag", "ThunderBird", "ThunderSpirit", "ZapMouse"),
        new("RockCreature",
            "DimensionToad", "PveDimensionToad", "RockGolem", "RockMage", "RockSlime", "MiniRock", "WallGolem",
            "PveBrickGolem"),
        new("WindCreature", "CloudDragon", "PveCorruptedWindSpirit", "WindSlime", "WindSpirit", "BombSprite"),
        new("OrganicBuilding",
            "GiantVine", "LifeTree", "PveNatureSlimeNest", "PveVineColony",
            "PveWaterSlimeNest", "SeedNest", "Vine", "VineColony"),
        new("StoneBuilding",
            "Crater", "FireworkTower", "GroundCannon", "GroundTower", "RockTurret",
            "Towerback", "DragonTower", "TitanRemnant"),
        new("ArcaneDevice",
            "BubbleGenerator", "ElectricTower", "FireRune", "FrenzyTotem",
            "HealingTotem", "LightningRune", "ManaWell", "NatureRune",
            "RallyingTotem", "RockRune", "WaterRune", "WindRune", "WindTotem",
            "GrassGenerator", "RepairTotem", "ShockTrap"),
        // Fields and falls remain silent. Unit launches belong to the attacker;
        // standalone spell releases and impacts retain their own spawn profiles.
        new("TransientLegacy",
            "CraterEmber", "ElectricField", "FireDrop", "FireField", "LeafField",
            "Leafair", "LightningDrop", "MeteorDrop", "MeteorShower",
            "NatureDrop", "Overgrowth", "RainCloud", "RazorGale", "RockDrop",
            "RockRemnant", "RockRolling", "SandStorm", "TornadoStrike", "WaterField",
            "WindDrop", "LightningCloud", "MediumRockRemnant", "EarthCall",
            "BoulderStrike", "TitanFist"),
        new("TransientShot",
            "ChainLightning", "TideCall", "WindBlade"),
        new("UnitShot", "CraterShell", "ElectricShot", "FireShot", "LeafShot", "WaterShot", "Bean",
            "MagmaFist", "DragonFlame", "BombSpriteBomb", "GroundTidalWarhead", "TidalWarhead"),
        new("TransientExplode",
            "ElectricExplode", "FireExplode", "FireworkShell", "LeafExplode",
            "CraterExplosion", "MagmaExplosion", "RockExplode", "ShockOverload", "WaterExplode",
            "WaterExplosion", "WindExplode", "BombSpriteExplosion", "TidalWarheadExplosion")
    };

    private static readonly string[] IntentionalSilentRuntimeTypes =
    {
        "ServedObjectHpBar",
        "RiverWater",
        "RiverBridge",
        "RiverOverlay"
    };

    // Defaults follow server attack implementations and database size tags.
    private static readonly CatalogGroup[] AttackGroups =
    {
        new("TowerAttack", "GroundCannon", "GroundTower", "RockTurret", "ElectricTower", "FireworkTower", "DragonTower", "Towerback", "TitanRemnant"),
        new("WaterLaunch", "AquaArcher", "WaterSlime", "BubbleSpirit", "CloudDragon", "BubbleGenerator"),
        new("FireLaunch", "CraterCarrier", "FireChildSpirit", "FireSpirit"),
        new("SmallAttack", "ArcaneInfantry", "ChickenCommando", "EmberSpirit", "FireSlime", "FireTadpole", "PveFireTadpole", "LeafSlime", "SeedSpirit", "VineSpirit", "PveCorruptedVineSpirit", "PveCorruptedWindSpirit", "BeanShooter", "ElectricSlime", "LightningTadpole", "PveLightningTadpole", "ThunderBird", "ThunderSpirit", "ZapMouse", "RockMage", "RockSlime", "MiniRock", "WindSlime", "WindSpirit", "BombSprite"),
        new("SwingAttack", "SeaSerpent", "TreeGolem", "EvilEnt", "PveEvilEnt", "PveVineWitch", "StormRider", "StormStag", "RockGolem", "WallGolem", "PveBrickGolem", "MagmaSpirit"),
    };

    private static readonly string[] ServerAliases =
    {
        "ElectricSummon", "FireSummon", "RockSummon", "WindSummon", "ElectricAbsorb", "LightningDrop"
    };

    // Physical target material, not element: magma armor is rock, a vine witch is
    // a creature, and the four legacy nests use the wooden SeedNest body.
    private static readonly CatalogGroup[] HitGroups =
    {
        new("WoodHit",
            "BeanShooter", "EvilEnt", "PveEvilEnt", "TreeGolem", "VineSpirit",
            "PveCorruptedVineSpirit", "GiantVine", "LifeTree", "PveNatureSlimeNest",
            "PveVineColony", "SeedNest", "Vine", "VineColony", "ElectricSummon",
            "FireSummon", "RockSummon", "WindSummon", "FireRune", "FrenzyTotem",
            "HealingTotem", "ManaWell", "RallyingTotem", "GrassGenerator", "RepairTotem", "ShockTrap"),
        new("RockHit",
            "CraterCarrier", "MagmaSpirit", "DimensionToad", "PveDimensionToad",
            "RockGolem", "RockMage", "RockSlime", "MiniRock", "WallGolem", "PveBrickGolem",
            "Crater", "FireworkTower", "GroundCannon", "GroundTower", "RockTurret",
            "Towerback", "DragonTower", "TitanRemnant", "BubbleGenerator", "ElectricTower",
            "LightningRune", "NatureRune", "RockRune", "WaterRune", "WindRune", "WindTotem"),
        new("CreatureHit",
            "ArcaneInfantry", "ChickenCommando", "Player", "EmberSpirit", "FireChildSpirit",
            "FireLordSpirit", "FireSlime", "FireSpirit", "FireTadpole", "PveFireTadpole",
            "AquaArcher", "BubbleSpirit", "SeaSerpent", "WaterSlime", "LeafSlime",
            "PveVineWitch", "SeedSpirit", "WillOWisp", "ElectricSlime", "LightningTadpole",
            "PveLightningTadpole", "StormRider", "StormStag", "ThunderBird", "ThunderSpirit",
            "ZapMouse", "CloudDragon", "PveCorruptedWindSpirit", "WindSlime", "WindSpirit", "BombSprite")
    };

    [MenuItem("Tools/Sound/Create or Update Baseline Object SFX Catalog")]
    public static void CreateOrUpdate()
    {
        EnsureFolder(ConfigRoot);
        EnsureFolder(ProfileRoot);

        Dictionary<string, ObjectSfxProfile> profiles = CreateOrUpdateProfiles();
        ObjectSfxCatalog catalog = AssetDatabase.LoadAssetAtPath<ObjectSfxCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ObjectSfxCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        // Keep Inspector tuning, including disabled overrides, when rebuilding.
        var existingAttacks = new Dictionary<string, ObjectSfxProfile>(StringComparer.Ordinal);
        var existingHits = new Dictionary<string, ObjectSfxProfile>(StringComparer.Ordinal);
        var existingAliases = new HashSet<string>(StringComparer.Ordinal);
        foreach (ObjectSfxCatalogEntry row in catalog.Entries)
        {
            if (row.AttackProfile != null) existingAttacks[row.RuntimeType] = row.AttackProfile;
            if (row.HitProfile != null) existingHits[row.RuntimeType] = row.HitProfile;
            if (row.ServerAlias) existingAliases.Add(row.RuntimeType);
        }
        var defaultAttacks = new Dictionary<string, ObjectSfxProfile>(StringComparer.Ordinal);
        foreach (CatalogGroup group in AttackGroups)
            foreach (string runtimeType in group.RuntimeTypes)
                defaultAttacks.Add(runtimeType, profiles[group.ProfileId]);
        var defaultHits = new Dictionary<string, ObjectSfxProfile>(StringComparer.Ordinal);
        foreach (CatalogGroup group in HitGroups)
            foreach (string runtimeType in group.RuntimeTypes)
                defaultHits.Add(runtimeType, profiles[group.ProfileId]);

        ObjectSfxProfile HitFor(string runtimeType) =>
            existingHits.TryGetValue(runtimeType, out var tuned) ? tuned :
                defaultHits.TryGetValue(runtimeType, out var baseline) ? baseline : null;

        SerializedObject serializedCatalog = new(catalog);
        SerializedProperty entries = serializedCatalog.FindProperty("entries");
        int entryCount = CountRuntimeTypes();
        entries.arraySize = entryCount;

        int index = 0;
        foreach (CatalogGroup group in CatalogGroups)
        {
            foreach (string runtimeType in group.RuntimeTypes)
            {
                WriteEntry(
                    entries.GetArrayElementAtIndex(index++),
                    runtimeType,
                    profiles[group.ProfileId],
                    false,
                    existingAttacks.TryGetValue(runtimeType, out var attack) ? attack :
                        defaultAttacks.TryGetValue(runtimeType, out var baseline) ? baseline : null,
                    existingAliases.Contains(runtimeType) || Array.IndexOf(ServerAliases, runtimeType) >= 0,
                    HitFor(runtimeType));
            }
        }

        foreach (string runtimeType in IntentionalSilentRuntimeTypes)
        {
            WriteEntry(entries.GetArrayElementAtIndex(index++), runtimeType, null, true);
        }

        foreach (string runtimeType in ServerAliases)
        {
            if (runtimeType == "LightningDrop") continue; // Already in TransientLegacy.
            ObjectSfxProfile aliasProfile = profiles[runtimeType == "ElectricAbsorb" ? "UnitShot" : "OrganicBuilding"];
            WriteEntry(entries.GetArrayElementAtIndex(index++), runtimeType, aliasProfile, false, null, true,
                HitFor(runtimeType));
        }

        serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        List<string> errors = ObjectSfxCatalogValidator.Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Baseline Object SFX catalog was created but validation found {errors.Count} error(s):\n" +
                string.Join("\n", errors));
        }

        Debug.Log($"Created or updated Object SFX catalog with {entryCount} explicit rows.");
    }

    private static Dictionary<string, ObjectSfxProfile> CreateOrUpdateProfiles()
    {
        var profiles = new Dictionary<string, ObjectSfxProfile>(StringComparer.Ordinal);
        foreach (ProfileDefinition definition in ProfileDefinitions)
        {
            string path = $"{ProfileRoot}/{definition.Id}.asset";
            ObjectSfxProfile profile = AssetDatabase.LoadAssetAtPath<ObjectSfxProfile>(path);
            bool created = profile == null;
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<ObjectSfxProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            SerializedObject serializedProfile = new(profile);
            serializedProfile.FindProperty("profileId").stringValue = definition.Id;
            serializedProfile.FindProperty("element").enumValueIndex = (int)definition.Element;
            serializedProfile.FindProperty("archetype").enumValueIndex = (int)definition.Archetype;
            if (created && definition.HitClip != null)
            {
                SerializedProperty hit = serializedProfile.FindProperty("hit");
                hit.FindPropertyRelative("enabled").boolValue = true;
                hit.FindPropertyRelative("clip").objectReferenceValue = Resources.Load<AudioClip>(definition.HitClip);
                hit.FindPropertyRelative("volume").floatValue = 0.55f;
                hit.FindPropertyRelative("pitch").floatValue = 1f;
            }
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            profiles[definition.Id] = profile;
        }

        return profiles;
    }

    private static void WriteEntry(
        SerializedProperty entry,
        string runtimeType,
        ObjectSfxProfile profile,
        bool intentionalSilent,
        ObjectSfxProfile attackProfile = null,
        bool serverAlias = false,
        ObjectSfxProfile hitProfile = null)
    {
        entry.FindPropertyRelative("runtimeType").stringValue = runtimeType;
        entry.FindPropertyRelative("profile").objectReferenceValue = profile;
        entry.FindPropertyRelative("attackProfile").objectReferenceValue = attackProfile;
        entry.FindPropertyRelative("hitProfile").objectReferenceValue = hitProfile;
        entry.FindPropertyRelative("intentionalSilent").boolValue = intentionalSilent;
        entry.FindPropertyRelative("serverAlias").boolValue = serverAlias;
    }

    private static int CountRuntimeTypes()
    {
        int count = IntentionalSilentRuntimeTypes.Length + ServerAliases.Length - 1;
        foreach (CatalogGroup group in CatalogGroups)
        {
            count += group.RuntimeTypes.Length;
        }

        return count;
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] segments = folderPath.Split('/');
        string currentPath = segments[0];
        for (int index = 1; index < segments.Length; index++)
        {
            string nextPath = $"{currentPath}/{segments[index]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, segments[index]);
            }
            currentPath = nextPath;
        }
    }

    private sealed class CatalogGroup
    {
        public readonly string ProfileId;
        public readonly string[] RuntimeTypes;

        public CatalogGroup(string profileId, params string[] runtimeTypes)
        {
            ProfileId = profileId;
            RuntimeTypes = runtimeTypes;
        }
    }

    private sealed class ProfileDefinition
    {
        public readonly string Id;
        public readonly ObjectSfxElement Element;
        public readonly ObjectSfxArchetype Archetype;
        public readonly string HitClip;

        public ProfileDefinition(
            string id,
            ObjectSfxElement element,
            ObjectSfxArchetype archetype,
            string hitClip = null)
        {
            Id = id;
            Element = element;
            Archetype = archetype;
            HitClip = hitClip;
        }
    }
}
#endif
