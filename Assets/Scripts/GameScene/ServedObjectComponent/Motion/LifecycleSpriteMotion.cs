using System;
using GameScene.Dto.Projectile;
using GameScene.ServedObjectComponent.Effect;
using UnityEngine;

namespace GameScene.ServedObjectComponent.Motion
{
    public sealed class LifecycleSpriteMotion : ServedObjectBehaviour
    {
        private const float FrameInterval = 1f / 12f;

        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite[] spawnFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] exitFrames = Array.Empty<Sprite>();
        [SerializeField] private string releaseCue;
        [SerializeField] private string skipExitCue;

        private SpriteRenderer renderer;
        private bool exitHandled;

        protected override void OnBound()
        {
            renderer = ResolveRenderer(targetRenderer);
            Owner.OnSpawned += PlaySpawn;
            Owner.OnDestroyed += PlayExit;
            Owner.OnProjectilePose += HandleCue;
        }

        protected override void OnUnbound()
        {
            if (Owner == null) return;
            Owner.OnSpawned -= PlaySpawn;
            Owner.OnDestroyed -= PlayExit;
            Owner.OnProjectilePose -= HandleCue;
        }

        private void PlaySpawn()
        {
            if (renderer == null || spawnFrames.Length == 0) return;
            gameObject.AddComponent<SpriteFrameAnimator>().PlayOnce(renderer, spawnFrames, FrameInterval);
        }

        private void HandleCue(ProjectileDto cue)
        {
            if (!string.IsNullOrEmpty(skipExitCue) && cue.type == skipExitCue)
            {
                exitHandled = true;
                if (renderer != null) renderer.enabled = false;
            }
            else if (!string.IsNullOrEmpty(releaseCue) && cue.type == releaseCue)
            {
                PlayExit();
            }
        }

        private void PlayExit()
        {
            if (exitHandled || renderer == null || exitFrames.Length == 0) return;
            exitHandled = true;
            renderer.enabled = false;

            GameObject effect = DeathVisualFactory.CreateSpriteClone(renderer, $"{Owner.name} {Owner.id} Exit");
            var sprite = effect.GetComponent<SpriteRenderer>();
            effect.AddComponent<SpriteFrameAnimator>().PlayOnce(sprite, exitFrames, FrameInterval, destroyAfterPlayback: true);
        }
    }
}
