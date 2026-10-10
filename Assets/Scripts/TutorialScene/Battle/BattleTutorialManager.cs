using System;
using System.Collections;
using System.Collections.Generic;
using Data.Magic;
using Global;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace TutorialScene
{
    public class BattleTutorialManager : LocalSingletonObject<BattleTutorialManager>
    {
        [SerializeField] TutorialCardSender _cardSender;
        [SerializeField] TutorialData _tutorialData;
        [SerializeField] TextMeshProUGUI _dialogueText;
        [SerializeField] ManaMocker _manaMocker;

        [Tooltip("마법을 써야 하는 단계에서 숨길 대화창. 비우면 대화 문구의 부모를 쓴다.")]
        [SerializeField] GameObject _dialogueRoot;

        private bool _advanceRequested;
        private readonly HashSet<string> _usedMagicNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _enemyDead;
        private readonly BattleTutorialPointerGuide _pointerGuide = new BattleTutorialPointerGuide();
        public event Action OnEnd;

        protected override void Awake()
        {
            base.Awake();

            _cardSender.MagicUsed += OnMagicUsed;
        }

        private void OnDestroy()
        {
            _cardSender.MagicUsed -= OnMagicUsed;
            _pointerGuide.Clear();
        }

        private void Start()
        {
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            // 전투 튜토리얼은 로비를 거치지 않고 로그인/가입 화면에서 바로 열리므로, 마법 목록과
            // parameter 를 받아오는 GameDataRefresh.Refresh 가 아직 한 번도 호출되지 않았을 수 있다.
            // 첫 단계를 그리기 전에 여기서 한 번 받아야 카드의 Magic, 마나, 사거리가 채워진다.
            yield return Data.GameDataRefresh.Refresh();

            for (int i = 0; i < _tutorialData.steps.Length; i++)
            {
                var step = _tutorialData.steps[i];

                if (step.shouldClearCards)
                {
                    ClearCard();
                }

                foreach (var name in step.cardNames)
                {
                    GiveCard(name);
                }

                yield return ShowStepMessage(step);

                switch (step.waitType)
                {
                    case TutorialWaitType.Next:
                        yield return WaitAdvance();
                        break;
                    case TutorialWaitType.UsedMagic:
                        yield return WaitUntil(() => _usedMagicNames.Contains(step.magicName), step.magicName);
                        break;
                    case TutorialWaitType.EnemyDead:
                        yield return WaitUntil(() => _enemyDead, null);
                        break;
                }
            }

            TutorialHint.HideCurrent();
            OnEnd?.Invoke();
            SceneManager.LoadScene(_tutorialData.lobbySceneName);
        }

        /// <summary>
        /// 마법을 써야 넘어가는 단계는 대화창을 숨기고 위쪽 한 줄 안내만 띄운다. 대화창은
        /// 아무 키나 눌러 넘기는 설명 단계에서만 쓴다. 대화창이 화면 아래에 떠 있으면
        /// 마법을 쓰는 동안 캐릭터와 전장을 가린다.
        /// </summary>
        private IEnumerator ShowStepMessage(TutorialStep step)
        {
            bool isAction = step.waitType != TutorialWaitType.Next;
            GameObject dialogueRoot = DialogueRoot;
            if (dialogueRoot != null)
            {
                dialogueRoot.SetActive(!isAction);
            }

            if (isAction)
            {
                TutorialHint.Show(_tutorialData.stringTableName, step.localizationKey);
                yield break;
            }

            TutorialHint.HideCurrent();
            yield return SetLocalizedDialogue(step.localizationKey);
        }

        /// <summary>
        /// 대화창 루트. 씬에서 따로 지정하지 않으면 문구의 부모(TutorialSelectPanel 루트)를 쓴다.
        /// </summary>
        private GameObject DialogueRoot
        {
            get
            {
                if (_dialogueRoot != null)
                {
                    return _dialogueRoot;
                }

                Transform parent = _dialogueText != null ? _dialogueText.transform.parent : null;
                return parent != null ? parent.gameObject : null;
            }
        }

        private IEnumerator SetLocalizedDialogue(string key)
        {
            var handle = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(_tutorialData.stringTableName, key);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                _dialogueText.text = handle.Result;
            else
                _dialogueText.text = key;
        }

        private IEnumerator WaitAdvance()
        {
            _advanceRequested = false;

            while (Input.anyKey)
                yield return null;

            while (!_advanceRequested)
            {
                if (Input.anyKeyDown)
                    _advanceRequested = true;

                yield return null;
            }
        }

        /// <summary>마법을 써야 넘어가는 단계. 기다리는 동안 손가락이 써야 할 카드를 가리킨다.</summary>
        /// <param name="magicName">써야 할 마법. null 이면 아무 카드나 가리킨다.</param>
        private IEnumerator WaitUntil(System.Func<bool> condition, string magicName)
        {
            while (!condition())
            {
                _pointerGuide.Update(magicName, _cardSender);
                yield return null;
            }

            _pointerGuide.Clear();
        }

        public void RequestAdvance()
        {
            _advanceRequested = true;
        }

        public void NotifyEnemyDead()
        {
            _enemyDead = true;
        }

        private void OnMagicUsed(IReadOnlyList<CombinedMagicData> magics)
        {
            foreach (var magic in magics)
            {
                if (magic == null)
                {
                    continue;
                }

                _usedMagicNames.Add(magic.serverName);
                _manaMocker.UseMana(CardManaCost.Of(magic));
            }
        }

        void GiveCard(string name)
        {
            TutorialSceneUIController.Instance.AddCard(name);
        }

        void ClearCard()
        {
            TutorialSceneUIController.Instance.ClearAllCards();
            _cardSender.CancelAll();
        }
    }
}
