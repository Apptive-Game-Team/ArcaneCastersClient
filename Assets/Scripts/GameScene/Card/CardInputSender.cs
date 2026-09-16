using System.Collections.Generic;
using Data;
using Data.Magic;
using GameScene.Dto;
using GameScene.ServedObjectComponent;
using Global;
using UnityEngine;
using Global.Serialization;

namespace GameScene.Card
{
    public class CardInputSender : LocalSingletonObject<CardInputSender>, ICardSender
    {
        private class PendingInputRequest
        {
            public readonly List<CardUI> cards;

            public PendingInputRequest(List<CardUI> cards)
            {
                this.cards = cards;
            }
        }

        private const string Success = "SUCCESS";
        private const string FailInvalidMagic = "FAIL_INVALID_MAGIC";

        // 훈수 시스템이 구독한다. 정적 이벤트이므로 구독자는 씬을 떠날 때 반드시 해제해야 한다.
        public static event System.Action OnCardUsed;
        public static event System.Action OnMagicFailed;
        public static event System.Action OnMagicSucceeded;

        private readonly Dictionary<int, PendingInputRequest> inputRequestDict = new Dictionary<int, PendingInputRequest>();
        private readonly List<string> _currentCardNameList = new List<string>();
        private readonly List<CardUI> _currentCardList = new List<CardUI>();

        public bool CanSelectField => _currentCardList.Count >= 1;
        private bool isFieldSelectMode = false;
        private bool isWaitingInputResponse = false;
        private BarController barController;

        public bool IsFieldSelectMode()
        {
            return isFieldSelectMode;    
        }

        public bool IsWaitingInputResponse()
        {
            return isWaitingInputResponse;
        }

        public void CancelUseCard(CardUI cardObj)
        {
            if (isWaitingInputResponse)
            {
                return;
            }

            WDebug.Log($"CancelUseCard: {cardObj.CardName}");
            if (_currentCardList.Contains(cardObj))
            {
                _currentCardNameList.Remove(cardObj.CardName);
                _currentCardList.Remove(cardObj);
                if (cardObj.Magic != null)
                {
                    SendCardSelectionInput(new CardUnselectRequestDto(cardObj.Magic.id));
                }
                isFieldSelectMode = CanSelectField;
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                Cancel();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                // 마나 바가 내려가 있으면 손패가 보이지 않는다. 스페이스는 마나 바를 올리는
                // 입력으로만 쓴다. 카드 한 장을 고르는 순간 이미 필드 선택 모드로 들어가므로
                // 스페이스가 따로 확정할 것은 없다.
                TryOpenManaBar();
            }

            if (CardHotkey.TryGetPressedSlotIndex(out int slotIndex))
            {
                ToggleCardBySlot(slotIndex);
            }
        }

        /// <summary>
        /// 내려가 있는 마나 바를 올린다. 올릴 것이 없으면 false를 돌려 호출자가 확정으로 넘어가게 한다.
        /// </summary>
        private bool TryOpenManaBar()
        {
            if (barController == null)
            {
                barController = FindObjectOfType<BarController>();
            }

            return barController != null && barController.TryOpenBar();
        }

        private void ToggleCardBySlot(int slotIndex)
        {
            if (isWaitingInputResponse || GameSceneUIController.Instance == null)
            {
                return;
            }

            CardUI card = GameSceneUIController.Instance.GetCardAt(slotIndex);
            if (card != null)
            {
                card.OnCardClicked();
            }
        }

        public void Cancel()
        {
            if (isWaitingInputResponse)
            {
                return;
            }

            if (isFieldSelectMode)
            {
                CancelAll();
            }
        }
    
        public string GetMagicName()
        {
            return _currentCardNameList.Count > 0 ? _currentCardNameList[0] : null;
        }

        /// <summary>
        /// 지금 고른 카드의 마법. 손패에서 한 번에 한 장만 고르므로 목록의 첫 장을 본다.
        /// TODO(#576): 손패와 시전 흐름이 한 장 선택으로 정리되면 목록 자체가 카드 한 장으로 바뀐다.
        /// </summary>
        public bool TryGetCurrentMagicData(out CombinedMagicData data)
        {
            data = _currentCardList.Count > 0 ? _currentCardList[0].Magic : null;
            return data != null;
        }
    
        private void CancelAll()
        {
            WDebug.Log("CancelAll");
            foreach (var card in _currentCardList)
            {
                card.SetCardActive(false);
                if (card.Magic != null)
                {
                    SendCardSelectionInput(new CardUnselectRequestDto(card.Magic.id));
                }
            }
            _currentCardList.Clear();
            _currentCardNameList.Clear();

            SetExpectedMagicUI();
            isFieldSelectMode = false;
        }

        public void TryUseCard(CardUI cardObj)
        {
            if (isWaitingInputResponse)
            {
                return;
            }

            // 카드 한 장이 마법 하나의 시전이므로, 이미 고른 카드가 있으면 그 선택을 버리고
            // 새로 고른 카드로 바꾼다.
            if (_currentCardList.Count > 0)
            {
                ReplaceSelection(cardObj);
            }
            else
            {
                AddCardList(cardObj);
            }

            OnCardUsed?.Invoke();

            // 카드 한 장이 곧 마법 하나이므로, 고르는 순간 바로 필드 선택 모드로 들어간다.
            isFieldSelectMode = TryGetCurrentMagicData(out _);
        }

        /// <summary>이미 고른 카드가 있을 때 새 카드로 선택을 바꾼다. 이전 카드는 서버에도 선택 해제를 보낸다.</summary>
        private void ReplaceSelection(CardUI newCard)
        {
            foreach (CardUI previousCard in _currentCardList)
            {
                if (previousCard == null || previousCard == newCard)
                {
                    continue;
                }

                previousCard.SetCardActive(false);
                if (previousCard.Magic != null)
                {
                    SendCardSelectionInput(new CardUnselectRequestDto(previousCard.Magic.id));
                }
            }

            _currentCardList.Clear();
            _currentCardNameList.Clear();
            AddCardList(newCard);
        }

        public void SendInput(Vector3 pos) //whenFieldSelect
        {
            if (isWaitingInputResponse)
            {
                return;
            }

            if (!TryGetCurrentMagicData(out CombinedMagicData magic))
            {
                WDebug.LogWarning("[CardInputSender] No magic to cast. Input dropped.");
                return;
            }

            var input = new CardUseInput(magic.id, pos);
            string json = JsonCodec.Serialize(input);
        
            string destination = $"/app/game/input/{SceneContext.MatchInfo.sessionId}/{SceneContext.UserID}";
            StompConnector.Instance.SendMessageToServer(destination, json);
            inputRequestDict[input.id] = new PendingInputRequest(new List<CardUI>(_currentCardList));
            isWaitingInputResponse = true;
            _currentCardNameList.Clear();
            _currentCardList.Clear();
            isFieldSelectMode = false;

            // 카드가 손을 떠났으니 마나 바에 남은 예상 소모량을 지운다. 응답이 실패로 오면
            // RestorePendingSelection이 선택을 되살리면서 다시 그린다.
            if (GameSceneUIController.Instance != null)
            {
                GameSceneUIController.Instance.SetExpectedManaCost(0);
            }

            StopCoroutine(nameof(WaitInputResponseTimeout));
            StartCoroutine(nameof(WaitInputResponseTimeout));
        }

        public bool TrySendInput(Vector3 pos)
        {
            if (!isFieldSelectMode || isWaitingInputResponse)
            {
                return false;
            }

            SendInput(pos);
            return isWaitingInputResponse;
        }

        private System.Collections.IEnumerator WaitInputResponseTimeout()
        {
            yield return new WaitForSeconds(3f);
            if (isWaitingInputResponse)
            {
                WDebug.LogWarning("[CardInputSender] Input response timeout. Releasing lock.");
                isWaitingInputResponse = false;
                
                // 타임아웃 시 남아있는 PendingRequest 처리 (카드를 다시 복구하거나 파괴)
                foreach (var kvp in inputRequestDict)
                {
                    RestorePendingSelection(kvp.Value.cards);
                }
                inputRequestDict.Clear();
            }
        }

        public void HandleInputResponse(MagicValidInfo magicValid)
        {
            StopCoroutine(nameof(WaitInputResponseTimeout));

            if (!inputRequestDict.TryGetValue(magicValid.id, out PendingInputRequest pendingInputRequest))
            {
                WDebug.LogWarning($"[Magic Valid] Pending input request not found. id: {magicValid.id}");
                isWaitingInputResponse = false;
                return;
            }

            if (ShouldConsumeCards(magicValid))
            {
                foreach (CardUI cardUI in pendingInputRequest.cards)
                {
                    if (cardUI != null)
                    {
                        cardUI.Destroy();
                    }
                }

                isWaitingInputResponse = false;

                // FAIL_INVALID_MAGIC도 카드를 소비하므로 이 분기에 들어온다. 카드가 사라졌다고
                // 시전에 성공한 것은 아니라서 결과 코드로 다시 갈라야 한다.
                if (IsSuccess(magicValid))
                {
                    OnMagicSucceeded?.Invoke();
                }
                else
                {
                    OnMagicFailed?.Invoke();
                }
            }
            else
            {
                RestorePendingSelection(pendingInputRequest.cards);
                SystemMessageUI.Instance.ShowMessage(magicValid.message);
                WDebug.Log("[Magic Valid]" + magicValid.message);
                OnMagicFailed?.Invoke();
            }

            inputRequestDict.Remove(magicValid.id);
        }

        private void RestorePendingSelection(List<CardUI> cards)
        {
            isWaitingInputResponse = false;

            foreach (CardUI currentCard in _currentCardList)
            {
                if (currentCard != null)
                {
                    currentCard.SetCardActive(false);
                }
            }

            _currentCardNameList.Clear();
            _currentCardList.Clear();

            foreach (CardUI card in cards)
            {
                if (card == null)
                {
                    continue;
                }

                card.SetCardActive(true);
                _currentCardNameList.Add(card.CardName);
                _currentCardList.Add(card);
            }

            isFieldSelectMode = CanSelectField;
            SetExpectedMagicUI();
        }

        private static bool IsSuccess(MagicValidInfo magicValid)
        {
            if (string.IsNullOrEmpty(magicValid.resultCode))
            {
                return magicValid.valid;
            }

            return string.Equals(magicValid.resultCode, Success, System.StringComparison.Ordinal);
        }

        private static bool ShouldConsumeCards(MagicValidInfo magicValid)
        {
            if (string.IsNullOrEmpty(magicValid.resultCode))
            {
                return magicValid.valid;
            }

            return string.Equals(magicValid.resultCode, Success, System.StringComparison.Ordinal)
                   || string.Equals(magicValid.resultCode, FailInvalidMagic, System.StringComparison.Ordinal);
        }
    
        private void AddCardList(CardUI card)
        {
            WDebug.Log("AddCardList: " + card.CardName);
            _currentCardNameList.Add(card.CardName);
            _currentCardList.Add(card);
            if (card.Magic != null)
            {
                SendCardSelectionInput(new CardSelectRequestDto(card.Magic.id));
            }
        }

        private static void SendCardSelectionInput(object input)
        {
            string json = JsonCodec.Serialize(input);
            string destination = $"/app/game/input/{SceneContext.MatchInfo.sessionId}/{SceneContext.UserID}";
            StompConnector.Instance.SendMessageToServer(destination, json);
        }

        public void SetExpectedMagicUI()
        {
            if (GameSceneUIController.Instance == null)
            {
                return;
            }

            TryGetCurrentMagicData(out CombinedMagicData magic);
            GameSceneUIController.Instance.SetExpectedManaCost(CardManaCost.Of(magic));
        }
    }
}
