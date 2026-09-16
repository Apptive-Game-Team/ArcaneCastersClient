using System;
using System.Collections.Generic;
using Data;
using Data.Magic;
using GameScene.Card;
using GameScene.PopupBook;
using GameScene.ServedObjectComponent;
using Global;
using Unity.Mathematics;
using UnityEngine;

namespace TutorialScene
{
    public class TutorialCardSender : MonoBehaviour, ICardSender
    {
        private static readonly Vector3 CasterPosition = new Vector3(1f, 0f, 5f);

        public event Action<IReadOnlyList<CombinedMagicData>> MagicUsed;

        private TutorialCardUI _currentCard;

        [SerializeField] GameObject shotPrefab;
        [SerializeField] GameObject mobPrefab;

        public bool CanSelectField => _currentCard != null;
        private bool isFieldSelectMode = false;

        public bool IsFieldSelectMode()
        {
            return isFieldSelectMode;
        }

        public void CancelUseCard(TutorialCardUI cardObj)
        {
            if (_currentCard == cardObj)
            {
                _currentCard = null;
                isFieldSelectMode = CanSelectField;
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                Cancel();
            }

            if (CardHotkey.TryGetPressedSlotIndex(out int slotIndex))
            {
                ToggleCardBySlot(slotIndex);
            }
        }

        private void ToggleCardBySlot(int slotIndex)
        {
            if (TutorialSceneUIController.Instance == null)
            {
                return;
            }

            TutorialCardUI card = TutorialSceneUIController.Instance.GetCardAt(slotIndex);
            if (card != null)
            {
                card.OnCardClicked();
            }
        }

        public void Cancel()
        {
            if (isFieldSelectMode)
            {
                CancelAll();
            }
        }

        public string GetMagicName()
        {
            return _currentCard != null ? _currentCard.CardName : null;
        }

        /// <summary>지금 고른 카드의 마법. 한 번에 한 장만 고른다.</summary>
        public CombinedMagicData GetCurrentMagic()
        {
            return _currentCard != null ? _currentCard.Magic : null;
        }

        public void CancelAll()
        {
            WDebug.Log("CancelAll");
            _currentCard?.SetCardActive(false);
            _currentCard = null;
            SetExpectedMagicUI();
            isFieldSelectMode = false;
        }

        public void TryUseCard(TutorialCardUI cardObj)
        {
            // 카드 한 장이 마법 하나의 시전이므로, 이미 고른 카드가 있으면 그 선택을 버리고
            // 새로 고른 카드로 바꾼다.
            if (_currentCard != null && _currentCard != cardObj)
            {
                _currentCard.SetCardActive(false);
            }

            _currentCard = cardObj;

            // 카드 한 장이 곧 마법 하나이므로, 고르는 순간 바로 필드 선택 모드로 들어간다.
            isFieldSelectMode = cardObj.Magic != null;

            if (isFieldSelectMode)
            {
                return;
            }

            // 마법 목록이 아직 도착하지 않은 카드다. 고른 카드는 그대로 두고 조준만 막는다.
            // 부르는 쪽(TutorialCardUI.OnCardClicked)이 이 호출 뒤에 카드를 선택 표시로 바꾸므로,
            // 여기서 선택을 비우면 표시와 실제 선택이 어긋난다.
            WDebug.LogWarning($"[TutorialCardSender] No magic data for card: {cardObj.CardName}");
            TutorialSceneUIController.Instance?.PlayMagicFailEffect();
        }

        public void SendInput(Vector3 pos)
        {
            CombinedMagicData magic = GetCurrentMagic();
            var magics = magic != null
                ? new List<CombinedMagicData> { magic }
                : new List<CombinedMagicData>();

            MagicUsed?.Invoke(magics);

            // 튜토리얼은 서버 없이 도는 흉내라 실제 시전 결과를 받지 않고 연출만 두 가지로 가른다.
            // 레인 조준 마법(투사체)은 시전자 자리에서 날아가야 하니 shotPrefab, 그 밖에는 찍은
            // 자리에 하수인이 세워지는 mobPrefab.
            if (magic != null && GameScene.MagicIndicatorResolver.IsLaneAim(magic))
            {
                AttachPopupBookPresenter(Instantiate(shotPrefab, CasterPosition, quaternion.identity));
            }
            else if (magic != null)
            {
                AttachPopupBookPresenter(Instantiate(mobPrefab, pos, quaternion.identity));
            }

            _currentCard = null;
            isFieldSelectMode = false;

            // 카드가 손을 떠났으니 마나 바에 남은 예상 소모량을 지운다.
            TutorialSceneUIController.Instance.SetExpectedManaCost(0);
        }

        private static void AttachPopupBookPresenter(GameObject target)
        {
            ServedObject servedObject = target.GetComponent<ServedObject>();
            if (servedObject == null)
            {
                servedObject = target.AddComponent<ServedObject>();
            }

            PopupBookVisualPresenter.Attach(servedObject);
            servedObject.BindListeners();
        }

        public void TryUseCard(CardUI cardObj)
        {
            throw new NotImplementedException();
        }

        public void SetExpectedMagicUI()
        {
            CombinedMagicData magic = GetCurrentMagic();
            TutorialSceneUIController.Instance.TrySetExpectedMagicUI(magic);
            TutorialSceneUIController.Instance.SetExpectedManaCost(CardManaCost.Of(magic));
        }
    }
}
