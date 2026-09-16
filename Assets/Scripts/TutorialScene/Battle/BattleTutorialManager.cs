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

        private bool _advanceRequested;
        private readonly HashSet<string> _usedMagicNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _enemyDead;
        public event Action OnEnd;

        protected override void Awake()
        {
            base.Awake();

            _cardSender.MagicUsed += OnMagicUsed;
        }

        private void OnDestroy()
        {
            _cardSender.MagicUsed -= OnMagicUsed;
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

                yield return SetLocalizedDialogue(step.localizationKey);

                switch (step.waitType)
                {
                    case TutorialWaitType.Next:
                        yield return WaitAdvance();
                        break;
                    case TutorialWaitType.UsedMagic:
                        yield return WaitUntil(() => _usedMagicNames.Contains(step.magicName));
                        break;
                    case TutorialWaitType.EnemyDead:
                        yield return WaitUntil(() => _enemyDead);
                        break;
                }
            }

            OnEnd?.Invoke();
            SceneManager.LoadScene(_tutorialData.lobbySceneName);
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

        private IEnumerator WaitUntil(System.Func<bool> condition)
        {
            while (!condition())
                yield return null;
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
