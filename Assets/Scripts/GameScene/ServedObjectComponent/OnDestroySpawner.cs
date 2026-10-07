using UnityEngine;
using GameScene.ServedObjectComponent.Sound;
using Sound.Config;

namespace GameScene.ServedObjectComponent
{
    public class OnDestroySpawner : MonoBehaviour
    {
        
        public GameObject prefab;
        [SerializeField] private ObjectSfxProfile spawnSfxProfile;
        public ObjectSfxProfile SpawnSfxProfile => spawnSfxProfile;
        public bool SuppressPresentation { get; set; }

        private void OnDestroy()
        {
            if (prefab != null && !SuppressPresentation)
            {
                var world = GameScene.Object.PresentationWorld.For(this);
                if (world != null) world.SpawnEffect(prefab, transform.position, Quaternion.identity);
                else
                {
                    GameObject effect = Instantiate(prefab, transform.position, Quaternion.identity);
                    if (effect != null) ServedObjectSfxController.PlaySpawn(spawnSfxProfile);
                }
            }
        }
    }
}
