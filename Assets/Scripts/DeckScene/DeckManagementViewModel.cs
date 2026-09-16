using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data;
using Data.Deck;
using Data.Magic;
using Global;
using Global.Util;

namespace DeckScene
{
    public enum DeckEditMode
    {
        None,
        Create,
        Update
    }

    public readonly struct DeckRequirementSummary
    {
        public DeckRequirementSummary(int cardCount)
        {
            CardCount = cardCount;
        }

        public int CardCount { get; }
    }

    public class DeckManagementViewModel
    {
        /// <summary>덱 한 벌의 카드 수. lobby 의 DeckValidator 가 쓰는 값과 같아야 한다.</summary>
        public const int DeckCardCount = 15;

        private static DeckResponseDto[] cachedUserDecks;
        private static CardDto[] cachedOwnedCards;

        private readonly DeckApiClient deckApiClient = new DeckApiClient();

        /// <summary>
        /// Drops decks and cards cached from the matching server, so the next fetch reloads them.
        /// </summary>
        public static void ClearCachedUserData()
        {
            cachedUserDecks = null;
            cachedOwnedCards = null;
        }

        public DeckResponseDto[] UserDecks => cachedUserDecks ?? Array.Empty<DeckResponseDto>();
        public CardDto[] OwnedCards => cachedOwnedCards ?? Array.Empty<CardDto>();
        public DeckResponseDto CurrentDeck { get; private set; }
        public DeckEditMode CurrentMode { get; private set; } = DeckEditMode.None;
        public bool HasCachedData => UserDecks.Length > 0 && OwnedCards.Length > 0;
        public bool CanDeleteCurrentDeck => CurrentMode == DeckEditMode.Update && CurrentDeck != null;
        public bool CanSubmitCurrentDeck => (CurrentDeck?.cards?.Length ?? 0) == DeckCardCount;

        public void SelectDeck(DeckResponseDto deck)
        {
            CurrentDeck = CloneDeck(deck);
            CurrentMode = DeckEditMode.Update;
            DeckSceneContext.CurrentDeck = CurrentDeck;
        }

        public void SelectNewDeck(string deckName)
        {
            CurrentDeck = new DeckResponseDto
            {
                id = -1,
                name = deckName,
                cards = Array.Empty<CardDto>()
            };
            CurrentMode = DeckEditMode.Create;
            DeckSceneContext.CurrentDeck = CurrentDeck;
        }

        public bool TryAddOwnedCard(CardDto card)
        {
            if (CurrentDeck == null || card == null || !card.unlocked)
            {
                return false;
            }

            // 덱은 15장이다. 같은 마법 장수와 원소 종류에는 제한이 없고, 가진 장수만 본다.
            int ownedCount = card.count;
            int inDeckCount = CurrentDeck.cards.Count(c => c.id == card.id);
            if (CurrentDeck.cards.Length >= DeckCardCount || inDeckCount >= ownedCount)
            {
                return false;
            }

            CurrentDeck.cards = CurrentDeck.cards.Append(card).ToArray();
            DeckSceneContext.CurrentDeck = CurrentDeck;
            return true;
        }

        public bool TryRemoveCard(CardDto card)
        {
            if (CurrentDeck == null || card == null)
            {
                return false;
            }

            var cardList = CurrentDeck.cards.ToList();
            CardDto toDelete = cardList.FirstOrDefault(c => c.id == card.id);
            if (toDelete == null)
            {
                return false;
            }

            cardList.Remove(toDelete);
            CurrentDeck.cards = cardList.ToArray();
            DeckSceneContext.CurrentDeck = CurrentDeck;
            return true;
        }

        public DeckRequirementSummary GetCurrentDeckSummary()
        {
            return new DeckRequirementSummary(CurrentDeck?.cards?.Length ?? 0);
        }

        /// <summary>
        /// 이 카드로 쓸 수 있는 마법. 카드 한 장이 곧 마법 하나이므로 그 마법 하나다.
        /// TODO(#577): 덱 화면이 정리되면 "이 카드로 만들 수 있는 마법" 목록 UI 자체가 없어진다.
        /// </summary>
        public IReadOnlyList<CombinedMagicData> GetOwnedCardMagicSuggestions(CardDto card)
        {
            if (!TryGetMagic(card, out CombinedMagicData magic))
            {
                return Array.Empty<CombinedMagicData>();
            }

            return new List<CombinedMagicData> { magic };
        }

        /// <summary>덱에 든 마법 목록. 덱의 카드가 곧 마법이다.</summary>
        public IReadOnlyList<CombinedMagicData> GetCurrentDeckAvailableMagics()
        {
            var magics = new List<CombinedMagicData>();
            foreach (CardDto card in CurrentDeck?.cards ?? Array.Empty<CardDto>())
            {
                if (TryGetMagic(card, out CombinedMagicData magic) &&
                    !magics.Exists(existing => existing.id == magic.id))
                {
                    magics.Add(magic);
                }
            }

            return magics
                .OrderBy(magic => magic.localizationKey)
                .ToList();
        }

        public IEnumerator LoadAll(Action<bool> callback)
        {
            yield return GameDataRefresh.Refresh();

            CardDto[] ownedCards = null;
            yield return deckApiClient.GetOwnedCards(cards => ownedCards = cards);
            if (ownedCards == null)
            {
                callback?.Invoke(false);
                yield break;
            }

            DeckResponseDto[] userDecks = null;
            yield return deckApiClient.GetDecks(decks => userDecks = decks);
            if (userDecks == null)
            {
                callback?.Invoke(false);
                yield break;
            }

            cachedOwnedCards = ownedCards;
            cachedUserDecks = userDecks;
            DeckSceneContext.OwnedCards = cachedOwnedCards;
            callback?.Invoke(true);
        }

        public IEnumerator SubmitCurrentDeck(string deckName, Action<DeckEditMode, bool> callback)
        {
            if (CurrentDeck == null)
            {
                callback?.Invoke(CurrentMode, false);
                yield break;
            }

            DeckRequestDto requestDto = new DeckRequestDto
            {
                name = deckName,
                cardIds = CurrentDeck.cards.Select(c => c.id).ToArray()
            };

            if (CurrentMode == DeckEditMode.Create)
            {
                bool isSuccess = false;
                DeckResponseDto createdDeck = null;
                yield return deckApiClient.CreateDeck(requestDto, (success, deck) =>
                {
                    isSuccess = success;
                    createdDeck = deck;
                });

                if (isSuccess)
                {
                    long createdDeckId = createdDeck?.id ?? 0;
                    if (createdDeckId <= 0)
                    {
                        yield return ResolveDeckId(requestDto, deckId => createdDeckId = deckId);
                    }

                    if (createdDeckId > 0)
                    {
                        CurrentDeck.id = createdDeckId;
                        yield return SelectSubmittedDeck(createdDeckId);
                    }
                }

                callback?.Invoke(DeckEditMode.Create, isSuccess);
                yield break;
            }

            bool updateSuccess = false;
            yield return deckApiClient.UpdateDeck(CurrentDeck.id, requestDto, isSuccess => updateSuccess = isSuccess);

            if (updateSuccess)
            {
                yield return SelectSubmittedDeck(CurrentDeck.id);
            }

            callback?.Invoke(DeckEditMode.Update, updateSuccess);
        }

        public IEnumerator DeleteCurrentDeck(Action<bool> callback)
        {
            if (!CanDeleteCurrentDeck)
            {
                callback?.Invoke(false);
                yield break;
            }

            yield return deckApiClient.DeleteDeck(CurrentDeck.id, callback);
        }

        private static DeckResponseDto CloneDeck(DeckResponseDto deck)
        {
            if (deck == null)
            {
                return null;
            }

            return new DeckResponseDto
            {
                id = deck.id,
                name = deck.name,
                cards = deck.cards?.ToArray() ?? Array.Empty<CardDto>()
            };
        }

        private IEnumerator SelectSubmittedDeck(long deckId)
        {
            bool isSelected = false;
            yield return deckApiClient.SelectDeck(deckId, success => isSelected = success);

            if (isSelected && SceneContext.User != null)
            {
                SceneContext.User.selectedDeckId = deckId;
            }
        }

        private IEnumerator ResolveDeckId(DeckRequestDto requestDto, Action<long> callback)
        {
            DeckResponseDto[] decks = null;
            yield return deckApiClient.GetDecks(result => decks = result);

            DeckResponseDto matchingDeck = decks?
                .Where(deck => deck.name == requestDto.name && HasSameCards(deck, requestDto.cardIds))
                .LastOrDefault();

            callback?.Invoke(matchingDeck?.id ?? 0);
        }

        private static bool HasSameCards(DeckResponseDto deck, long[] cardIds)
        {
            long[] deckCardIds = deck.cards?.Select(card => card.id).OrderBy(id => id).ToArray() ?? Array.Empty<long>();
            long[] requestCardIds = cardIds?.OrderBy(id => id).ToArray() ?? Array.Empty<long>();
            return deckCardIds.SequenceEqual(requestCardIds);
        }

        private static bool TryGetMagic(CardDto card, out CombinedMagicData magic)
        {
            magic = null;
            if (card == null)
            {
                return false;
            }

            return LocalCombinedMagicData.TryGetById(card.id, out magic) ||
                   CardNameMapper.TryMapToMagic(card.name, out magic);
        }
    }
}
