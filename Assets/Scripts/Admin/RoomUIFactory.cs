using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Admin.Dto;
using TMPro;
using UnityEngine;

namespace Admin
{
    public class RoomUIFactory : MonoBehaviour
    {
        [SerializeField] private AdminViewModel adminViewModel;
        [SerializeField] private GameObject roomUIPrefab;
        [SerializeField] private Transform contentTransform;
        [SerializeField] private TMP_Text roomCountText;
        [SerializeField] private UnityEngine.UI.InputField filterInput;

        private readonly List<RoomInfo> fetchedRooms = new List<RoomInfo>();

        private void Awake()
        {
            if (filterInput != null) filterInput.onValueChanged.AddListener(OnFilterChanged);
        }

        private void OnDestroy()
        {
            if (filterInput != null) filterInput.onValueChanged.RemoveListener(OnFilterChanged);
        }

        private void Start()
        {
            SetCountText("Rooms: loading...");
            adminViewModel.FetchRoomList(OnRoomListFetched);
        }

        private void OnRoomListFetched(Dto.RoomList roomList)
        {
            fetchedRooms.Clear();

            if (roomList?.rooms == null)
            {
                SetCountText("Rooms: fetch failed");
                return;
            }

            fetchedRooms.AddRange(roomList.rooms);
            Render();
        }

        public void ReloadRoomList()
        {
            fetchedRooms.Clear();
            ClearRows();
            SetCountText("Rooms: loading...");
            adminViewModel.FetchRoomList(OnRoomListFetched);
        }

        // Filtering re-renders from the rooms already in hand. Refetching per keystroke would fan out
        // to every game server through the lobby, and the list would shift under the user as they type.
        private void OnFilterChanged(string _)
        {
            Render();
        }

        private void Render()
        {
            ClearRows();

            string filter = filterInput == null ? string.Empty : filterInput.text.Trim();
            int shown = 0;

            foreach (RoomInfo roomInfo in fetchedRooms)
            {
                if (!Matches(roomInfo, filter)) continue;

                var roomUIGameObject = Instantiate(roomUIPrefab, contentTransform);
                var roomUI = roomUIGameObject.GetComponent<RoomUI>();
                roomUI.SetRoomInfo(roomInfo);
                shown++;
            }

            SetCountText(FormatCount(shown, filter));
        }

        private static bool Matches(RoomInfo roomInfo, string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;

            return Contains(roomInfo.sessionId, filter)
                   || Contains(roomInfo.leftUserId.ToString(CultureInfo.InvariantCulture), filter)
                   || Contains(roomInfo.rightUserId.ToString(CultureInfo.InvariantCulture), filter);
        }

        private static bool Contains(string value, string filter)
        {
            return !string.IsNullOrEmpty(value)
                   && value.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ClearRows()
        {
            foreach (Transform child in contentTransform)
            {
                Destroy(child.gameObject);
            }
        }

        // The list concatenates every source (Lobby, then Local), so a bare total hides which source a
        // missing room came from. Break it down whenever more than one source answered.
        private string FormatCount(int shown, string filter)
        {
            var label = new StringBuilder("Rooms: ");
            label.Append(string.IsNullOrEmpty(filter) ? $"{fetchedRooms.Count}" : $"{shown} / {fetchedRooms.Count}");

            var countsBySource = new Dictionary<string, int>();
            foreach (RoomInfo roomInfo in fetchedRooms)
            {
                string sourceName = string.IsNullOrEmpty(roomInfo.sourceName) ? "Unknown" : roomInfo.sourceName;
                countsBySource.TryGetValue(sourceName, out int count);
                countsBySource[sourceName] = count + 1;
            }

            if (countsBySource.Count > 1)
            {
                label.Append(" (");
                bool first = true;
                foreach (var entry in countsBySource)
                {
                    if (!first) label.Append(", ");
                    label.Append($"{entry.Key} {entry.Value}");
                    first = false;
                }
                label.Append(')');
            }

            return label.ToString();
        }

        private void SetCountText(string text)
        {
            if (roomCountText != null) roomCountText.text = text;
        }
    }
}
