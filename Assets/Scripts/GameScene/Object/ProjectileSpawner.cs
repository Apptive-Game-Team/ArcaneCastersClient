using GameScene.Dto;
using GameScene.Dto.Projectile;
using GameScene.Object.Projectile;
using GameScene.ServedObjectComponent;
using Global;
using UnityEngine;

namespace GameScene.Object
{
    public class ProjectileSpawner : LocalSingletonObject<ProjectileSpawner>
    {
        private const string ShockOverloadSecondaryType = "ShockOverloadSecondary";
        private const string ShockOverloadPrefabPath = "Prefabs/ShockOverload";
        private const float ShockOverloadSecondaryScale = 0.6f;

        public void Spawn(ProjectileDto dto)
        {
            SpawnShared(dto);
        }

        public static void SpawnShared(ProjectileDto dto, PresentationWorld world = null)
        {
            WDebug.Log("ProjectileSpawner Spawn called for type: " + dto.type);

            if (dto.type == "BoulderStrikeImpact")
            {
                SpawnBoulderStrikeImpact(dto, world);
                return;
            }

            if (dto.type == "SpiritBombBeam")
            {
                SpawnSpiritBombBeam(dto, world);
                return;
            }

            if (ShouldSuppressStormStagImpactProjectile(dto, world))
            {
                WDebug.Log("Suppressed ElectricShot visual for Storm Stag charge impact.");
                return;
            }

            if (TrySpawnShockOverloadSecondary(dto, world))
            {
                return;
            }

            GameObject prefabs = GetPrefab(dto.type);
            
            if (prefabs == null) return;
            
            GameObject projectileObject = world != null ? world.InstantiateInactive(prefabs, Vector3.zero, prefabs.transform.rotation) : Instantiate(prefabs);
            if (world != null) world.Activate(projectileObject);

            IProjectile projectile = projectileObject.GetComponent<IProjectile>();
            
            Destroy(projectileObject, dto.duration);
            
            projectile.Init(dto);
        }

        private static void SpawnBoulderStrikeImpact(ProjectileDto dto, PresentationWorld world)
        {
            GameObject impactPrefab = Resources.Load<GameObject>("Prefabs/RockExplode");
            if (impactPrefab == null)
            {
                Debug.LogError("RockExplode prefab not found for BoulderStrikeImpact.");
                return;
            }

            GameObject impact = world != null ? world.SpawnEffect(impactPrefab, ProjectileUtil.GetPosition(dto.start, world), impactPrefab.transform.rotation) : Instantiate(
                impactPrefab,
                ProjectileUtil.GetPosition(dto.start),
                impactPrefab.transform.rotation);
            impact.transform.localScale *= 0.65f;
            Destroy(impact, dto.duration);
        }

        private static void SpawnSpiritBombBeam(ProjectileDto dto, PresentationWorld world)
        {
            GameObject projectileObject = new GameObject("SpiritBombBeam");
            if (world != null) world.Own(projectileObject);
            SpiritBombBeamProjectile projectile = projectileObject.AddComponent<SpiritBombBeamProjectile>();
            Destroy(projectileObject, dto.duration);
            projectile.Init(dto);
        }

        private static bool TrySpawnShockOverloadSecondary(ProjectileDto dto, PresentationWorld world)
        {
            if (!string.Equals(dto.type, ShockOverloadSecondaryType, System.StringComparison.Ordinal))
            {
                return false;
            }

            GameObject prefab = Resources.Load<GameObject>(ShockOverloadPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Projectile prefab not found: {dto.type}");
                return true;
            }

            Vector3 position = ProjectileUtil.GetPosition(dto.start, world);
            GameObject effect = world != null ? world.SpawnEffect(prefab, position, prefab.transform.rotation) : Instantiate(prefab, position, prefab.transform.rotation);
            SpriteRenderer renderer = effect.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.transform.localScale *= ShockOverloadSecondaryScale;
            }

            Destroy(effect, dto.duration);
            return true;
        }

        private static bool ShouldSuppressStormStagImpactProjectile(ProjectileDto dto, PresentationWorld world)
        {
            if (!(dto.start is ReferenceProjectileTarget sourceReference))
            {
                return false;
            }

            ServedObject source = PresentationWorld.Find(sourceReference.id, world);
            return source != null
                && StormStagChargeImpactRules.ShouldSuppressProjectile(dto.type, source.ActiveEffects);
        }
        
        
        public static GameObject GetPrefab(string type)
        {
            string resourceType = type == "ElectricAbsorb" ? "ElectricShot" : type;
            GameObject prefab = Resources.Load<GameObject>($"Projectiles/{resourceType}");
            if (prefab == null)
            {
                Debug.LogError($"Projectile prefab not found: {type}");
            }
            return prefab;
        }
    }
}
