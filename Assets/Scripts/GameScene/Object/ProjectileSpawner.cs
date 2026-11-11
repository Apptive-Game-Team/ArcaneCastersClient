using GameScene.Dto;
using GameScene.Dto.Projectile;
using GameScene.Object.Projectile;
using GameScene.ServedObjectComponent;
using GameScene.ServedObjectComponent.OnAttack;
using GameScene.ServedObjectComponent.Sound;
using Global;
using UnityEngine;

namespace GameScene.Object
{
    public class ProjectileSpawner : LocalSingletonObject<ProjectileSpawner>
    {
        private const string ShockOverloadSecondaryType = "ShockOverloadSecondary";
        private const string ShockOverloadPrefabPath = "Prefabs/ShockOverload";
        private const float ShockOverloadSecondaryScale = 0.6f;
        private const string WindSpiritBurstType = "WindSpiritBurst";
        private const string WindSpiritBurstPrefabPath = "Prefabs/Effects/WindSpiritBurst";
        private const string SummonReleaseType = "SummonRelease";
        private const string SummonReleasePrefabPath = "Prefabs/Effects/SummonRelease";
        private const string VineWitchRageType = "VineWitchRage";
        private const string VineWitchRagePrefabPath = "Prefabs/Effects/NatureAttackAura";

        public void Spawn(ProjectileDto dto)
        {
            SpawnShared(dto);
        }

        public static void SpawnShared(ProjectileDto dto, PresentationWorld world = null)
        {
            WDebug.Log("ProjectileSpawner Spawn called for type: " + dto.type);

            if (IsPoseOnly(dto.type)) return;

            if (TrySpawnAreaBurst(dto, world)) return;

            if (dto.type == SummonReleaseType || dto.type == VineWitchRageType)
            {
                SpawnProjectionEffect(dto, world);
                return;
            }

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

            if (dto.type == WindSpiritBurstType)
            {
                SpawnWindSpiritBurst(dto, world);
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
            
            if (projectile is DefaultProjectile shot && TryPlayCannonMuzzleFlash(dto, world, out Vector3 origin))
            {
                shot.Init(dto, origin);
                return;
            }

            projectile.Init(dto);
        }

        private static bool TrySpawnAreaBurst(ProjectileDto dto, PresentationWorld world)
        {
            Color32 tint = dto.type switch
            {
                "FireRuneBurst" => new Color32(255, 142, 61, 255),
                "WaterRuneBurst" => new Color32(102, 197, 242, 255),
                "NatureRuneBurst" => new Color32(167, 222, 73, 255),
                "RockRuneBurst" => new Color32(212, 164, 115, 255),
                "LightningRuneBurst" => new Color32(244, 218, 99, 255),
                "WindRuneBurst" => new Color32(163, 224, 207, 255),
                "FrenzyTotemBurst" => new Color32(238, 82, 100, 255),
                _ => default
            };
            if (tint.a == 0) return false;

            GameObject prefab = Resources.Load<GameObject>("Prefabs/Effects/CombatAreaBurst");
            if (prefab == null) return true;

            Vector3 position = ProjectileUtil.GetPosition(dto.end, world) + Vector3.up * 0.04f;
            Quaternion flat = Quaternion.Euler(90f, 0f, 0f);
            GameObject effect = world != null
                ? world.SpawnEffect(prefab, position, world.transform.rotation * flat)
                : Instantiate(prefab, position, flat);
            if (effect == null) return true;

            SpriteRenderer renderer = effect.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                renderer.color = tint;
                // The authored frames share a canvas, so the effect grows within the server's diameter.
                if (dto.width > 0f)
                    effect.transform.localScale *= dto.width / (renderer.sprite.rect.width / renderer.sprite.pixelsPerUnit);
            }

            if (dto.start is ReferenceProjectileTarget reference)
            {
                ServedObject source = PresentationWorld.Find(reference.id, world);
                OnDestroySpawner fallback = source != null ? source.GetComponentInChildren<OnDestroySpawner>() : null;
                if (fallback != null)
                {
                    fallback.SuppressPresentation = true;
                    if (world == null) ServedObjectSfxController.PlaySpawn(fallback.SpawnSfxProfile);
                }
            }
            return true;
        }

        private static void SpawnWindSpiritBurst(ProjectileDto dto, PresentationWorld world)
        {
            GameObject effect = SpawnProjectionEffect(dto, world);
            if (effect == null || !(dto.start is ReferenceProjectileTarget reference)) return;

            ServedObject source = PresentationWorld.Find(reference.id, world);
            if (source == null || !source.TryGetComponent(out OnDestroySpawner fallback)) return;
            fallback.SuppressPresentation = true;
            if (world == null) ServedObjectSfxController.PlaySpawn(fallback.SpawnSfxProfile);
        }

        private static GameObject SpawnProjectionEffect(ProjectileDto dto, PresentationWorld world)
        {
            GameObject prefab = GetPrefab(dto.type);
            if (prefab == null) return null;

            Vector3 position = ProjectileUtil.GetPosition(dto.end, world);
            if (dto.type == VineWitchRageType) position += ProjectileUtil.GetScreenUp(world) * 0.72f;
            Quaternion facing = ProjectileUtil.GetRotation(position, position, world);
            return world != null
                ? world.SpawnEffect(prefab, position, facing)
                : Instantiate(prefab, position, facing);
        }

        private static bool TryPlayCannonMuzzleFlash(ProjectileDto dto, PresentationWorld world, out Vector3 origin)
        {
            origin = default;
            if (dto.type != "RockShot" || !(dto.start is ReferenceProjectileTarget reference))
            {
                return false;
            }

            ServedObject source = PresentationWorld.Find(reference.id, world);
            CannonMuzzleFlash flash = source != null ? source.GetComponent<CannonMuzzleFlash>() : null;
            if (flash == null) return false;

            if (dto.end is PositionProjectileTarget position)
            {
                source.FaceTowards(ProjectileUtil.GetPosition(position, world));
            }
            else if (dto.end is ReferenceProjectileTarget targetReference)
            {
                ServedObject target = PresentationWorld.Find(targetReference.id, world);
                if (target != null) source.FaceTowards(target.transform.position);
            }

            return flash.TryPlay(out origin);
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
            if (impact == null) return;
            if (world == null) ServedObjectSfxController.PlaySpawnForType("RockExplode");
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
            if (effect == null) return true;
            if (world == null) ServedObjectSfxController.PlaySpawnForType("ShockOverload");
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
            string path = type switch
            {
                WindSpiritBurstType => WindSpiritBurstPrefabPath,
                SummonReleaseType => SummonReleasePrefabPath,
                VineWitchRageType => VineWitchRagePrefabPath,
                "ElectricAbsorb" => "Projectiles/ElectricShot",
                _ => $"Projectiles/{type}"
            };
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"Projectile prefab not found: {type}");
            }
            return prefab;
        }

        internal static bool IsPoseOnly(string type)
        {
            return type == "VineWitchCast" || type == "VineWitchFan" ||
                   type == "WindTotemPulse" || type == "GrassGeneratorBloom" || type == "CraterEruption" ||
                   type == "OvergrowthRelease" || type == "RockRemnantConsumed" || type == "SprayRelease" ||
                   type == "BrickGolemSweepPose";
        }
    }
}
