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
        private const string RockShotType = "RockShot";
        private const float BeamSourcePositionTolerance = 0.001f;

        public void Spawn(ProjectileDto dto)
        {
            SpawnShared(dto);
        }

        public static void SpawnShared(ProjectileDto dto, PresentationWorld world = null)
        {
            if (FindAuthoredRockShotSource(dto, world) != null)
            {
                SpawnDeferred(new List<ProjectileDto> { dto }, world);
                return;
            }

            SpawnPresented(dto, world, null);
        }

        private static void SpawnPresented(ProjectileDto dto, PresentationWorld world, Vector3? authoredStart)
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
            
            if (authoredStart.HasValue && projectile is BeamProjectile beam)
            {
                beam.InitFromMouth(dto, authoredStart.Value);
            }
            else if (authoredStart.HasValue && projectile is DefaultProjectile shot)
            {
                shot.InitFromMuzzle(dto, authoredStart.Value);
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

        private static ServedObject FindAuthoredRockShotSource(ProjectileDto dto, PresentationWorld world)
        {
            if (dto?.type != RockShotType || dto.start is not ReferenceProjectileTarget sourceRef)
            {
                return null;
            }

            ServedObject source = PresentationWorld.Find(sourceRef.id, world);
            return source != null && HasAuthoredRockShot(source.PrefabType) ? source : null;
        }

        private static bool HasAuthoredRockShot(string prefab) => prefab is "RockMage" or "RockTurret";

        internal static GroundedUnitPoseController FindReferencedLauncher(ProjectileDto dto, PresentationWorld world)
        {
            string prefab = dto?.type switch
            {
                RockShotType => "Towerback",
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
                    FindAuthoredRockShotSource(projectile, world) != null ||
                    IsSnapshotAuthoredShot(projectile, snapshotObjects))
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

        private static bool IsSnapshotAuthoredShot(ProjectileDto projectile, SnapshotObjectDto[] snapshotObjects)
        {
            return snapshotObjects != null &&
                projectile.start is ReferenceProjectileTarget reference &&
                (projectile.type is "WaterShot" or RockShotType) &&
                System.Array.Exists(snapshotObjects, source => source.id == reference.id &&
                    (projectile.type == RockShotType ? HasAuthoredRockShot(source.prefab) : source.prefab == "WaterSlime"));
        }

        internal static void SpawnDeferred(List<ProjectileDto> projectiles, PresentationWorld world = null,
            IEnumerable<UpdatedObjectDto> updates = null, SnapshotObjectDto[] snapshotObjects = null)
        {
            if (projectiles == null)
            {
                return;
            }

            Dictionary<int, Vector3?> mageStarts = null;
            for (int index = 0; index < projectiles.Count; index++)
            {
                ProjectileDto projectile = projectiles[index];
                ServedObject source = FindAuthoredRockShotSource(projectile, world);
                if (source != null && source.PrefabType == "RockMage")
                {
                    mageStarts ??= new Dictionary<int, Vector3?>();
                    if (!mageStarts.TryGetValue(source.id, out Vector3? muzzle))
                    {
                        muzzle = GetRockMageMuzzle(projectiles, index, source, world, snapshotObjects);
                        mageStarts.Add(source.id, muzzle);
                    }
                    SpawnPresented(projectile, world, muzzle);
                    continue;
                }

                if (source != null)
                {
                    SpawnPresented(projectile, world, GetRockTurretMuzzle(projectile, source, world, snapshotObjects));
                    continue;
                }

                var launcher = FindSeaSerpentLauncher(projectile, updates, world);
                Vector3? mouth = launcher != null &&
                    launcher.TryLaunchProjectile(ProjectileUtil.GetPosition(projectile.end, world), out Vector3 start)
                    ? start
                    : (Vector3?)null;
                SpawnPresented(projectile, world, mouth);
            }
        }

        private static Vector3? GetRockTurretMuzzle(ProjectileDto projectile, ServedObject source,
            PresentationWorld world, SnapshotObjectDto[] snapshotObjects)
        {
            if (source.IsDespawning || !IsInSnapshot(source.id, snapshotObjects))
            {
                return null;
            }

            var launcher = source.GetComponent<GroundedUnitPoseController>();
            if (launcher == null)
            {
                return null;
            }

            Vector3 destination;
            switch (projectile.end)
            {
                case PositionProjectileTarget position:
                    destination = ProjectileUtil.GetPosition(position, world);
                    break;
                case ReferenceProjectileTarget reference:
                    ServedObject target = PresentationWorld.Find(reference.id, world);
                    if (target == null || target.IsDespawning || !IsInSnapshot(reference.id, snapshotObjects))
                    {
                        return null;
                    }
                    destination = target.transform.position;
                    break;
                default:
                    return null;
            }

            return launcher.TryLaunchProjectile(destination, out Vector3 muzzle) ? muzzle : (Vector3?)null;
        }

        private static Vector3? GetRockMageMuzzle(List<ProjectileDto> projectiles, int first, ServedObject mage,
            PresentationWorld world, SnapshotObjectDto[] snapshotObjects)
        {
            var launcher = mage.GetComponent<GroundedUnitPoseController>();
            if (launcher == null || !IsInSnapshot(mage.id, snapshotObjects))
            {
                return null;
            }

            // 첫 탄이 가는 표적이 사라졌으면 나머지 탄을 살핀 뒤 두 탄의 출발점을 함께 정한다.
            Vector3? knownTarget = null;
            Vector3? destination = null;
            for (int i = first; i < projectiles.Count; i++)
            {
                ProjectileDto shot = projectiles[i];
                if (shot.type != RockShotType || shot.start is not ReferenceProjectileTarget source || source.id != mage.id)
                {
                    continue;
                }

                if (shot.end is PositionProjectileTarget)
                {
                    destination = ProjectileUtil.GetPosition(shot.end, world);
                    break;
                }

                if (shot.end is ReferenceProjectileTarget targetRef)
                {
                    ServedObject target = PresentationWorld.Find(targetRef.id, world);
                    if (target == null)
                    {
                        continue;
                    }

                    knownTarget ??= target.transform.position;
                    if (!target.IsDespawning && IsInSnapshot(targetRef.id, snapshotObjects))
                    {
                        destination = target.transform.position;
                        break;
                    }
                }
            }

            return launcher.TryLaunchProjectile(destination ?? knownTarget ?? mage.transform.position, out Vector3 muzzle)
                ? muzzle : (Vector3?)null;
        }

        private static bool IsInSnapshot(int id, SnapshotObjectDto[] snapshotObjects) =>
            snapshotObjects == null || System.Array.Exists(snapshotObjects, entry => entry.id == id);

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
