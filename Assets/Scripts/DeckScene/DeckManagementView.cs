using System;
using System.Collections.Generic;
using System.Linq;
using Data.Deck;
using Data.Magic;
using UnityEngine;
using UnityEngine.UI;

namespace DeckScene
{
    public class DeckManagementView
    {
        private readonly Transform deckListContainer;
        private readonly GameObject deckPrefab;
        private readonly Transform deckCardsContainer;
        private readonly Transform ownedCardsContainer;
        private readonly GameObject cardItemPrefab;
        private readonly GameObject cardInDeckItemPrefab;
        private readonly GameObject createDeckPrefab;
        private readonly Button submitDeckButton;
        private readonly Button removeDeckButton;
        private readonly InputField deckNameInputField;
        private readonly HaveCardMagicPopup ownedCardMagicPopup;

        private readonly Action<DeckResponseDto> onDeckSelected;
        private readonly Action onNewDeckSelected;
        private readonly Action<CardDto> onOwnedCardSelected;
        private readonly Action<CardDto> onCardInDeckSelected;
        private readonly Func<CardDto, IReadOnlyList<CombinedMagicData>> getOwnedCardMagicSuggestions;

        public DeckManagementView(
            Transform deckListContainer,
            GameObject deckPrefab,
            Transform deckCardsContainer,
            Transform ownedCardsContainer,
            GameObject cardItemPrefab,
            GameObject cardInDeckItemPrefab,
            GameObject createDeckPrefab,
            Button submitDeckButton,
            Button removeDeckButton,
            InputField deckNameInputField,
            Action<DeckResponseDto> onDeckSelected,
            Action onNewDeckSelected,
            Action<CardDto> onOwnedCardSelected,
            Action<CardDto> onCardInDeckSelected,
            HaveCardMagicPopup ownedCardMagicPopup,
            Func<CardDto, IReadOnlyList<CombinedMagicData>> getOwnedCardMagicSuggestions)
        {
            this.deckListContainer = deckListContainer;
            this.deckPrefab = deckPrefab;
            this.deckCardsContainer = deckCardsContainer;
            this.ownedCardsContainer = ownedCardsContainer;
            this.cardItemPrefab = cardItemPrefab;
            this.cardInDeckItemPrefab = cardInDeckItemPrefab;
            this.createDeckPrefab = createDeckPrefab;
            this.submitDeckButton = submitDeckButton;
            this.removeDeckButton = removeDeckButton;
            this.deckNameInputField = deckNameInputField;
            this.onDeckSelected = onDeckSelected;
            this.onNewDeckSelected = onNewDeckSelected;
            this.onOwnedCardSelected = onOwnedCardSelected;
            this.onCardInDeckSelected = onCardInDeckSelected;
            this.ownedCardMagicPopup = ownedCardMagicPopup;
            this.getOwnedCardMagicSuggestions = getOwnedCardMagicSuggestions;
        }

        public string DeckName => deckNameInputField.text;

        public void SetDeckName(string deckName)
        {
            deckNameInputField.SetTextWithoutNotify(deckName);
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

        public void RenderDecks(DeckResponseDto[] decks)
        {
            ClearChildren(deckListContainer);

            foreach (DeckResponseDto deck in decks)
            {
                GameObject deckObject = UnityEngine.Object.Instantiate(deckPrefab, deckListContainer);
                Button button = deckObject.GetComponent<Button>();
                DeckItemUI deckUI = deckObject.GetComponent<DeckItemUI>();

                DeckResponseDto localDeck = deck;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onDeckSelected?.Invoke(localDeck));
                deckUI.Init(deck.name);
            }

            GameObject createDeckObject = UnityEngine.Object.Instantiate(createDeckPrefab, deckListContainer);
            Button createDeckButton = createDeckObject.GetComponent<Button>();
            createDeckButton.onClick.RemoveAllListeners();
            createDeckButton.onClick.AddListener(() => onNewDeckSelected?.Invoke());
        }

        public void RenderOwnedCards(CardDto[] ownedCards)
        {
            ClearChildren(ownedCardsContainer);

            foreach (CardDto card in ownedCards)
            {
                GameObject item = UnityEngine.Object.Instantiate(cardItemPrefab, ownedCardsContainer);
                CardItemUI ui = item.GetComponent<CardItemUI>();
                Button button = item.GetComponent<Button>();

                ui.Init(card.name, card.count, card.unlocked, card.unlockText, card.progressText);
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
        }

        public void RenderDeckCards(DeckResponseDto deck)
        {
            ClearChildren(deckCardsContainer);
            if (deck == null)
            {
                return;
            }

            var summary = deck.cards
                .GroupBy(c => c.id)
                .Select(g => new
                {
                    Id = g.Key,
                    Name = g.First().name,
                    Count = g.Count()
                })
                .ToList();

            foreach (var cardInDeck in summary)
            {
                GameObject item = UnityEngine.Object.Instantiate(cardInDeckItemPrefab, deckCardsContainer);
                CardItemUI ui = item.GetComponent<CardItemUI>();
                Button button = item.GetComponent<Button>();

                CardDto refCard = deck.cards.First(c => c.id == cardInDeck.Id);

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onCardInDeckSelected?.Invoke(refCard));

                ui.Init(cardInDeck.Name, cardInDeck.Count);
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
