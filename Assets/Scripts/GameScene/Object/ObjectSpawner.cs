using GameScene.Dto;
using GameScene.Exception;
using GameScene.PopupBook;
using GameScene.ServedObjectComponent;
using GameScene.ServedObjectComponent.Sound;
using Global;
using Unity.VisualScripting;
using UnityEngine;

namespace GameScene.Object
{
    public class ObjectSpawner : LocalSingletonObject<ObjectSpawner>
    {
        public void SpawnObject(CreatedObjectDto createdObjectDto, bool playSpawnPresentation = true)
        {
            SpawnShared(createdObjectDto, null, playSpawnPresentation);
        }

        public static ServedObject SpawnShared(CreatedObjectDto createdObjectDto, PresentationWorld world = null, bool playSpawnPresentation = true)
        {
            if (PresentationWorld.Find(createdObjectDto.id, world) != null)
            {
                WDebug.LogWarning($"Object with ID {createdObjectDto.id} already exists. Spawn aborted.");
                return null;
            }

            WDebug.Log($"Spawning object: {createdObjectDto.type}, id: {createdObjectDto.id}");

            GameObject spawnedObject = InstantiateGameObject(createdObjectDto, world);

            if (createdObjectDto.type == "TitanFist")
            {
                TitanFistPresenter.Attach(spawnedObject);
            }

            // Previews pass a world and must not touch the live BGM.
            if (createdObjectDto.boss && world == null)
            {
                BossBgmTrigger.Attach(spawnedObject);
            }

            ServedObject servedObject = spawnedObject.GetOrAddComponent<ServedObject>();
            PopupBookVisualPresenter popupBookPresenter = PopupBookVisualPresenter.Attach(servedObject);

            servedObject.PresentationWorld = world;
            servedObject.SetMaster(createdObjectDto.master);
            servedObject.id = createdObjectDto.id;

            // Gizmos carry server-side shapes such as the detection radius, so they must land
            // before BindListeners: a listener like DetectionRangeMarker reads the radius on bind.
            servedObject.SetGizmos(createdObjectDto.gizmos);

            // Bind once the object is fully configured. Per-creature presentation lives on the
            // prefabs as ServedObjectBehaviour components, so nothing here keys off the object type.
            if (world != null) world.Activate(spawnedObject);
            servedObject.BindListeners();

            WDebug.Log($"Spawned object: {spawnedObject}, master set to: {createdObjectDto.master}, id set to: {createdObjectDto.id}");
            try
            {
                if (world != null) world.Register(servedObject);
                else ObjectContainer.Instance.RegisterObject(servedObject);
                if (world == null) ServedObjectSfxController.Attach(
                    servedObject,
                    createdObjectDto.type,
                    playSpawnPresentation);
                if (playSpawnPresentation)
                {
                    servedObject.NotifySpawned();
                    if (popupBookPresenter != null)
                    {
                        popupBookPresenter.PlaySpawnPresentation(
                            SpawnPresentationTypeCatalog.IsBuilding(createdObjectDto.type));
                    }
                }
            } catch (DuplicatedException e)
            {
                WDebug.LogError($"Failed to register object: {e.Message}");
                Destroy(spawnedObject);
            }
            return servedObject;
        }

        public static GameObject GetPrefab(string type)
        {
            // These legacy server nests never had their own client asset. Use the same
            // explicit compatibility body in live play and previews, not a second renderer.
            string resource = type == "ElectricSummon" || type == "FireSummon" || type == "RockSummon" || type == "WindSummon" ? "SeedNest" : type;
            return Resources.Load<GameObject>($"Prefabs/{resource}");
        }

        private static GameObject InstantiateGameObject(CreatedObjectDto createdObjectDto, PresentationWorld world)
        {
            GameObject spawnedObject;
            GameObject prefab = GetPrefab(createdObjectDto.type);

            WDebug.Log($"Spawning object: {createdObjectDto.type}, prefab found: {prefab != null}");

            if (!prefab)
            {
                if (world != null) throw new System.InvalidOperationException($"Missing runtime prefab: {createdObjectDto.type}");
                spawnedObject = new GameObject(createdObjectDto.type);
                spawnedObject.transform.position = createdObjectDto.position;
            }
            else
            {
                spawnedObject = world != null
                    ? world.InstantiateInactive(prefab, world.ToWorld(createdObjectDto.position), prefab.transform.rotation)
                    : Instantiate(prefab, createdObjectDto.position, prefab.transform.rotation);
            }

            WDebug.Log($"Spawned object: {spawnedObject}, gameObject created at position {createdObjectDto.position}");
            return spawnedObject;
        }
    }
}
