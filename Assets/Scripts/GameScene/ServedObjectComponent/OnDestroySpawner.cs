using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    public class OnDestroySpawner : MonoBehaviour
    {
        
        public GameObject prefab;
        public bool SuppressPresentation { get; set; }

        private void OnDestroy()
        {
            if (prefab != null && !SuppressPresentation)
            {
                var world = GameScene.Object.PresentationWorld.For(this);
                if (world != null) world.SpawnEffect(prefab, transform.position, Quaternion.identity);
                else Instantiate(prefab, transform.position, Quaternion.identity);
            }
        }
    }
}
