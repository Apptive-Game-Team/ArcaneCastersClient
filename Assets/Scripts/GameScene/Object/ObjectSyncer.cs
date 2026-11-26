using System.Collections.Generic;
using System.Linq;
using Data;
using GameScene.Dto;
using GameScene.Dto.Projectile;
using Global;

namespace GameScene.Object
{
    public class ObjectSyncer : LocalSingletonObject<ObjectSyncer>
    {
        private bool hasCompletedInitialSync;

        public void Sync(SnapshotObjectDto[] snapshotObjects, List<ProjectileDto> deferredProjectiles = null)
        {
            if (snapshotObjects == null)
            {
                WDebug.LogWarning("Snapshot objects array is null. Sync operation aborted.");
                ObjectContainer.Instance.Clear();
                return;
            }
            
            // WDebug.Log("ObjectSyncer Sync called with " + string.Join(" | ", snapshotObjects.Select(dto => $"id: {dto.id}, {dto.prefab}")) + " objects.");
            bool playSpawnPresentation = hasCompletedInitialSync;
            foreach (var snapshotObject in snapshotObjects)
            {
                if (!ObjectContainer.Instance.IsExist(snapshotObject.id))
                {
                    // create
                    ObjectSpawner.Instance.SpawnObject(
                        new CreatedObjectDto(snapshotObject),
                        playSpawnPresentation);
                    ObjectUpdater.Instance.UpdateObject(new UpdatedObjectDto(snapshotObject));
                }
                else
                {
                    // update
                    ObjectUpdater.Instance.UpdateObject(new UpdatedObjectDto(snapshotObject));
                }
            }

            // 같은 틱에 사라지는 발사원과 표적을 참조할 수 있도록 삭제 전에 발사한다.
            if (deferredProjectiles != null)
            {
                var serpentAttacks = snapshotObjects
                    .Where(obj => obj.prefab == "SeaSerpent" && obj.status == "Attack")
                    .Select(obj => new UpdatedObjectDto(obj));
                ProjectileSpawner.SpawnDeferred(deferredProjectiles, updates: serpentAttacks);
            }
            RemoveObjects(snapshotObjects);
            hasCompletedInitialSync = true;
        }

        private void RemoveObjects(SnapshotObjectDto[] snapshotObjects)
        {
            HashSet<int> snapshotIdSet = new HashSet<int>(snapshotObjects.Length);

            foreach (var obj in snapshotObjects)
            {
                snapshotIdSet.Add(obj.id);
            }
            
            var currentIds = ObjectContainer.Instance.GetIds(); 

            foreach (var id in currentIds)
            {
                if (!snapshotIdSet.Contains(id))
                {
                    ObjectContainer.Instance.UnregisterObject(id);
                }
            }
        }
    }
}
