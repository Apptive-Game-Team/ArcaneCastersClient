using System.Collections;
using System.Collections.Generic;
using Data.Adventures.Local;
using Data.Profile;
using Data.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ProfileScene
{
    /// <summary>
    /// The profile's quest list: an overlay panel that loads <c>GET /api/users/mine/quests</c> every time it
    /// opens and draws one <see cref="QuestItem"/> per quest. Opened by a <c>GameObjectVisibilityButton</c>
    /// on ProfileScene and closed by <see cref="Close"/>.
    /// </summary>
    public class QuestPanel : MonoBehaviour
    {
        private const string LobbyTableName = "LobbyUI";

        [SerializeField] private QuestApiClient apiClient;
        [SerializeField] private QuestItemFactory itemFactory;
        [SerializeField] private TMP_Text statusText;

        /// <summary>Source of adventure names for ADVENTURE_CLEAR quests, matched by adventureId.</summary>
        [SerializeField] private List<AdventureScriptableObject> adventures = new List<AdventureScriptableObject>();

        private readonly QuestProgressPresenterRegistry presenterRegistry = QuestProgressPresenterRegistry.CreateDefault();
        private Coroutine refreshCoroutine;

        private void Awake()
        {
            if (apiClient == null)
            {
                apiClient = GetComponent<QuestApiClient>();
            }

            if (itemFactory == null)
            {
                itemFactory = GetComponent<QuestItemFactory>();
            }
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnDisable()
        {
            refreshCoroutine = null;
        }

        public void Refresh()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (refreshCoroutine != null)
            {
                StopCoroutine(refreshCoroutine);
            }

            refreshCoroutine = StartCoroutine(RefreshCoroutine());
        }

        /// <summary>Wired to the close button. <c>GlobalButtonSoundPlayer</c> already plays the click.</summary>
        public void Close()
        {
            gameObject.SetActive(false);
        }

        private IEnumerator RefreshCoroutine()
        {
            if (itemFactory != null)
            {
                itemFactory.ClearItems();
            }

            AsyncOperationHandle<StringTable> tableHandle = LocalizationSettings.StringDatabase.GetTableAsync(LobbyTableName);
            yield return tableHandle;
            StringTable table = tableHandle.Status == AsyncOperationStatus.Succeeded ? tableHandle.Result : null;
            var text = new LocalizedQuestText(table, adventures);

            SetStatus(string.Empty);
            if (apiClient == null)
            {
                SetStatus(text.Localize(QuestLocalizationKeys.LoadFailed, "Could not load quests."));
                refreshCoroutine = null;
                yield break;
            }

            bool done = false;
            QuestDto[] quests = null;
            apiClient.GetQuests(response =>
            {
                quests = response;
                done = true;
            });
            yield return new WaitUntil(() => done);

            if (quests == null)
            {
                SetStatus(text.Localize(QuestLocalizationKeys.LoadFailed, "Could not load quests."));
                refreshCoroutine = null;
                yield break;
            }

            QuestDto[] ordered = QuestListOrder.Order(quests);
            if (ordered.Length == 0)
            {
                SetStatus(text.Localize(QuestLocalizationKeys.Empty, "No quests right now."));
            }

            if (itemFactory != null)
            {
                itemFactory.CreateRange(ordered, presenterRegistry, text);
            }

            refreshCoroutine = null;
        }

        private void SetStatus(string message)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = message;
            statusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
    }
}
