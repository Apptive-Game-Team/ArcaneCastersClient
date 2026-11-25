using System.Collections.Generic;
using GameScene.Dto;
using GameScene.Dto.Projectile;
using GameScene.Object.Projectile;
using GameScene.ServedObjectComponent;
using GameScene.ServedObjectComponent.Motion;
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
        private const string SeaSerpentBeamType = "SeaSerpentHydroPump";
        private const string FireSpiritSprayType = "SprayedFlame";
        private const float BeamSourcePositionTolerance = 0.001f;

        public void Spawn(ProjectileDto dto)
        {
            SpawnShared(dto);
        }

        public static void SpawnShared(ProjectileDto dto, PresentationWorld world = null)
        {
            SpawnPresented(dto, world, null);
        }

        private static void SpawnPresented(ProjectileDto dto, PresentationWorld world, Vector3? beamMouth)
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
            
            if (beamMouth.HasValue && projectile is BeamProjectile beam)
            {
                beam.InitFromMouth(dto, beamMouth.Value);
            }
            else
            {
                projectile.Init(dto);
            }

            if (HasFireSpiritSpraySource(dto))
            {
                ServedObject source = PresentationWorld.Find(dto.sourceId.Value, world);
                if (source != null && source.PrefabType == "FireSpirit")
                {
                    Vector3 direction = ProjectileUtil.GetPosition(dto.end, world) -
                                        ProjectileUtil.GetPosition(dto.start, world);
                    source.GetComponent<GroundedUnitPoseController>()?.PlayProjectilePose(source.transform.position + direction);
                }
            }
        }

        private static bool HasFireSpiritSpraySource(ProjectileDto dto) =>
            dto.type == FireSpiritSprayType && dto.sourceId is > 0 &&
            dto.start is PositionProjectileTarget && dto.end is PositionProjectileTarget;

        internal static GroundedUnitPoseController FindReferencedLauncher(ProjectileDto dto, PresentationWorld world)
        {
            string prefab = dto?.type switch
            {
                "RockShot" => "Towerback",
                "WaterShot" => "WaterSlime",
                _ => null
            };
            if (prefab == null || dto.start is not ReferenceProjectileTarget reference)
            {
                return null;
            }

            ServedObject source = PresentationWorld.Find(reference.id, world);
            return source != null && source.PrefabType == prefab
                ? source.GetComponent<GroundedUnitPoseController>()
                : null;
        }

        public static List<ProjectileDto> SpawnBeforeObjectUpdates(IEnumerable<ProjectileDto> projectiles, PresentationWorld world = null) =>
            SpawnBeforeUpdates(projectiles, world, null);

        public static List<ProjectileDto> SpawnBeforeSnapshotUpdates(IEnumerable<ProjectileDto> projectiles,
            SnapshotObjectDto[] snapshotObjects) => SpawnBeforeUpdates(projectiles, null, snapshotObjects);

        private static List<ProjectileDto> SpawnBeforeUpdates(IEnumerable<ProjectileDto> projectiles,
            PresentationWorld world, SnapshotObjectDto[] snapshotObjects)
        {
            if (projectiles == null)
            {
                return null;
            }

            List<ProjectileDto> deferred = null;
            foreach (ProjectileDto projectile in projectiles)
            {
                // 이번 틱의 공격과 위치를 반영한 뒤 포구와 입에서 발사한다.
                if (projectile.type == SeaSerpentBeamType && projectile.start is PositionProjectileTarget ||
                    HasFireSpiritSpraySource(projectile) ||
                    FindReferencedLauncher(projectile, world) != null ||
                    IsSnapshotWaterSlimeShot(projectile, snapshotObjects))
                {
                    deferred ??= new List<ProjectileDto>();
                    deferred.Add(projectile);
                }
                else
                {
                    SpawnShared(projectile, world);
                }
            }

            return deferred;
        }

        private static bool IsSnapshotWaterSlimeShot(ProjectileDto projectile, SnapshotObjectDto[] snapshotObjects)
        {
            return snapshotObjects != null && projectile.type == "WaterShot" &&
                projectile.start is ReferenceProjectileTarget reference &&
                System.Array.Exists(snapshotObjects, source => source.id == reference.id && source.prefab == "WaterSlime");
        }

        internal static void SpawnDeferred(List<ProjectileDto> projectiles, PresentationWorld world = null,
            IEnumerable<UpdatedObjectDto> updates = null)
        {
            if (projectiles == null)
            {
                return;
            }

            foreach (ProjectileDto projectile in projectiles)
            {
                var launcher = FindSeaSerpentLauncher(projectile, updates, world);
                Vector3? mouth = launcher != null &&
                    launcher.TryLaunchProjectile(ProjectileUtil.GetPosition(projectile.end, world), out Vector3 start)
                    ? start
                    : (Vector3?)null;
                SpawnPresented(projectile, world, mouth);
            }
        }

        private static GroundedUnitPoseController FindSeaSerpentLauncher(ProjectileDto projectile,
            IEnumerable<UpdatedObjectDto> updates, PresentationWorld world)
        {
            if (projectile.type != SeaSerpentBeamType || updates == null ||
                !(projectile.start is PositionProjectileTarget origin))
            {
                return null;
            }

            ServedObject matched = null;
            Vector3 groundOrigin = origin.ToVector3();
            foreach (UpdatedObjectDto update in updates)
            {
                if (update.status != "Attack" ||
                    Mathf.Abs(update.position.x - groundOrigin.x) > BeamSourcePositionTolerance ||
                    Mathf.Abs(update.position.z - groundOrigin.z) > BeamSourcePositionTolerance)
                {
                    continue;
                }

                ServedObject source = PresentationWorld.Find(update.id, world);
                if (source == null || source.PrefabType != "SeaSerpent")
                {
                    continue;
                }

                // 좌표만 담긴 투사체의 발사원을 하나로 식별할 수 있을 때만 입에서 그린다.
                if (matched != null && matched != source)
                {
                    return null;
                }
                matched = source;
            }

            return matched != null ? matched.GetComponent<GroundedUnitPoseController>() : null;
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
