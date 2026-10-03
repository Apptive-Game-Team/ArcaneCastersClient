using System;
using Data.Quests;
using DG.Tweening;
using RewardChest;
using UnityEngine;
using UnityEngine.UI;

namespace Adventures
{
    /// <summary>
    /// The chest at the end of an adventure's stage path. It only draws a state and reports clicks;
    /// <see cref="AdventureEndChestController"/> decides the state and runs the claim.
    /// <para>
    /// <see cref="closedChestSprite"/> and <see cref="openChestSprite"/> hold the default chest art, and
    /// <see cref="SetChestKey"/> swaps in the art of the quest's own chest. While a closed sprite exists the
    /// placeholder lid and lock stay hidden; with an open sprite, the claimed chest swaps the body to it.
    /// Without sprites the chest falls back to the placeholder shapes (body, lid, lock).
    /// </para>
    /// </summary>
    public class AdventureEndChest : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform chestRoot;
        [SerializeField] private Image chestBodyImage;
        [SerializeField] private RectTransform chestLid;
        [SerializeField] private GameObject lockObject;
        [SerializeField] private Image glowImage;
        [SerializeField] private Sprite closedChestSprite;
        [SerializeField] private Sprite openChestSprite;

        [Header("Claimable")]
        [SerializeField] private float bobHeight = 12f;
        [SerializeField] private float bobSeconds = 0.8f;
        [SerializeField] private float glowAlpha = 0.7f;

        [Header("Claimed")]
        [SerializeField] private float openLidLift = 28f;
        [SerializeField] private float openLidAngle = 24f;
        [SerializeField] private Color claimedBodyColor = new Color(0.62f, 0.5f, 0.4f, 1f);

        private bool authoredCaptured;
        private Vector2 chestRootPosition;
        private Vector2 lidPosition;
        private Quaternion lidRotation;
        private Sprite authoredBodySprite;
        private Color authoredBodyColor;
        private bool busy;
        private bool listenerAdded;
        private Sprite keyedClosedSprite;
        private Sprite keyedOpenSprite;

        private Sprite ClosedSprite => keyedClosedSprite != null ? keyedClosedSprite : closedChestSprite;
        private Sprite OpenSprite => keyedOpenSprite != null ? keyedOpenSprite : openChestSprite;

        public event Action Clicked;

        public AdventureChestState State { get; private set; } = AdventureChestState.Hidden;

        private void Awake()
        {
            CaptureAuthoredPose();
            AddListener();
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }

        /// <summary>
        /// Picks the art of <paramref name="chestKey"/> (the quest's <c>rewardKey</c>); an unknown or empty key
        /// gets the default chest. Call it before <see cref="SetState"/>, which draws the sprite.
        /// </summary>
        public void SetChestKey(string chestKey)
        {
            keyedClosedSprite = RewardSpriteResolver.TryResolveChestIcon(chestKey, out Sprite closed) ? closed : null;
            keyedOpenSprite = RewardSpriteResolver.TryResolveChestOpenIcon(chestKey, out Sprite open) ? open : null;
        }

        /// <summary>Hidden turns the whole chest off; the other two show it closed and bobbing, or open and empty.</summary>
        public void SetState(AdventureChestState state)
        {
            CaptureAuthoredPose();
            AddListener();
            State = state;
            DOTween.Kill(this);
            ResetPose();

            switch (state)
            {
                case AdventureChestState.Claimable:
                    gameObject.SetActive(true);
                    ShowClaimable();
                    break;
                case AdventureChestState.Claimed:
                    gameObject.SetActive(true);
                    ShowClaimed();
                    break;
                default:
                    gameObject.SetActive(false);
                    break;
            }

            RefreshInteractable();
        }

        /// <summary>Blocks clicks while a claim runs, so one chest is never claimed twice from one screen.</summary>
        public void SetBusy(bool isBusy)
        {
            busy = isBusy;
            RefreshInteractable();
        }

        private void ShowClaimable()
        {
            if (chestRoot != null)
            {
                chestRoot.DOAnchorPosY(chestRootPosition.y + bobHeight, bobSeconds)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetTarget(this);
            }

            if (glowImage != null)
            {
                glowImage.gameObject.SetActive(true);
                SetAlpha(glowImage, glowAlpha * 0.5f);
                glowImage.DOFade(glowAlpha, bobSeconds)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetTarget(this);
            }
        }

        private void ShowClaimed()
        {
            if (glowImage != null)
            {
                glowImage.gameObject.SetActive(false);
            }

            if (lockObject != null)
            {
                lockObject.SetActive(false);
            }

            if (chestBodyImage != null)
            {
                // Real open art keeps its own colours; only the placeholder body is darkened to read as taken.
                if (OpenSprite != null)
                {
                    chestBodyImage.sprite = OpenSprite;
                }
                else
                {
                    chestBodyImage.color = claimedBodyColor;
                }
            }

            if (chestLid == null)
            {
                return;
            }

            if (OpenSprite != null)
            {
                chestLid.gameObject.SetActive(false);
                return;
            }

            // Placeholder: the lid stays tipped open above the body, so the chest reads as already taken.
            chestLid.anchoredPosition = lidPosition + new Vector2(0f, openLidLift);
            chestLid.localRotation = lidRotation * Quaternion.Euler(0f, 0f, openLidAngle);
        }

        private void ResetPose()
        {
            if (chestRoot != null)
            {
                chestRoot.anchoredPosition = chestRootPosition;
            }

            if (chestLid != null)
            {
                chestLid.gameObject.SetActive(ClosedSprite == null);
                chestLid.anchoredPosition = lidPosition;
                chestLid.localRotation = lidRotation;
            }

            if (lockObject != null)
            {
                lockObject.SetActive(ClosedSprite == null);
            }

            if (chestBodyImage != null)
            {
                chestBodyImage.sprite = ClosedSprite != null ? ClosedSprite : authoredBodySprite;
                chestBodyImage.color = authoredBodyColor;
            }

            if (glowImage != null)
            {
                SetAlpha(glowImage, 0f);
                glowImage.gameObject.SetActive(false);
            }
        }

        private void RefreshInteractable()
        {
            if (button != null)
            {
                button.interactable = !busy && State == AdventureChestState.Claimable;
            }
        }

        private void CaptureAuthoredPose()
        {
            if (authoredCaptured)
            {
                return;
            }

            if (chestRoot != null)
            {
                chestRootPosition = chestRoot.anchoredPosition;
            }

            if (chestLid != null)
            {
                lidPosition = chestLid.anchoredPosition;
                lidRotation = chestLid.localRotation;
            }

            if (chestBodyImage != null)
            {
                authoredBodySprite = chestBodyImage.sprite;
                authoredBodyColor = chestBodyImage.color;
            }

            authoredCaptured = true;
        }

        private void AddListener()
        {
            if (listenerAdded || button == null)
            {
                return;
            }

            button.onClick.AddListener(OnButtonClicked);
            listenerAdded = true;
        }

        private void OnButtonClicked()
        {
            if (busy || State != AdventureChestState.Claimable)
            {
                return;
            }

            Clicked?.Invoke();
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
