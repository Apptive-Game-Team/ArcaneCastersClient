using Global;
using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    public static class DamagedObjectEffect
    {
        public static void SetSelfDestroyEffect(string effect, Transform tr)
        {
            SetSelfDestroyEffect(effect, tr.position, GameScene.Object.PresentationWorld.For(tr));
        }

        public static void SetSelfDestroyEffect(string effect, Vector3 worldPosition, GameScene.Object.PresentationWorld world = null)
        {
            GameObject effectPrefab = (GameObject) Resources.Load($"Prefabs/Effects/{effect}");

            if (effectPrefab == null)
            {
                WDebug.LogWarning($"Effect prefab '{effect}' not found.");
                return;
            }

            if (world != null) world.SpawnEffect(effectPrefab, worldPosition, Quaternion.identity);
            else UnityEngine.Object.Instantiate(effectPrefab, worldPosition, Quaternion.identity);
        }
    }
}
