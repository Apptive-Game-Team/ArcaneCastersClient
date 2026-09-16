using System.Collections.Generic;
using Data;
using Data.Magic;
using GameScene.Card;
using Global;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameScene
{
    public class GameSceneUIController : LocalSingletonObject<GameSceneUIController>
    {
        [SerializeField] private TextMeshProUGUI manaText;
        [SerializeField] private Slider manaSlider;

        [SerializeField] private CardUI cardUIPrefab;
        [SerializeField] private GameObject lowerBar;


        private ManaCostPreview manaCostPreview;
        private int currentMana;
        private int expectedManaCost;

        public void UpdateMana(int mana)
        {
            if (manaText == null || manaSlider == null) return;
            manaText.text = mana.ToString();
            manaSlider.value = mana;
            currentMana = mana;
            RefreshManaCostPreview();
        }

        /// <summary>
        /// 준비 중인 조합이 먹을 마나를 마나 바에 덧그린다. 조합이 비거나 시전 입력을 보낸 뒤에는
        /// 0을 넣어 덧그리기를 지운다.
        /// </summary>
        public void SetExpectedManaCost(int cost)
        {
            expectedManaCost = cost;
            RefreshManaCostPreview();
        }

        private void RefreshManaCostPreview()
        {
            if (manaSlider == null) return;
            manaCostPreview ??= new ManaCostPreview(manaSlider);
            manaCostPreview.Render(currentMana, expectedManaCost);
        }

        /// <summary>
        /// 손패에 카드를 한 장 붙인다. 카드 앞면은 마법별 아트다. 마법 목록이 아직 도착하지 않아
        /// magicId 를 못 찾으면 경고만 남기고 카드를 만들지 않는다.
        /// </summary>
        public void AddCard(long magicId)
        {
            if (lowerBar == null || cardUIPrefab == null) return;

            if (!LocalCombinedMagicData.TryGetById(magicId, out CombinedMagicData magic))
            {
                WDebug.LogWarning($"[GameSceneUIController] 마법 id {magicId} 를 찾지 못했다. " +
                                   "마법 목록이 아직 안 왔을 수 있다. 카드를 만들지 않는다.");
                return;
            }

            CardUI cardUI = Instantiate(cardUIPrefab, lowerBar.transform);
            cardUI.Init(magic, DeckScene.DeckCardSpriteResolver.GetMagicSprite(magic));
        }

        /// <summary>같은 마법 id 를 가진 카드를 손패에서 한 장 지운다.</summary>
        public void RemoveCard(long magicId)
        {
            foreach (Transform child in lowerBar.transform)
            {
                CardUI cardUI = child.GetComponent<CardUI>();
                if (cardUI != null && cardUI.Magic != null && cardUI.Magic.id == magicId)
                {
                    Destroy(child.gameObject);
                    return;
                }
            }
        }

        public CardUI GetCardAt(int index)
        {
            if (lowerBar == null) return null;
            return CardHotkey.FindCardAt<CardUI>(lowerBar.transform, index);
        }

        /// <summary>지금 손패에 있는 카드들의 마법 id 목록.</summary>
        public List<long> GetAllCards()
        {
            if (lowerBar == null) return new List<long>();
            List<long> magicIds = new List<long>();
            foreach (Transform child in lowerBar.transform)
            {
                CardUI cardUI = child.GetComponent<CardUI>();
                if (cardUI != null && cardUI.Magic != null)
                {
                    magicIds.Add(cardUI.Magic.id);
                }
            }
            return magicIds;
        }
    }
}
