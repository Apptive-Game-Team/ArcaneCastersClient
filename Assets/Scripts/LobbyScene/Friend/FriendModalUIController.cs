using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LobbyScene
{
    /// <summary>
    /// The lobby friend modal. The layout lives in <c>Assets/Prefabs/UI/Lobby/Friend/FriendModal.prefab</c>,
    /// instanced once in <c>LobbyScene</c> and saved inactive. This component sits on the prefab root,
    /// which is also the full-screen dim overlay: a click that lands on the overlay itself closes the modal.
    /// <para>
    /// Rows are instances of the three row prefabs, created into the tab's list on every refresh.
    /// Static labels are localized in the prefabs; the status messages below are set here.
    /// </para>
    /// </summary>
    public class FriendModalUIController : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color PrimaryOrange = new Color32(0xFF, 0x9A, 0x1F, 0xFF);
        private static readonly Color TabInactiveBg = new Color32(0xEE, 0xF3, 0xF8, 0xFF);
        private static readonly Color SlateTextColor = new Color32(0x5B, 0x62, 0x75, 0xFF);

        private enum Tab
        {
            Search,
            Friends,
            Requests
        }

        [Header("Header & Tabs")]
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private UnityEngine.UI.Button searchTabButton;
        [SerializeField] private UnityEngine.UI.Button friendsTabButton;
        [SerializeField] private UnityEngine.UI.Button requestsTabButton;
        [SerializeField] private GameObject searchTitle;
        [SerializeField] private GameObject friendsTitle;
        [SerializeField] private GameObject requestsTitle;

        [Header("Tab Panels")]
        [SerializeField] private GameObject searchPanel;
        [SerializeField] private GameObject friendsPanel;
        [SerializeField] private GameObject requestsPanel;

        [Header("Search Tab")]
        [SerializeField] private InputField searchInputField;
        [SerializeField] private UnityEngine.UI.Button searchButton;
        [SerializeField] private Transform searchResultContainer;
        [SerializeField] private TMP_Text searchStatusText;

        [Header("Friends Tab")]
        [SerializeField] private Transform friendListContainer;
        [SerializeField] private TMP_Text friendStatusText;

        [Header("Requests Tab")]
        [SerializeField] private Transform receivedRequestsContainer;
        [SerializeField] private TMP_Text receivedStatusText;
        [SerializeField] private Transform sentRequestsContainer;
        [SerializeField] private TMP_Text sentStatusText;

        [Header("Row Prefabs")]
        [SerializeField] private FriendItemView friendRowPrefab;
        [SerializeField] private FriendRequestItemView requestRowPrefab;
        [SerializeField] private FriendSearchResultItemView searchResultRowPrefab;

        private readonly List<GameObject> searchRows = new List<GameObject>();
        private readonly List<GameObject> friendRows = new List<GameObject>();
        private readonly List<GameObject> receivedRows = new List<GameObject>();
        private readonly List<GameObject> sentRows = new List<GameObject>();

        private FriendApiClient apiClient;
        private Tab currentTab = Tab.Search;
        private bool wired;

        // Each refresh bumps its counter, so a response that arrives after a newer request is dropped.
        private int searchVersion;
        private int friendsVersion;
        private int requestsVersion;

        public bool IsOpen => gameObject.activeSelf;

        // The F hotkey toggles the modal; it must not fire while the player types into the search field.
        public bool IsTyping => searchInputField != null && searchInputField.isFocused;

        private void Awake()
        {
            Wire();
        }

        public void Initialize(FriendApiClient client)
        {
            apiClient = client;
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
                return;
            }

            Open();
        }

        public void Open()
        {
            Wire();
            gameObject.SetActive(true);
            ShowSearchTab();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        // Clicks on the window bubble up to here too; only a click on the dim overlay closes the modal.
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.gameObject != gameObject)
            {
                return;
            }

            Close();
        }

        public void RefreshCurrentTab()
        {
            if (!IsOpen) return;

            if (currentTab == Tab.Friends)
            {
                ShowFriendsTab();
            }
            else if (currentTab == Tab.Requests)
            {
                ShowRequestsTab();
            }
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (searchTabButton != null) searchTabButton.onClick.AddListener(ShowSearchTab);
            if (friendsTabButton != null) friendsTabButton.onClick.AddListener(ShowFriendsTab);
            if (requestsTabButton != null) requestsTabButton.onClick.AddListener(ShowRequestsTab);
            if (searchButton != null) searchButton.onClick.AddListener(ExecuteSearch);
            if (searchInputField != null) searchInputField.onSubmit.AddListener(_ => ExecuteSearch());
        }

        private void SelectTab(Tab tab)
        {
            currentTab = tab;

            if (searchPanel != null) searchPanel.SetActive(tab == Tab.Search);
            if (friendsPanel != null) friendsPanel.SetActive(tab == Tab.Friends);
            if (requestsPanel != null) requestsPanel.SetActive(tab == Tab.Requests);

            if (searchTitle != null) searchTitle.SetActive(tab == Tab.Search);
            if (friendsTitle != null) friendsTitle.SetActive(tab == Tab.Friends);
            if (requestsTitle != null) requestsTitle.SetActive(tab == Tab.Requests);

            SetTabStyle(searchTabButton, tab == Tab.Search);
            SetTabStyle(friendsTabButton, tab == Tab.Friends);
            SetTabStyle(requestsTabButton, tab == Tab.Requests);
        }

        private static void SetTabStyle(UnityEngine.UI.Button button, bool isSelected)
        {
            if (button == null) return;

            if (button.image != null)
            {
                button.image.color = isSelected ? PrimaryOrange : TabInactiveBg;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = isSelected ? Color.white : SlateTextColor;
            }
        }

        #region Search Tab

        public void ShowSearchTab()
        {
            SelectTab(Tab.Search);
            searchVersion++;
            if (searchInputField != null) searchInputField.text = string.Empty;
            ClearRows(searchRows);
            HideStatus(searchStatusText);
        }

        public void ExecuteSearch()
        {
            if (searchInputField == null || apiClient == null) return;
            string query = searchInputField.text?.Trim();
            if (string.IsNullOrEmpty(query)) return;

            int version = ++searchVersion;
            ClearRows(searchRows);
            ShowStatus(searchStatusText, $"'{query}' 검색 중...", PrimaryOrange);

            StartCoroutine(apiClient.SearchMembers(query, (success, results) =>
            {
                if (version != searchVersion) return;

                if (!success || results == null || results.Count == 0)
                {
                    ShowStatus(searchStatusText, "검색 결과가 없습니다.", SlateTextColor);
                    return;
                }

                HideStatus(searchStatusText);
                if (searchResultRowPrefab == null || searchResultContainer == null) return;

                foreach (FriendSearchResult result in results)
                {
                    FriendSearchResultItemView row = Instantiate(searchResultRowPrefab, searchResultContainer);
                    row.Bind(result, OnSendFriendRequest);
                    searchRows.Add(row.gameObject);
                }
            }));
        }

        private void OnSendFriendRequest(FriendSearchResult result)
        {
            if (apiClient == null) return;
            StartCoroutine(apiClient.SendFriendRequest(result.name, (success, request) =>
            {
                if (success) ExecuteSearch();
            }));
        }

        #endregion

        #region Friends Tab

        public void ShowFriendsTab()
        {
            SelectTab(Tab.Friends);
            int version = ++friendsVersion;
            ClearRows(friendRows);
            ShowStatus(friendStatusText, "친구 목록 불러오는 중...", PrimaryOrange);

            if (apiClient == null) return;
            StartCoroutine(apiClient.GetFriends((success, friends) =>
            {
                if (version != friendsVersion) return;

                if (!success || friends == null || friends.Count == 0)
                {
                    ShowStatus(friendStatusText, "등록된 친구가 없습니다. [친구 검색]에서 친구를 추가해보세요!", SlateTextColor);
                    return;
                }

                HideStatus(friendStatusText);
                if (friendRowPrefab == null || friendListContainer == null) return;

                foreach (FriendSummary friend in friends)
                {
                    FriendItemView row = Instantiate(friendRowPrefab, friendListContainer);
                    row.Bind(friend, OnInviteFriend, OnDeleteFriend);
                    friendRows.Add(row.gameObject);
                }
            }));
        }

        private void OnInviteFriend(FriendSummary friend)
        {
            if (apiClient == null) return;
            StartCoroutine(apiClient.InviteFriend(friend.userId, (success, invite) => { }));
        }

        private void OnDeleteFriend(FriendSummary friend)
        {
            if (apiClient == null) return;
            StartCoroutine(apiClient.DeleteFriend(friend.userId, success =>
            {
                if (success) ShowFriendsTab();
            }));
        }

        #endregion

        #region Requests Tab

        public void ShowRequestsTab()
        {
            SelectTab(Tab.Requests);
            int version = ++requestsVersion;
            ClearRows(receivedRows);
            ClearRows(sentRows);
            HideStatus(receivedStatusText);
            HideStatus(sentStatusText);

            if (apiClient == null) return;
            StartCoroutine(apiClient.GetReceivedRequests((success, requests) =>
            {
                if (version != requestsVersion) return;

                if (!success || requests == null || requests.Count == 0)
                {
                    ShowStatus(receivedStatusText, "받은 친구 요청이 없습니다.", SlateTextColor);
                    return;
                }

                foreach (FriendRequestItem request in requests)
                {
                    FriendRequestItemView row = CreateRequestRow(receivedRequestsContainer, receivedRows);
                    if (row != null) row.BindReceived(request, OnAcceptRequest, OnRejectRequest);
                }
            }));

            StartCoroutine(apiClient.GetSentRequests((success, requests) =>
            {
                if (version != requestsVersion) return;

                if (!success || requests == null || requests.Count == 0)
                {
                    ShowStatus(sentStatusText, "보낸 친구 요청이 없습니다.", SlateTextColor);
                    return;
                }

                foreach (FriendRequestItem request in requests)
                {
                    FriendRequestItemView row = CreateRequestRow(sentRequestsContainer, sentRows);
                    if (row != null) row.BindSent(request, OnCancelRequest);
                }
            }));
        }

        private FriendRequestItemView CreateRequestRow(Transform container, List<GameObject> rows)
        {
            if (requestRowPrefab == null || container == null) return null;
            FriendRequestItemView row = Instantiate(requestRowPrefab, container);
            rows.Add(row.gameObject);
            return row;
        }

        private void OnAcceptRequest(FriendRequestItem request)
        {
            if (apiClient == null) return;
            StartCoroutine(apiClient.AcceptFriendRequest(request.id, (success, accepted) =>
            {
                if (success) ShowRequestsTab();
            }));
        }

        private void OnRejectRequest(FriendRequestItem request)
        {
            if (apiClient == null) return;
            StartCoroutine(apiClient.RejectFriendRequest(request.id, (success, rejected) =>
            {
                if (success) ShowRequestsTab();
            }));
        }

        private void OnCancelRequest(FriendRequestItem request)
        {
            if (apiClient == null) return;
            StartCoroutine(apiClient.CancelFriendRequest(request.id, success =>
            {
                if (success) ShowRequestsTab();
            }));
        }

        #endregion

        private static void ShowStatus(TMP_Text status, string message, Color color)
        {
            if (status == null) return;
            status.text = message;
            status.color = color;
            status.gameObject.SetActive(true);
        }

        private static void HideStatus(TMP_Text status)
        {
            if (status != null) status.gameObject.SetActive(false);
        }

        private static void ClearRows(List<GameObject> rows)
        {
            foreach (GameObject row in rows)
            {
                if (row != null) Destroy(row);
            }

            rows.Clear();
        }
    }
}
