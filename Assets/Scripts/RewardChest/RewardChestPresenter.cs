using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using RewardChest.Renderers;
using UnityEngine;
using UnityEngine.UI;

namespace RewardChest
{
    /// <summary>
    /// Shows a closed chest, opens it, reveals the rewards one tile at a time and waits for the player to
    /// tap. It knows nothing about quests or the chest endpoints: callers hand it <see cref="RewardView"/>s.
    /// <para>
    /// <see cref="closedChestSprite"/> and <see cref="openChestSprite"/> hold the default chest art; a chest
    /// key (passed to <see cref="Play"/>, or the first CHEST reward in the list) swaps in that chest's own art
    /// through <see cref="RewardSpriteResolver"/>. While a closed sprite exists the placeholder lid stays hidden;
    /// with an open sprite, the body swaps to it on open. The rays turn behind the chest and the sparkles
    /// twinkle once it is open.
    /// </para>
    /// </summary>
    public class RewardChestPresenter : MonoBehaviour
    {
        [Header("Chest")]
        [SerializeField] private RectTransform chestRoot;
        [SerializeField] private Image chestBodyImage;
        [SerializeField] private RectTransform chestLid;
        [SerializeField] private Image chestLidImage;
        [SerializeField] private Image glowImage;
        [SerializeField] private Sprite closedChestSprite;
        [SerializeField] private Sprite openChestSprite;

        [Header("Open effects")]
        [SerializeField] private Image rayImage;
        [SerializeField] private Image[] sparkleImages;
        [SerializeField] private float rayAlpha = 0.8f;
        [SerializeField] private float raySecondsPerTurn = 24f;
        [SerializeField] private float sparkleSeconds = 0.6f;
        [SerializeField] private float sparkleStaggerSeconds = 0.17f;

        [Header("Rewards")]
        [SerializeField] private RectTransform tileRoot;
        [SerializeField] private RewardTileView tilePrefab;

        [Header("Dismiss")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button dismissButton;
        [SerializeField] private GameObject continueHint;

        [Header("Timing (seconds)")]
        [SerializeField] private float appearSeconds = 0.25f;
        [SerializeField] private float shakeSeconds = 0.5f;
        [SerializeField] private float openSeconds = 0.35f;
        [SerializeField] private float tilePopSeconds = 0.3f;
        [SerializeField] private float tileIntervalSeconds = 0.2f;
        [SerializeField] private float lidLift = 60f;

        private RewardTileRendererSelector selector;
        private bool skipRequested;
        private bool dismissRequested;
        private bool waitingForDismiss;
        private bool listenerAdded;
        private bool lidPoseCaptured;
        private Vector2 lidAnchoredPosition;
        private Quaternion lidRotation;
        private Sprite authoredBodySprite;
        private bool authoredBodySpriteCaptured;
        private Sprite activeClosedSprite;
        private Sprite activeOpenSprite;

        public bool IsPlaying { get; private set; }

        /// <summary>Replaces the default renderer list, for a caller that draws rewards differently.</summary>
        public void SetRendererSelector(RewardTileRendererSelector rendererSelector)
        {
            selector = rendererSelector;
        }

        /// <summary>
        /// Plays the whole sequence and finishes after the player taps to dismiss. A tap while the chest
        /// opens or the tiles appear skips to the end of the reveal instead.
        /// Run it from the caller's coroutine (<c>yield return presenter.Play(...)</c>): the presenter may
        /// start inactive, and an inactive object cannot start a coroutine of its own.
        /// <paramref name="chestKey"/> names the chest being opened; when null, the first CHEST reward in
        /// <paramref name="rewards"/> names it, and with neither the default chest is drawn.
        /// </summary>
        public IEnumerator Play(IReadOnlyList<RewardView> rewards, Action onDismissed = null, string chestKey = null)
        {
            selector ??= RewardTileRenderers.CreateDefaultSelector();
            IsPlaying = true;
            gameObject.SetActive(true);
            ChooseChestSprites(chestKey ?? FirstChestKey(rewards));
            PrepareClosedChest();

            yield return Appear();
            yield return OpenChest();
            yield return RevealTiles(rewards ?? Array.Empty<RewardView>());

            waitingForDismiss = true;
            SetContinueHintVisible(true);
            while (!dismissRequested)
            {
                yield return null;
            }

            waitingForDismiss = false;
            yield return Disappear();

            DOTween.Kill(this);
            gameObject.SetActive(false);
            IsPlaying = false;
            onDismissed?.Invoke();
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }

        private void ChooseChestSprites(string chestKey)
        {
            activeClosedSprite = RewardSpriteResolver.TryResolveChestIcon(chestKey, out Sprite closed) ? closed : closedChestSprite;
            activeOpenSprite = RewardSpriteResolver.TryResolveChestOpenIcon(chestKey, out Sprite open) ? open : openChestSprite;
        }

        private static string FirstChestKey(IReadOnlyList<RewardView> rewards)
        {
            if (rewards == null)
            {
                return null;
            }

            foreach (RewardView reward in rewards)
            {
                if (reward != null && reward.Type == RewardTypes.Chest)
                {
                    return reward.Key;
                }
            }

            return null;
        }

        private void PrepareClosedChest()
        {
            if (!listenerAdded && dismissButton != null)
            {
                dismissButton.onClick.AddListener(OnDismissButtonClicked);
                listenerAdded = true;
            }

            DOTween.Kill(this);
            skipRequested = false;
            dismissRequested = false;
            waitingForDismiss = false;
            SetContinueHintVisible(false);
            ClearTiles();

            if (chestBodyImage != null)
            {
                if (!authoredBodySpriteCaptured)
                {
                    authoredBodySprite = chestBodyImage.sprite;
                    authoredBodySpriteCaptured = true;
                }

                chestBodyImage.sprite = activeClosedSprite != null ? activeClosedSprite : authoredBodySprite;
            }

            if (chestLid != null)
            {
                if (!lidPoseCaptured)
                {
                    lidAnchoredPosition = chestLid.anchoredPosition;
                    lidRotation = chestLid.localRotation;
                    lidPoseCaptured = true;
                }

                chestLid.anchoredPosition = lidAnchoredPosition;
                chestLid.localRotation = lidRotation;
                chestLid.gameObject.SetActive(activeClosedSprite == null);
            }

            SetAlpha(chestLidImage, 1f);
            SetAlpha(glowImage, 0f);
            SetAlpha(rayImage, 0f);
            if (sparkleImages != null)
            {
                foreach (Image sparkle in sparkleImages)
                {
                    SetAlpha(sparkle, 0f);
                }
            }

            if (glowImage != null)
            {
                glowImage.rectTransform.localScale = Vector3.one * 0.3f;
            }

            if (chestRoot != null)
            {
                chestRoot.localRotation = Quaternion.identity;
                chestRoot.localScale = Vector3.one * 0.6f;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private IEnumerator Appear()
        {
            Sequence sequence = DOTween.Sequence().SetTarget(this);
            if (canvasGroup != null)
            {
                sequence.Join(canvasGroup.DOFade(1f, appearSeconds));
            }

            if (chestRoot != null)
            {
                sequence.Join(chestRoot.DOScale(1f, appearSeconds).SetEase(Ease.OutBack));
            }

            yield return WaitOrSkip(sequence);
        }

        private IEnumerator OpenChest()
        {
            Sequence sequence = DOTween.Sequence().SetTarget(this);
            if (chestRoot != null)
            {
                sequence.Append(chestRoot.DOShakeRotation(shakeSeconds, new Vector3(0f, 0f, 12f), 14, 90f));
            }

            sequence.AppendCallback(() =>
            {
                SwapToOpenSprite();
                StartOpenEffects();
            });
            if (chestLid != null && activeOpenSprite == null)
            {
                sequence.Append(chestLid.DOAnchorPosY(lidAnchoredPosition.y + lidLift, openSeconds).SetEase(Ease.OutQuad));
                sequence.Join(chestLid.DOLocalRotate(new Vector3(0f, 0f, 20f), openSeconds));
                if (chestLidImage != null)
                {
                    sequence.Join(chestLidImage.DOFade(0f, openSeconds).SetEase(Ease.InQuad));
                }
            }

            if (rayImage != null)
            {
                sequence.Join(rayImage.DOFade(rayAlpha, openSeconds));
            }

            if (glowImage != null)
            {
                sequence.Join(glowImage.rectTransform.DOScale(1.3f, openSeconds).SetEase(Ease.OutBack));
                sequence.Join(glowImage.DOFade(0.85f, openSeconds));
            }

            yield return WaitOrSkip(sequence);
        }

        /// <summary>Spins the rays and starts the sparkles. They loop on their own tweens, so a skip does not wait for them.</summary>
        private void StartOpenEffects()
        {
            if (rayImage != null)
            {
                rayImage.rectTransform
                    .DOLocalRotate(new Vector3(0f, 0f, -360f), raySecondsPerTurn, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart)
                    .SetTarget(this);
            }

            if (sparkleImages == null)
            {
                return;
            }

            for (int index = 0; index < sparkleImages.Length; index++)
            {
                if (sparkleImages[index] == null)
                {
                    continue;
                }

                sparkleImages[index].DOFade(1f, sparkleSeconds)
                    .SetDelay(index * sparkleStaggerSeconds)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetTarget(this);
            }
        }

        private void SwapToOpenSprite()
        {
            if (activeOpenSprite == null || chestBodyImage == null)
            {
                return;
            }

            chestBodyImage.sprite = activeOpenSprite;
            if (chestLid != null)
            {
                chestLid.gameObject.SetActive(false);
            }
        }

        private IEnumerator RevealTiles(IReadOnlyList<RewardView> rewards)
        {
            if (tileRoot == null || tilePrefab == null)
            {
                yield break;
            }

            foreach (RewardView reward in rewards)
            {
                if (reward == null)
                {
                    continue;
                }

                RewardTileView tile = Instantiate(tilePrefab, tileRoot);
                tile.gameObject.SetActive(true);
                tile.Render(reward, selector.Build(reward));

                if (skipRequested)
                {
                    continue;
                }

                tile.transform.localScale = Vector3.zero;
                Tween pop = tile.transform.DOScale(1f, tilePopSeconds).SetEase(Ease.OutBack).SetTarget(this);
                pop.SetLink(tile.gameObject);
                yield return WaitSeconds(tileIntervalSeconds);
                if (skipRequested)
                {
                    pop.Complete();
                }
            }

            // Tiles still popping when the player skipped land at full size.
            if (skipRequested)
            {
                DOTween.Complete(this);
            }
        }

        private IEnumerator Disappear()
        {
            if (canvasGroup == null)
            {
                yield break;
            }

            Tween fade = canvasGroup.DOFade(0f, appearSeconds).SetTarget(this);
            yield return fade.WaitForCompletion();
        }

        private IEnumerator WaitOrSkip(Tween tween)
        {
            while (tween.IsActive() && tween.IsPlaying())
            {
                if (skipRequested)
                {
                    tween.Complete();
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator WaitSeconds(float seconds)
        {
            float endTime = Time.unscaledTime + seconds;
            while (Time.unscaledTime < endTime && !skipRequested)
            {
                yield return null;
            }
        }

        private void OnDismissButtonClicked()
        {
            if (waitingForDismiss)
            {
                dismissRequested = true;
                return;
            }

            skipRequested = true;
        }

        private void ClearTiles()
        {
            if (tileRoot == null)
            {
                return;
            }

            // Every child is a spawned tile: the template is the prefab asset, not a child of this root.
            for (int index = tileRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(tileRoot.GetChild(index).gameObject);
            }
        }

        private void SetContinueHintVisible(bool visible)
        {
            if (continueHint != null)
            {
                continueHint.SetActive(visible);
            }
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null)
            {
                return;
            }

            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
