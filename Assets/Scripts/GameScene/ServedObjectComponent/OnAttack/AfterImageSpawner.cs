using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameScene.ServedObjectComponent.OnAttack
{
    public class AfterImageSpawner : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sourceRenderer;
        [SerializeField] private float spawnInterval = 0.05f;
        [SerializeField] private float lifetime = 0.35f;
        [SerializeField] private float alphaMultiplier = 0.35f;
        [SerializeField] private float endScaleMultiplier = 0.2f;
        [SerializeField] private int initialPoolSize = 8;
        
        private WaitForSeconds spawnDelay;
        private Transform poolRoot;
        private readonly Queue<SpriteAfterImageFade> inactiveAfterImages = new Queue<SpriteAfterImageFade>();
        private bool isDestroyed;

        private void Start()
        {
            if (sourceRenderer == null)
            {
                Transform parent = transform.parent;
                if (parent != null)
                {
                    foreach (SpriteRenderer candidate in parent.GetComponentsInChildren<SpriteRenderer>())
                    {
                        if (!candidate.transform.IsChildOf(transform))
                        {
                            sourceRenderer = candidate;
                            break;
                        }
                    }
                }
            }
            CreatePoolRoot();
            PreloadPool();
            spawnDelay = new WaitForSeconds(Mathf.Max(0.01f, spawnInterval));
            StartCoroutine(PlayAfterImage());
        }
        
        private void OnDestroy()
        {
            isDestroyed = true;

            if (poolRoot != null)
            {
                Destroy(poolRoot.gameObject);
            }
        }
        
        private IEnumerator PlayAfterImage()
        {
            while (true)
            {
                yield return spawnDelay;    
                
                CreateAfterImage();
                
            }
        }
        
        private void CreatePoolRoot()
        {
            GameObject root = new GameObject($"{name}AfterImagePool");
            GameScene.Object.PresentationWorld.For(this)?.Own(root);
            poolRoot = root.transform;
        }

        private void PreloadPool()
        {
            int poolSize = Mathf.Max(0, initialPoolSize);

            for (int i = 0; i < poolSize; i++)
            {
                ReturnAfterImage(CreatePooledAfterImage());
            }
        }

        private void CreateAfterImage()
        {
            if (isDestroyed) return;
            if (sourceRenderer == null || sourceRenderer.sprite == null)
            {
                return;
            }

            SpriteAfterImageFade afterImageFade = GetAfterImage();
            GameObject afterImage = afterImageFade.gameObject;
            afterImage.transform.position = sourceRenderer.transform.position;
            afterImage.transform.rotation = sourceRenderer.transform.rotation;
            afterImage.transform.localScale = sourceRenderer.transform.lossyScale;

            SpriteRenderer afterImageRenderer = afterImage.GetComponent<SpriteRenderer>();
            CopyRenderer(sourceRenderer, afterImageRenderer);

            afterImage.SetActive(true);
            afterImageFade.Play(Mathf.Max(0.01f, lifetime), endScaleMultiplier, ReturnAfterImage);
        }

        private SpriteAfterImageFade GetAfterImage()
        {
            if (inactiveAfterImages.Count > 0)
            {
                return inactiveAfterImages.Dequeue();
            }

            return CreatePooledAfterImage();
        }

        private SpriteAfterImageFade CreatePooledAfterImage()
        {
            GameObject afterImage = new GameObject("AfterImage");
            afterImage.transform.SetParent(poolRoot, false);
            afterImage.AddComponent<SpriteRenderer>();
            return afterImage.AddComponent<SpriteAfterImageFade>();
        }

        private void ReturnAfterImage(SpriteAfterImageFade afterImageFade)
        {
            if (afterImageFade == null)
            {
                return;
            }

            if (isDestroyed)
            {
                Destroy(afterImageFade.gameObject);
                return;
            }

            afterImageFade.gameObject.SetActive(false);
            inactiveAfterImages.Enqueue(afterImageFade);
        }

        private void CopyRenderer(SpriteRenderer source, SpriteRenderer target)
        {
            target.sprite = source.sprite;
            target.sharedMaterial = source.sharedMaterial;
            target.flipX = source.flipX;
            target.flipY = source.flipY;
            target.drawMode = source.drawMode;
            target.size = source.size;
            target.maskInteraction = source.maskInteraction;
            target.sortingLayerID = source.sortingLayerID;
            target.sortingOrder = source.sortingOrder - 1;

            Color color = source.color;
            color.a = Mathf.Clamp01(color.a * alphaMultiplier);
            target.color = color;
        }
    }
}
