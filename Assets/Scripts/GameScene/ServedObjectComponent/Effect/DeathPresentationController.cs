using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityObject = UnityEngine.Object;
using Sequence = DG.Tweening.Sequence;

namespace GameScene.ServedObjectComponent.Effect
{
    /// <summary>How a dying object leaves the field.</summary>
    public enum DeathPresentationStyle
    {
        /// <summary>No presentation — the object simply disappears.</summary>
        None = 0,

        /// <summary>Fades the sprite out and scatters a few crumbling fragments.</summary>
        Fade = 1
    }

    /// <summary>
    /// Plays a death presentation on a throwaway sprite clone when the server destroys the object.
    /// <para>
    /// Set the style on the prefab. Abstract base prefabs carry the default for their whole family
    /// and variants override only what differs — projectiles that leave no remains, for instance —
    /// so no object-type string is needed in code.
    /// </para>
    /// <para>
    /// Anything that moves a dying unit belongs on the server, not here: the clone spawns after the
    /// object is already gone, so its position is not authoritative. Aerial units fall through a
    /// server-side dying state and only reach this component once they have actually landed.
    /// </para>
    /// </summary>
    public sealed class DeathPresentationController : ServedObjectBehaviour
    {
        private const int FragmentGridSize = 3;
        private const float FragmentLifetime = 0.5f;
        private const float FragmentRiseDistance = 0.35f;
        private const float FragmentSpreadDistance = 0.3f;
        private const float FragmentSpin = 90f;

        [SerializeField] private DeathPresentationStyle style = DeathPresentationStyle.Fade;
        [SerializeField] private SpriteRenderer sourceRenderer;
        [SerializeField] private float fadeDuration = 0.35f;
        [SerializeField] private bool scatterFragments = true;

        private bool played;

        protected override void OnBound()
        {
            if (style == DeathPresentationStyle.None)
            {
                return;
            }

            Owner.OnDestroyed += Play;
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnDestroyed -= Play;
            }
        }

        private void Play()
        {
            if (played)
            {
                return;
            }

            played = true;
            Owner.OnDestroyed -= Play;

            SpriteRenderer source = ResolveRenderer(sourceRenderer);
            if (source == null || source.sprite == null)
            {
                return;
            }

            if (style == DeathPresentationStyle.Fade)
            {
                PlayFade(source);
            }
        }

        /// <summary>
        /// Runs the whole presentation — fade plus every fragment — on a single sequence.
        /// <para>
        /// One sequence per dying unit, not one per fragment: each sequence occupies a slot in
        /// DOTween's active-tween list, and a wipe that killed a handful of units at once used to
        /// exhaust the pool and corrupt the list while DOTween reorganized it mid-frame.
        /// </para>
        /// </summary>
        private void PlayFade(SpriteRenderer source)
        {
            GameObject visual = DeathVisualFactory.CreateSpriteClone(source, $"{Owner.name}DeathFade");
            SpriteRenderer visualRenderer = visual.GetComponent<SpriteRenderer>();
            List<UnityObject> spawned = new List<UnityObject> { visual };

            Sequence sequence = DOTween.Sequence();
            sequence.SetLink(visual);
            sequence.Append(visualRenderer.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));

            if (scatterFragments)
            {
                ScatterFragments(sequence, source, spawned);
            }

            // Sole cleanup path: OnKill covers both natural completion (auto-kill) and the
            // SetLink kill that fires when the clone is destroyed early, e.g. on scene unload.
            sequence.OnKill(() => DestroySpawned(spawned));
        }

        private static void ScatterFragments(Sequence sequence, SpriteRenderer source, List<UnityObject> spawned)
        {
            Sprite sprite = source.sprite;
            Rect textureRect = sprite.textureRect;
            if (textureRect.width < FragmentGridSize || textureRect.height < FragmentGridSize)
            {
                return;
            }

            float cellWidth = textureRect.width / FragmentGridSize;
            float cellHeight = textureRect.height / FragmentGridSize;
            Vector3 scale = source.transform.lossyScale;
            float cellWorldWidth = cellWidth / sprite.pixelsPerUnit * scale.x;
            float cellWorldHeight = cellHeight / sprite.pixelsPerUnit * scale.y;
            Vector3 center = source.bounds.center;

            for (int column = 0; column < FragmentGridSize; column++)
            {
                for (int row = 0; row < FragmentGridSize; row++)
                {
                    Rect cell = new Rect(
                        textureRect.x + column * cellWidth,
                        textureRect.y + row * cellHeight,
                        cellWidth,
                        cellHeight);
                    float offsetFromCenter = column - (FragmentGridSize - 1) * 0.5f;
                    Vector3 cellPosition = center + new Vector3(
                        offsetFromCenter * cellWorldWidth,
                        (row - (FragmentGridSize - 1) * 0.5f) * cellWorldHeight,
                        0f);

                    SpawnFragment(sequence, spawned, source, sprite, cell, cellPosition, offsetFromCenter);
                }
            }
        }

        private static void SpawnFragment(
            Sequence sequence,
            List<UnityObject> spawned,
            SpriteRenderer source,
            Sprite sprite,
            Rect cell,
            Vector3 position,
            float offsetFromCenter)
        {
            Sprite fragmentSprite = Sprite.Create(
                sprite.texture,
                cell,
                new Vector2(0.5f, 0.5f),
                sprite.pixelsPerUnit);

            GameObject fragment = new GameObject($"{source.name}DeathFragment");
            fragment.transform.position = position;
            fragment.transform.localScale = source.transform.lossyScale;
            GameScene.Object.PresentationWorld.For(source)?.Own(fragment);

            SpriteRenderer fragmentRenderer = fragment.AddComponent<SpriteRenderer>();
            fragmentRenderer.sprite = fragmentSprite;
            fragmentRenderer.color = source.color;
            fragmentRenderer.sharedMaterial = source.sharedMaterial;
            fragmentRenderer.sortingLayerID = source.sortingLayerID;
            fragmentRenderer.sortingOrder = source.sortingOrder + 1;

            Vector3 drift = new Vector3(
                offsetFromCenter * FragmentSpreadDistance,
                FragmentRiseDistance,
                0f);

            // Inserted at 0 so every fragment still starts with the fade, as it did when each
            // fragment owned its own sequence.
            sequence.Insert(0f, fragment.transform.DOMove(position + drift, FragmentLifetime).SetEase(Ease.OutQuad));
            sequence.Insert(0f, fragment.transform.DORotate(
                new Vector3(0f, 0f, offsetFromCenter * FragmentSpin),
                FragmentLifetime));
            sequence.Insert(0f, fragmentRenderer.DOFade(0f, FragmentLifetime).SetEase(Ease.InQuad));

            spawned.Add(fragment);
            spawned.Add(fragmentSprite);
        }

        private static void DestroySpawned(List<UnityObject> spawned)
        {
            foreach (UnityObject spawnedObject in spawned)
            {
                if (spawnedObject != null)
                {
                    Destroy(spawnedObject);
                }
            }
        }
    }
}
