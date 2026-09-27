using System;
using System.Collections.Generic;
using System.Linq;
using Data.Deck;
using Data.Magic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckScene
{
    public class DeckManagementView
    {
        private readonly TMP_Dropdown deckDropdown;
        private readonly Button createDeckButton;
        private readonly Transform deckCardsContainer;
        private readonly Transform ownedCardsContainer;
        private readonly GameObject cardItemPrefab;
        private readonly GameObject cardInDeckItemPrefab;
        private readonly Button submitDeckButton;
        private readonly Button removeDeckButton;
        private readonly TMP_InputField deckNameInputField;
        private readonly HaveCardMagicPopup ownedCardMagicPopup;

        private readonly Action<DeckResponseDto> onDeckSelected;
        private readonly Action onNewDeckSelected;
        private readonly Action<CardDto> onOwnedCardSelected;
        private readonly Action<CardDto> onCardInDeckSelected;
        private readonly Func<CardDto, IReadOnlyList<CombinedMagicData>> getOwnedCardMagicSuggestions;

        // 드롭다운 항목 순서대로 든 덱. 저장 전의 새 덱 항목은 null 이다.
        private readonly List<DeckResponseDto> dropdownDecks = new();
        private readonly List<(long CardId, CardItemUI Item)> ownedCardItems = new();
        private DeckResponseDto renderedDeck;

        public DeckManagementView(
            TMP_Dropdown deckDropdown,
            Button createDeckButton,
            Transform deckCardsContainer,
            Transform ownedCardsContainer,
            GameObject cardItemPrefab,
            GameObject cardInDeckItemPrefab,
            Button submitDeckButton,
            Button removeDeckButton,
            TMP_InputField deckNameInputField,
            Action<DeckResponseDto> onDeckSelected,
            Action onNewDeckSelected,
            Action<CardDto> onOwnedCardSelected,
            Action<CardDto> onCardInDeckSelected,
            HaveCardMagicPopup ownedCardMagicPopup,
            Func<CardDto, IReadOnlyList<CombinedMagicData>> getOwnedCardMagicSuggestions)
        {
            this.deckDropdown = deckDropdown;
            this.createDeckButton = createDeckButton;
            this.deckCardsContainer = deckCardsContainer;
            this.ownedCardsContainer = ownedCardsContainer;
            this.cardItemPrefab = cardItemPrefab;
            this.cardInDeckItemPrefab = cardInDeckItemPrefab;
            this.submitDeckButton = submitDeckButton;
            this.removeDeckButton = removeDeckButton;
            this.deckNameInputField = deckNameInputField;
            this.onDeckSelected = onDeckSelected;
            this.onNewDeckSelected = onNewDeckSelected;
            this.onOwnedCardSelected = onOwnedCardSelected;
            this.onCardInDeckSelected = onCardInDeckSelected;
            this.ownedCardMagicPopup = ownedCardMagicPopup;
            this.getOwnedCardMagicSuggestions = getOwnedCardMagicSuggestions;

            if (deckDropdown != null)
            {
                deckDropdown.onValueChanged.RemoveAllListeners();
                deckDropdown.onValueChanged.AddListener(OnDeckDropdownChanged);
            }

            if (createDeckButton != null)
            {
                createDeckButton.onClick.RemoveAllListeners();
                createDeckButton.onClick.AddListener(() => onNewDeckSelected?.Invoke());
            }
        }

        public string DeckName => deckNameInputField != null ? deckNameInputField.text : string.Empty;

        public void SetDeckName(string deckName)
        {
            deckNameInputField?.SetTextWithoutNotify(deckName);
        }

        public void BindSubmit(Action onSubmit)
        {
            submitDeckButton.onClick.RemoveAllListeners();
            submitDeckButton.onClick.AddListener(() => onSubmit?.Invoke());
        }

        public void BindRemove(Action onRemove)
        {
            if (removeDeckButton == null)
            {
                return;
            }

            removeDeckButton.onClick.RemoveAllListeners();
            removeDeckButton.onClick.AddListener(() => onRemove?.Invoke());
        }

        public void SetRemoveButtonActive(bool isActive)
        {
            if (removeDeckButton == null)
            {
                return;
            }

            removeDeckButton.gameObject.SetActive(isActive);
        }

        /// <summary>
        /// 덱 드롭다운을 다시 채운다. 저장 전의 새 덱은 맨 끝 항목으로 붙여 고른 상태로 둔다.
        /// </summary>
        public void RenderDecks(DeckResponseDto[] decks, DeckResponseDto currentDeck, DeckEditMode mode)
        {
            if (deckDropdown == null)
            {
                return;
            }

            dropdownDecks.Clear();
            var options = new List<string>();
            int selectedIndex = -1;

            foreach (DeckResponseDto deck in decks ?? Array.Empty<DeckResponseDto>())
            {
                if (mode == DeckEditMode.Update && currentDeck != null && deck.id == currentDeck.id)
                {
                    selectedIndex = options.Count;
                }

                dropdownDecks.Add(deck);
                options.Add(deck.name);
            }

            if (mode == DeckEditMode.Create && currentDeck != null)
            {
                selectedIndex = options.Count;
                dropdownDecks.Add(null);
                options.Add(currentDeck.name);
            }

            deckDropdown.ClearOptions();
            deckDropdown.AddOptions(options);
            deckDropdown.SetValueWithoutNotify(Mathf.Max(selectedIndex, 0));
            deckDropdown.RefreshShownValue();

            if (selectedIndex < 0 && deckDropdown.captionText != null)
            {
                deckDropdown.captionText.text = string.Empty;
            }
        }

        private void OnDeckDropdownChanged(int index)
        {
            if (index < 0 || index >= dropdownDecks.Count || dropdownDecks[index] == null)
            {
                return;
            }

            onDeckSelected?.Invoke(dropdownDecks[index]);
        }

        public void RenderOwnedCards(CardDto[] ownedCards)
        {
            ClearChildren(ownedCardsContainer);
            ownedCardItems.Clear();

            foreach (CardDto card in ownedCards)
            {
                GameObject item = UnityEngine.Object.Instantiate(cardItemPrefab, ownedCardsContainer);
                CardItemUI ui = item.GetComponent<CardItemUI>();
                Button button = item.GetComponent<Button>();

                ui.Init(card.name, card.count, card.unlocked, card.unlockText, card.progressText);
                ownedCardItems.Add((card.id, ui));
                CardDto localCard = card;
                ui.BindHover(
                    hovered =>
                    {
                        if (!localCard.unlocked || ownedCardMagicPopup == null)
                        {
                            return;
                        }

                        ownedCardMagicPopup.Show(
                            getOwnedCardMagicSuggestions?.Invoke(localCard),
                            hovered.transform as RectTransform);
                    },
                    () => ownedCardMagicPopup?.Hide()
                );

                button.onClick.RemoveAllListeners();
                button.interactable = card.unlocked;

                if (!card.unlocked)
                {
                    continue;
                }

                button.onClick.AddListener(() => onOwnedCardSelected?.Invoke(localCard));
            }

            RenderInDeckMarkers();
        }

        /// <summary>
        /// 덱의 카드를 한 장에 한 칸씩 그린다. 같은 마법은 붙여 놓는다.
        /// 빈 칸 테두리는 씬에 고정으로 깔려 있어 여기서 그리지 않는다.
        /// </summary>
        public void RenderDeckCards(DeckResponseDto deck)
        {
            ClearChildren(deckCardsContainer);
            renderedDeck = deck;
            RenderInDeckMarkers();
            if (deck?.cards == null)
            {
                return;
            }

            IEnumerable<CardDto> cardsInSlotOrder = deck.cards
                .GroupBy(c => c.id)
                .SelectMany(group => group);

            foreach (CardDto card in cardsInSlotOrder)
            {
                GameObject item = UnityEngine.Object.Instantiate(cardInDeckItemPrefab, deckCardsContainer);
                CardItemUI ui = item.GetComponent<CardItemUI>();
                Button button = item.GetComponent<Button>();

                CardDto refCard = card;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onCardInDeckSelected?.Invoke(refCard));

                ui.Init(card.name, 1);
            }
        }

        private void RenderInDeckMarkers()
        {
            var cardIdsInDeck = new HashSet<long>(renderedDeck?.cards?.Select(card => card.id) ?? Enumerable.Empty<long>());
            foreach ((long cardId, CardItemUI item) in ownedCardItems)
            {
                if (item != null)
                {
                    item.SetInDeck(cardIdsInDeck.Contains(cardId));
                }
            }
        }

        private static void ClearChildren(Transform container)
        {
            foreach (Transform child in container)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }
}
