using System.Collections.Generic;
using Data;
using Data.Magic;
using GameScene;
using GameScene.Card;
using GameScene.Player;
using GameScene.PopupBook;
using GameScene.ServedObjectComponent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TutorialScene
{
    public class TutorialSceneUIController : MonoBehaviour
    {
        public static TutorialSceneUIController Instance;
        
        [SerializeField] private TextMeshProUGUI leftUserIDText;
        [SerializeField] private TextMeshProUGUI rightUserIDText;
        [SerializeField] private TextMeshProUGUI manaText;
        [SerializeField] private Slider manaSlider;
    
        [SerializeField] private Slider leftUserHpSlider;
        [SerializeField] private Slider rightUserHpSlider;
    
        private int rightUserHp = 100;
    
        [SerializeField] private TutorialCardUI cardUIPrefab;
        [SerializeField] private GameObject lowerBar;

        [SerializeField] private ExpectedMagicUI expectedMagicUI;
    
        [SerializeField] private MagicFailEffecter leftUserMagicFailEffecter;
        [SerializeField] private MagicFailEffecter rightUserMagicFailEffecter;

        private Transform leftPlayerTr;
        private Transform rightPlayerTr;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            GameObject leftObj = GameObject.Find("LeftPlayer");
            if (leftObj != null)
            {
                leftPlayerTr = leftObj.transform;
                AttachPopupBookPresenter(leftObj);
            }

            GameObject rightObj = GameObject.Find("RightPlayer");
            if (rightObj != null)
            {
                rightPlayerTr = rightObj.transform;
                AttachPopupBookPresenter(rightObj);
            }

            if (leftUserIDText == null || rightUserIDText == null) return;

#if UNITY_WEBGL && !UNITY_EDITOR
        if (Global.SceneContext.MatchInfo != null)
        {
            leftUserIDText.text = Global.SceneContext.MatchInfo.leftUser.name;
            rightUserIDText.text = Global.SceneContext.MatchInfo.rightUser.name;
        }
#endif
        }

        private static void AttachPopupBookPresenter(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            ServedObject servedObject = target.GetComponent<ServedObject>();
            if (servedObject == null)
            {
                servedObject = target.AddComponent<ServedObject>();
            }

            PopupBookVisualPresenter.Attach(servedObject);
            servedObject.BindListeners();
        }

        public void UpdateUserHps(int leftUserHp, int rightUserHp)
        {
            if (leftUserHpSlider.value > leftUserHp)
            {
                if (leftPlayerTr == null)
                {
                    GameObject leftObj = GameObject.Find("LeftPlayer");
                    if (leftObj != null) leftPlayerTr = leftObj.transform;
                }

                if (leftPlayerTr != null)
                {
                    DOTweenAction.BounceMob(leftPlayerTr.GetChild(0));
                    DamagedObjectEffect.SetSelfDestroyEffect("HitEffect", leftPlayerTr);
                }
            }
            else if (rightUserHpSlider.value > rightUserHp)
            {
                if (rightPlayerTr == null)
                {
                    GameObject rightObj = GameObject.Find("RightPlayer");
                    if (rightObj != null) rightPlayerTr = rightObj.transform;
                }

                if (rightPlayerTr != null)
                {
                    DOTweenAction.BounceMob(rightPlayerTr.GetChild(0));
                    DamagedObjectEffect.SetSelfDestroyEffect("HitEffect", rightPlayerTr);
                }
            }

            leftUserHpSlider.value = leftUserHp;
            rightUserHpSlider.value = rightUserHp;

            leftUserIDText.text = $"ME\n HP: {leftUserHp}";
            rightUserIDText.text = $"ENEMY\n HP: {rightUserHp}";
        }

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

        public void AddCard(string cardname)
        {
            if (lowerBar == null || cardUIPrefab == null) return;
            TutorialCardUI cardUI = Instantiate(cardUIPrefab, lowerBar.transform);
            // 카드 앞면은 마법마다 다른 아트다.
            cardUI.transform.GetChild(2).GetComponent<Image>().sprite =
                DeckScene.DeckCardSpriteResolver.GetMagicSprite(cardname);
            cardUI.Init(cardname);
        }

        public TutorialCardUI GetCardAt(int index)
        {
            if (lowerBar == null) return null;
            return CardHotkey.FindCardAt<TutorialCardUI>(lowerBar.transform, index);
        }

        public void TrySetExpectedMagicUI(CombinedMagicData magic)
        {
            if (expectedMagicUI == null) return;
            expectedMagicUI.SetImage(magic?.GetSprite());
        }
    
        public void PlayMagicFailEffect()
        {
            if (leftUserMagicFailEffecter != null)
            {
                leftUserMagicFailEffecter.Trigger();
            }
            else if (rightUserMagicFailEffecter != null)
            {
                rightUserMagicFailEffecter.Trigger();
            }
        }

        public void ClearAllCards()
        {
            for (int i = lowerBar.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(lowerBar.transform.GetChild(i).gameObject);
            }
        }

        public void HPMocking(int hpReduce)
        {
            rightUserHp-= hpReduce;
            UpdateUserHps(100, rightUserHp);
            if(rightUserHp <= 0) BattleTutorialManager.Instance.NotifyEnemyDead();
        }
    }
}
