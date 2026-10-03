using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace RewardChest
{
    /// <summary>
    /// The short "you got a chest" card shown right after a claim: the chest drawn as a reward tile by the
    /// <c>CHEST</c> tile renderer, an "Open now" button and a "Later" button. It only asks; opening the chest is
    /// <see cref="ChestClaimFlow"/>'s job.
    /// </summary>
    public class ChestClaimChoicePanel : MonoBehaviour
    {
        [SerializeField] private RectTransform card;
        [SerializeField] private RectTransform tileRoot;
        [SerializeField] private RewardTileView tilePrefab;
        [SerializeField] private Button openNowButton;
        [SerializeField] private Button laterButton;
        [SerializeField] private float appearSeconds = 0.25f;

        private bool listenersAdded;
        private bool? openNowChosen;

        /// <summary>
        /// Shows the card for <paramref name="chest"/> and finishes when the player picks. Run it from the
        /// caller's coroutine: the panel starts inactive and cannot start a coroutine of its own.
        /// </summary>
        public IEnumerator Ask(RewardView chest, RewardTileRendererSelector selector, Action<bool> onChosen)
        {
            AddListeners();
            openNowChosen = null;
            gameObject.SetActive(true);
            SetButtonsInteractable(true);
            RenderTile(chest, selector);

            DOTween.Kill(this);
            if (card != null)
            {
                card.localScale = Vector3.one * 0.6f;
                card.DOScale(1f, appearSeconds).SetEase(Ease.OutBack).SetTarget(this);
            }

            while (!openNowChosen.HasValue)
            {
                yield return null;
            }

            DOTween.Kill(this);
            gameObject.SetActive(false);
            onChosen?.Invoke(openNowChosen.Value);
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }

        private void AddListeners()
        {
            if (listenersAdded)
            {
                return;
            }

            if (openNowButton != null)
            {
                openNowButton.onClick.AddListener(() => Choose(true));
            }

            if (laterButton != null)
            {
                laterButton.onClick.AddListener(() => Choose(false));
            }

            listenersAdded = true;
        }

        private void Choose(bool openNow)
        {
            if (openNowChosen.HasValue)
            {
                return;
            }

            openNowChosen = openNow;
            SetButtonsInteractable(false);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (openNowButton != null)
            {
                openNowButton.interactable = interactable;
            }

            if (laterButton != null)
            {
                laterButton.interactable = interactable;
            }
        }

        private void RenderTile(RewardView chest, RewardTileRendererSelector selector)
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

            if (tilePrefab == null || chest == null || selector == null)
            {
                return;
            }

            RewardTileView tile = Instantiate(tilePrefab, tileRoot);
            tile.gameObject.SetActive(true);
            tile.Render(chest, selector.Build(chest));
        }
    }
}
