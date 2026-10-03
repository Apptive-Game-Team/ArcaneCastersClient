using System;
using Data.Quests;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Adventures
{
    /// <summary>
    /// The chest at the end of an adventure's stage path. It only draws a state and reports clicks;
    /// <see cref="AdventureEndChestController"/> decides the state and runs the claim.
    /// <para>
    /// Drawn with the same placeholder shapes as <c>RewardChestPresenter.prefab</c> (body, lid, lock, glow)
    /// until chest art exists. Put real sprites in <see cref="closedChestSprite"/> and
    /// <see cref="openChestSprite"/>; with an open sprite set, the claimed chest swaps the body to it and hides
    /// the separate lid.
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
                chestBodyImage.color = claimedBodyColor;
                if (openChestSprite != null)
                {
                    chestBodyImage.sprite = openChestSprite;
                }
            }

            if (chestLid == null)
            {
                return;
            }

            if (openChestSprite != null)
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
                chestLid.gameObject.SetActive(true);
                chestLid.anchoredPosition = lidPosition;
                chestLid.localRotation = lidRotation;
            }

            if (lockObject != null)
            {
                lockObject.SetActive(true);
            }

            if (chestBodyImage != null)
            {
                chestBodyImage.sprite = closedChestSprite != null ? closedChestSprite : authoredBodySprite;
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
