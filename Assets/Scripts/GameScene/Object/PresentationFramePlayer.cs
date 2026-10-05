using System.Collections.Generic;
using GameScene.Dto;
using GameScene.Dto.Event;
using GameScene.Handler;

namespace GameScene.Object
{
    /// <summary>The single presentation dispatch order for live frames and offline recordings.</summary>
    public static class PresentationFramePlayer
    {
        private static readonly GameEventHandler eventHandler = new();
        public static void Apply(ObjectsInfo objects, List<GameEvent> events, PresentationWorld world = null)
        {
            if (objects?.create != null)
                foreach (CreatedObjectDto created in objects.create) ObjectSpawner.SpawnShared(created, world);
            eventHandler.Handler(events, world);
            if (objects?.projectile != null)
                foreach (var projectile in objects.projectile) ProjectileSpawner.SpawnShared(projectile, world);
            if (objects?.update != null)
                foreach (UpdatedObjectDto updated in objects.update) PresentationWorld.Find(updated.id, world)?.UpdateObject(updated);
        }
    }
}
