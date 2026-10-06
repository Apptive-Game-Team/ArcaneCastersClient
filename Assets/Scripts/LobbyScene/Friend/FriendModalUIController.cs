using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace LobbyScene
{
    public class FriendModalUIController : MonoBehaviour
    {
        [Header("Root & Navigation")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button friendListTabButton;
        [SerializeField] private Button requestsTabButton;
        [SerializeField] private Button searchTabButton;

        [Header("Tab Panels")]
        [SerializeField] private GameObject friendListPanel;
        [SerializeField] private GameObject requestsPanel;
        [SerializeField] private GameObject searchPanel;

        [Header("Friend List Tab")]
        [SerializeField] private Transform friendListContainer;
        [SerializeField] private FriendItemView friendItemPrefab;
        [SerializeField] private TMP_Text emptyFriendListText;
        [SerializeField] private Button refreshFriendListButton;

        [Header("Requests Tab")]
        [SerializeField] private Transform receivedRequestsContainer;
        [SerializeField] private Transform sentRequestsContainer;
        [SerializeField] private FriendRequestItemView requestItemPrefab;
        [SerializeField] private TMP_Text emptyReceivedRequestsText;
        [SerializeField] private TMP_Text emptySentRequestsText;
        [SerializeField] private Button refreshRequestsButton;

        [Header("Search Tab")]
        [SerializeField] private TMP_InputField searchInputField;
        [SerializeField] private Button searchButton;
        [SerializeField] private Transform searchResultContainer;
        [SerializeField] private FriendSearchResultItemView searchResultItemPrefab;
        [SerializeField] private TMP_Text searchStatusText;

        [Header("Feedback / Toast")]
        [SerializeField] private TMP_Text toastText;

        private FriendApiClient apiClient;
        private Coroutine toastCoroutine;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            EnsureRoot();
            if (root != null) root.SetActive(false);

            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (friendListTabButton != null) friendListTabButton.onClick.AddListener(ShowFriendListTab);
            if (requestsTabButton != null) requestsTabButton.onClick.AddListener(ShowRequestsTab);
            if (searchTabButton != null) searchTabButton.onClick.AddListener(ShowSearchTab);

            if (refreshFriendListButton != null) refreshFriendListButton.onClick.AddListener(RefreshFriendList);
            if (refreshRequestsButton != null) refreshRequestsButton.onClick.AddListener(RefreshRequests);
            if (searchButton != null) searchButton.onClick.AddListener(ExecuteSearch);
            if (searchInputField != null) searchInputField.onSubmit.AddListener(_ => ExecuteSearch());
        }

        public void Initialize(FriendApiClient client)
        {
            apiClient = client;
        }

        public void Open()
        {
            EnsureRoot();
            if (root != null) root.SetActive(true);
            ShowFriendListTab();
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
        }

        public void ShowFriendListTab()
        {
            ActivateTabPanel(friendListPanel);
            RefreshFriendList();
        }

        public void ShowRequestsTab()
        {
            ActivateTabPanel(requestsPanel);
            RefreshRequests();
        }

        public void ShowSearchTab()
        {
            ActivateTabPanel(searchPanel);
            if (searchStatusText != null) searchStatusText.text = "닉네임이나 이메일로 친구를 검색하세요.";
        }

        public void RefreshCurrentTab()
        {
            if (!IsOpen) return;
            if (friendListPanel != null && friendListPanel.activeSelf)
            {
                RefreshFriendList();
            }
            else if (requestsPanel != null && requestsPanel.activeSelf)
            {
                RefreshRequests();
            }
        }

        #region Friend List

        public void RefreshFriendList()
        {
            if (apiClient == null) return;
            StartCoroutine(apiClient.GetFriends((success, friends) =>
            {
                if (!success || friends == null)
                {
                    ShowToast("친구 목록을 불러오지 못했습니다.");
                    return;
                }

                RenderFriendList(friends);
            }));
        }

        private void RenderFriendList(List<FriendSummary> friends)
        {
            if (friendListContainer == null) return;

            ClearChildren(friendListContainer);

            bool isEmpty = friends == null || friends.Count == 0;
            if (emptyFriendListText != null)
            {
                emptyFriendListText.gameObject.SetActive(isEmpty);
            }

            if (isEmpty || friendItemPrefab == null) return;

            foreach (FriendSummary friend in friends)
            {
                FriendItemView item = Instantiate(friendItemPrefab, friendListContainer);
                item.gameObject.SetActive(true);
                item.Bind(
                    friend,
                    onInvite: OnInviteFriend,
                    onDelete: OnDeleteFriend
                );
            }
        }

        private void OnInviteFriend(FriendSummary friend)
        {
            if (apiClient == null) return;
            ShowToast($"{friend.name}님에게 친선전 초대를 보냈습니다.");

            StartCoroutine(apiClient.InviteFriend(friend.userId, (success, invite) =>
            {
                if (!success)
                {
                    ShowToast("친선전 초대를 보내지 못했습니다.");
                }
            }));
        }

        private void OnDeleteFriend(FriendSummary friend)
        {
            if (apiClient == null) return;
            StartCoroutine(apiClient.DeleteFriend(friend.userId, (success, _) =>
            {
                if (success)
                {
                    ShowToast($"{friend.name}님과 친구 관계를 삭제했습니다.");
                    RefreshFriendList();
                }
                else
                {
                    ShowToast("친구 삭제에 실패했습니다.");
                }
            }));
        }

        #endregion

        #region Requests Tab

        public void RefreshRequests()
        {
            if (apiClient == null) return;

            StartCoroutine(apiClient.GetReceivedRequests((success, list) =>
            {
                if (success && list != null)
                {
                    RenderReceivedRequests(list);
                }
            }));

            StartCoroutine(apiClient.GetSentRequests((success, list) =>
            {
                if (success && list != null)
                {
                    RenderSentRequests(list);
                }
            }));
        }

        private void RenderReceivedRequests(List<FriendRequestItem> list)
        {
            if (receivedRequestsContainer == null) return;
            ClearChildren(receivedRequestsContainer);

            bool isEmpty = list == null || list.Count == 0;
            if (emptyReceivedRequestsText != null)
            {
                emptyReceivedRequestsText.gameObject.SetActive(isEmpty);
            }

            if (isEmpty || requestItemPrefab == null) return;

            foreach (FriendRequestItem req in list)
            {
                FriendRequestItemView item = Instantiate(requestItemPrefab, receivedRequestsContainer);
                item.gameObject.SetActive(true);
                item.BindReceived(
                    req,
                    onAccept: OnAcceptRequest,
                    onReject: OnRejectRequest
                );
            }
        }

        private void RenderSentRequests(List<FriendRequestItem> list)
        {
            if (sentRequestsContainer == null) return;
            ClearChildren(sentRequestsContainer);

            bool isEmpty = list == null || list.Count == 0;
            if (emptySentRequestsText != null)
            {
                emptySentRequestsText.gameObject.SetActive(isEmpty);
            }

            if (isEmpty || requestItemPrefab == null) return;

            foreach (FriendRequestItem req in list)
            {
                FriendRequestItemView item = Instantiate(requestItemPrefab, sentRequestsContainer);
                item.gameObject.SetActive(true);
                item.BindSent(
                    req,
                    onCancel: OnCancelRequest
                );
            }
        }

        private void OnAcceptRequest(FriendRequestItem req)
        {
            if (apiClient == null) return;
            StartCoroutine(apiClient.AcceptFriendRequest(req.id, (success, _) =>
            {
                if (success)
                {
                    ShowToast("친구 요청을 수락했습니다.");
                    RefreshRequests();
                }
                else
                {
                    ShowToast("친구 요청 수락에 실패했습니다.");
                }
            }));
        }

        private void OnRejectRequest(FriendRequestItem req)
        {
            if (apiClient == null) return;
            StartCoroutine(apiClient.RejectFriendRequest(req.id, (success, _) =>
            {
                if (success)
                {
                    ShowToast("친구 요청을 거절했습니다.");
                    RefreshRequests();
                }
                else
                {
                    ShowToast("친구 요청 거절에 실패했습니다.");
                }
            }));
        }

        private void OnCancelRequest(FriendRequestItem req)
        {
            if (apiClient == null) return;
            StartCoroutine(apiClient.CancelFriendRequest(req.id, (success, _) =>
            {
                if (success)
                {
                    ShowToast("친구 요청을 취소했습니다.");
                    RefreshRequests();
                }
                else
                {
                    ShowToast("친구 요청 취소에 실패했습니다.");
                }
            }));
        }

        #endregion

        #region Search Tab

        public void ExecuteSearch()
        {
            if (searchInputField == null || apiClient == null) return;
            string query = searchInputField.text?.Trim();
            if (string.IsNullOrEmpty(query))
            {
                if (searchStatusText != null) searchStatusText.text = "검색어를 입력하세요.";
                return;
            }

            if (searchStatusText != null) searchStatusText.text = "검색 중...";
            if (searchButton != null) searchButton.interactable = false;

            StartCoroutine(apiClient.SearchMembers(query, (success, results) =>
            {
                if (searchButton != null) searchButton.interactable = true;

                if (!success || results == null)
                {
                    if (searchStatusText != null) searchStatusText.text = "검색에 실패했습니다.";
                    return;
                }

                RenderSearchResults(results);
            }));
        }

        private void RenderSearchResults(List<FriendSearchResult> results)
        {
            if (searchResultContainer == null) return;
            ClearChildren(searchResultContainer);

            if (results == null || results.Count == 0)
            {
                if (searchStatusText != null) searchStatusText.text = "검색 결과가 없습니다.";
                return;
            }

            if (searchStatusText != null) searchStatusText.text = $"{results.Count}명의 사용자를 찾았습니다.";

            if (searchResultItemPrefab == null) return;

            foreach (FriendSearchResult result in results)
            {
                FriendSearchResultItemView item = Instantiate(searchResultItemPrefab, searchResultContainer);
                item.gameObject.SetActive(true);
                item.Bind(result, onSendRequest: r => OnSendFriendRequest(r, item));
            }
        }

        private void OnSendFriendRequest(FriendSearchResult result, FriendSearchResultItemView itemView)
        {
            if (apiClient == null) return;
            string targetQuery = result.name;

            StartCoroutine(apiClient.SendFriendRequest(targetQuery, (success, _) =>
            {
                if (success)
                {
                    ShowToast($"{result.name}님에게 친구 요청을 보냈습니다.");
                    itemView.SetRequestSent();
                }
                else
                {
                    ShowToast("친구 요청 전송에 실패했습니다.");
                }
            }));
        }

        #endregion

        private void ActivateTabPanel(GameObject activePanel)
        {
            if (friendListPanel != null) friendListPanel.SetActive(friendListPanel == activePanel);
            if (requestsPanel != null) requestsPanel.SetActive(requestsPanel == activePanel);
            if (searchPanel != null) searchPanel.SetActive(searchPanel == activePanel);
        }

        private void ShowToast(string message)
        {
            if (toastText == null) return;
            toastText.text = message;
            toastText.gameObject.SetActive(true);

            if (toastCoroutine != null) StopCoroutine(toastCoroutine);
            toastCoroutine = StartCoroutine(HideToastRoutine(2.5f));
        }

        private IEnumerator HideToastRoutine(float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
            if (toastText != null) toastText.gameObject.SetActive(false);
            toastCoroutine = null;
        }

        private static void ClearChildren(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        private void EnsureRoot()
        {
            if (root == null)
            {
                root = gameObject;
            }
        }
    }
}
