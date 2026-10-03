using System.Collections;
using System.Collections.Generic;
using Data;
using Data.Appearances;
using Data.Profile;
using Data.Quests;
using Global;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace ProfileScene
{
    /// <summary>
    /// The appearance picker: every appearance of <c>GET /api/users/mine/appearances</c> as a tile, a large
    /// preview of the picked one, and a confirm that sends <c>PUT /api/users/mine/appearance</c>.
    /// <para>
    /// On success it writes the new key into <c>SceneContext.User.appearance</c>. The lobby reads that field
    /// on every load (<c>LobbyUIController.ApplyOwnAppearance</c>), so the lobby character changes without a
    /// restart; in a match the body is drawn from the appearance the server sends with the match, which the
    /// server reads from the same stored selection.
    /// </para>
    /// </summary>
    public class AppearancePanel : MonoBehaviour
    {
        private const string LobbyTableName = "LobbyUI";

        public const string NameKeyPrefix = "Appearance";
        public const string ChangedKey = "AppearanceChanged";
        public const string NotOwnedKey = "AppearanceNotOwned";
        public const string UnknownKey = "AppearanceUnknown";
        public const string FailedKey = "AppearanceSelectFailed";
        public const string LoadFailedKey = "AppearanceLoadFailed";

        [SerializeField] private AppearanceApiClient apiClient;
        [SerializeField] private AppearanceTile tilePrefab;
        [SerializeField] private Transform tileRoot;
        [SerializeField] private Image previewImage;
        [SerializeField] private TMP_Text previewNameText;
        [SerializeField] private TMP_Text statusText;

        private readonly List<AppearanceTile> tiles = new List<AppearanceTile>();
        private AppearanceDto[] catalog = new AppearanceDto[0];
        private StringTable table;
        private string pickedKey;
        private bool isBusy;
        private Coroutine refreshCoroutine;

        private void Awake()
        {
            if (apiClient == null)
            {
                apiClient = GetComponent<AppearanceApiClient>();
            }
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnDisable()
        {
            refreshCoroutine = null;
            isBusy = false;
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

        /// <summary>Wired to the confirm button. Does nothing while a request runs or when nothing changed.</summary>
        public void Confirm()
        {
            if (isBusy || apiClient == null || string.IsNullOrEmpty(pickedKey))
            {
                return;
            }

            if (pickedKey == AppearanceCatalog.FindSelectedKey(catalog, null))
            {
                Close();
                return;
            }

            isBusy = true;
            string requestedKey = pickedKey;
            apiClient.SelectAppearance(requestedKey, (outcome, selectedKey) => OnSelected(outcome, requestedKey, selectedKey));
        }

        private void OnSelected(AppearanceSelectionOutcome outcome, string requestedKey, string selectedKey)
        {
            isBusy = false;

            if (outcome == AppearanceSelectionOutcome.Success)
            {
                string key = string.IsNullOrEmpty(selectedKey) ? requestedKey : selectedKey;
                if (SceneContext.User != null)
                {
                    SceneContext.User.appearance = key;
                }

                AppearanceCatalog.MarkSelected(catalog, key);
                Pick(key);
                ShowMessage(Localize(ChangedKey, "Appearance changed."));
                return;
            }

            switch (outcome)
            {
                case AppearanceSelectionOutcome.NotOwned:
                    ShowMessage(Localize(NotOwnedKey, "You do not own this appearance yet."));
                    break;
                case AppearanceSelectionOutcome.UnknownAppearance:
                    ShowMessage(Localize(UnknownKey, "This appearance no longer exists."));
                    break;
                default:
                    ShowMessage(Localize(FailedKey, "Could not change the appearance. Please try again."));
                    break;
            }

            // The catalog may have changed under us (an appearance removed or not granted), so reload it.
            Refresh();
        }

        private IEnumerator RefreshCoroutine()
        {
            if (table == null)
            {
                AsyncOperationHandle<StringTable> tableHandle = LocalizationSettings.StringDatabase.GetTableAsync(LobbyTableName);
                yield return tableHandle;
                table = tableHandle.Status == AsyncOperationStatus.Succeeded ? tableHandle.Result : null;
            }

            SetStatus(string.Empty);
            if (apiClient == null)
            {
                SetStatus(Localize(LoadFailedKey, "Could not load appearances."));
                refreshCoroutine = null;
                yield break;
            }

            bool done = false;
            AppearanceDto[] response = null;
            apiClient.GetAppearances(appearances =>
            {
                response = appearances;
                done = true;
            });
            yield return new WaitUntil(() => done);

            if (response == null)
            {
                SetStatus(Localize(LoadFailedKey, "Could not load appearances."));
                refreshCoroutine = null;
                yield break;
            }

            catalog = AppearanceCatalog.Order(response);
            BuildTiles();
            Pick(AppearanceCatalog.FindSelectedKey(catalog, SceneContext.User?.appearance ?? PlayerAppearanceResolver.DefaultId));
            refreshCoroutine = null;
        }

        private void BuildTiles()
        {
            foreach (AppearanceTile tile in tiles)
            {
                if (tile != null)
                {
                    Destroy(tile.gameObject);
                }
            }

            tiles.Clear();
            if (tilePrefab == null || tileRoot == null)
            {
                WDebug.LogError("AppearancePanel has no tile prefab or tile root.");
                return;
            }

            foreach (AppearanceDto appearance in catalog)
            {
                AppearanceTile tile = Instantiate(tilePrefab, tileRoot);
                tile.gameObject.SetActive(true);
                tile.Bind(appearance, DisplayName(appearance.key), QuestRewardSpriteResolver.ResolveAppearanceSprite(appearance.key), OnTilePicked);
                tiles.Add(tile);
            }
        }

        private void OnTilePicked(AppearanceDto appearance)
        {
            if (!isBusy && appearance != null)
            {
                Pick(appearance.key);
            }
        }

        private void Pick(string key)
        {
            pickedKey = key;
            foreach (AppearanceTile tile in tiles)
            {
                if (tile != null)
                {
                    tile.SetPicked(tile.Key == key);
                }
            }

            if (previewImage != null)
            {
                Sprite preview = QuestRewardSpriteResolver.ResolveAppearanceSprite(key);
                previewImage.sprite = preview;
                previewImage.enabled = preview != null;
                previewImage.preserveAspect = true;
            }

            if (previewNameText != null)
            {
                previewNameText.text = DisplayName(key);
            }
        }

        /// <summary>
        /// The <c>LobbyUI</c> entry <c>Appearance&lt;PascalKey&gt;</c> (<c>storm</c> → <c>AppearanceStorm</c>),
        /// else the key itself in title case, so a key added on the server still gets a readable name.
        /// </summary>
        private string DisplayName(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string humanized = QuestTextFormat.Humanize(key);
            return Localize(NameKeyPrefix + humanized.Replace(" ", string.Empty), humanized);
        }

        private string Localize(string key, string fallback)
        {
            StringTableEntry entry = table != null ? table.GetEntry(key) : null;
            string localized = entry?.GetLocalizedString();
            return string.IsNullOrEmpty(localized) ? fallback : localized;
        }

        private void ShowMessage(string message)
        {
            if (SystemMessageUI.Instance != null)
            {
                SystemMessageUI.Instance.ShowMessage(message);
                return;
            }

            SetStatus(message);
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
