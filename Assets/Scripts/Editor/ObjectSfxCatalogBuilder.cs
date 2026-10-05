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
        new("TransientExplode", ObjectSfxElement.Neutral, ObjectSfxArchetype.TransientSpell)
    };

    private static readonly CatalogGroup[] CatalogGroups =
    {
        new("NeutralCreature", "ChickenCommando", "Player"),
        new("FireCreature",
            "EmberSpirit", "FireChildSpirit", "FireLordSpirit", "FireSlime",
            "FireSpirit", "FireTadpole", "MagmaSpirit"),
        new("WaterCreature", "AquaArcher", "BubbleSpirit", "SeaSerpent", "WaterSlime"),
        new("NatureCreature",
            "LeafSlime", "PveVineWitch", "SeedSpirit", "TreeGolem", "VineSpirit",
            "WillOWisp"),
        new("LightningCreature",
            "ElectricSlime", "LightningTadpole", "StormRider", "StormStag", "ThunderBird",
            "ThunderSpirit", "ZapMouse"),
        new("RockCreature", "DimensionToad", "RockGolem", "RockMage", "RockSlime"),
        new("WindCreature", "CloudDragon", "WindSlime", "WindSpirit"),
        new("OrganicBuilding",
            "GiantVine", "LifeTree", "PveNatureSlimeNest", "PveVineColony",
            "PveWaterSlimeNest", "SeedNest", "Vine", "VineColony"),
        new("StoneBuilding",
            "Crater", "FireworkTower", "GroundCannon", "GroundTower", "RockTurret"),
        new("ArcaneDevice",
            "BubbleGenerator", "ElectricTower", "FireRune", "FrenzyTotem",
            "HealingTotem", "LightningRune", "ManaWell", "NatureRune",
            "RallyingTotem", "RockRune", "WaterRune", "WindRune", "WindTotem"),
        // Fields and falls stay silent by concept-doc rule; only the release and
        // the impact are audible, and each is one shared sound for every element.
        new("TransientLegacy",
            "CraterEmber", "ElectricField", "FireDrop", "FireField", "LeafField",
            "Leafair", "LightningDrop", "MeteorDrop", "MeteorShower", "MiniRock",
            "NatureDrop", "Overgrowth", "RainCloud", "RazorGale", "RockDrop",
            "RockRemnant", "RockRolling", "SandStorm", "TornadoStrike", "WaterField",
            "WindDrop"),
        new("TransientShot",
            "ChainLightning", "ElectricShot", "FireShot", "LeafShot", "MagmaFist",
            "TideCall", "WaterShot", "WindBlade"),
        new("TransientExplode",
            "ElectricExplode", "FireExplode", "FireworkShell", "LeafExplode",
            "MagmaExplosion", "RockExplode", "ShockOverload", "WaterExplode",
            "WaterExplosion", "WindExplode")
    };

    private static readonly string[] IntentionalSilentRuntimeTypes =
    {
        "ServedObjectHpBar",
        "Towerback"
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
                    false);
            }
        }

        foreach (string runtimeType in IntentionalSilentRuntimeTypes)
        {
            WriteEntry(entries.GetArrayElementAtIndex(index++), runtimeType, null, true);
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
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<ObjectSfxProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            SerializedObject serializedProfile = new(profile);
            serializedProfile.FindProperty("profileId").stringValue = definition.Id;
            serializedProfile.FindProperty("element").enumValueIndex = (int)definition.Element;
            serializedProfile.FindProperty("archetype").enumValueIndex = (int)definition.Archetype;
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
        bool intentionalSilent)
    {
        entry.FindPropertyRelative("runtimeType").stringValue = runtimeType;
        entry.FindPropertyRelative("profile").objectReferenceValue = profile;
        entry.FindPropertyRelative("intentionalSilent").boolValue = intentionalSilent;
        entry.FindPropertyRelative("serverAlias").boolValue = false;
    }

    private static int CountRuntimeTypes()
    {
        int count = IntentionalSilentRuntimeTypes.Length;
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

        public ProfileDefinition(
            string id,
            ObjectSfxElement element,
            ObjectSfxArchetype archetype)
        {
            Id = id;
            Element = element;
            Archetype = archetype;
        }
    }
}
#endif
