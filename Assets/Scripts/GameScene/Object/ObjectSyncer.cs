using System.Collections.Generic;
using Data;
using GameScene.Dto;
using Global;

namespace GameScene.Object
{
    public class ObjectSyncer : LocalSingletonObject<ObjectSyncer>
    {
        private bool hasCompletedInitialSync;

        public void Sync(SnapshotObjectDto[] snapshotObjects, bool suppressSpawnPresentation = false)
        {
            if (snapshotObjects == null)
            {
                WDebug.LogWarning("Snapshot objects array is null. Sync operation aborted.");
                ObjectContainer.Instance.Clear();
                return;
            }
            
            // WDebug.Log("ObjectSyncer Sync called with " + string.Join(" | ", snapshotObjects.Select(dto => $"id: {dto.id}, {dto.prefab}")) + " objects.");
            bool playSpawnPresentation = hasCompletedInitialSync && !suppressSpawnPresentation;
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
