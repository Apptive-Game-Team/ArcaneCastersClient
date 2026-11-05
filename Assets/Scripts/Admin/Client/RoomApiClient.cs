using System;
using System.Collections;
using System.Collections.Generic;
using Admin.Dto;
using Data;
using UnityEngine;
using UnityEngine.Networking;
using Global.Serialization;

namespace Admin.Client
{
    public class RoomApiClient
    {
        private const string LocalGameServerUrl = "http://127.0.0.1:7777";
        private const string LobbyGameSessionsPath = "/api/game-sessions";
        private const string LocalGameSessionsPath = "/api/debug/game-sessions";
        private const int OptionalSourceTimeoutSeconds = 2;

        public IEnumerator GetRoomList(Action<RoomList> callback)
        {
            var rooms = new List<RoomInfo>();

            yield return FetchRoomList(
                ServerList.MatchingServer.url,
                LobbyGameSessionsPath,
                "Lobby",
                false,
                false,
                roomList => AddRooms(rooms, roomList));

            yield return FetchRoomList(
                LocalGameServerUrl,
                LocalGameSessionsPath,
                "Local",
                true,
                true,
                roomList => AddRooms(rooms, roomList));

            callback(new RoomList { rooms = rooms });
        }

        private IEnumerator FetchRoomList(
            string baseUrl,
            string path,
            string sourceName,
            bool isLocalSource,
            bool optional,
            Action<RoomList> callback)
        {
            string url = baseUrl + path;

            using UnityWebRequest request = UnityWebRequest.Get(url);

            Server.SetAcceptLanguage(request);
            Server.SetAuthorization(request);
            request.downloadHandler = new DownloadHandlerBuffer();
            if (optional)
            {
                request.timeout = OptionalSourceTimeoutSeconds;
            }

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string message = $"Error fetching {sourceName} room list from {url}: {request.error}";
                if (optional)
                {
                    Debug.LogWarning(message);
                }
                else
                {
                    Debug.LogError(message);
                }
                callback(null);
            }
            else
            {
                string jsonResponse = request.downloadHandler.text;
                if (!JsonCodec.TryDeserialize(jsonResponse, out RoomList roomList, out string error))
                {
                    Debug.LogError($"Room list parse error: {error} / {JsonCodec.Excerpt(jsonResponse)}");
                    callback(null);
                    yield break;
                }

                ApplySource(roomList, sourceName, baseUrl, isLocalSource);
                callback(roomList);
            }
        }

        private static void ApplySource(RoomList roomList, string sourceName, string baseUrl, bool isLocalSource)
        {
            if (roomList?.rooms == null)
            {
                return;
            }

            foreach (RoomInfo roomInfo in roomList.rooms)
            {
                roomInfo.sourceName = sourceName;
                roomInfo.sourceBaseUrl = baseUrl;
                roomInfo.isLocalSource = isLocalSource;
                if (isLocalSource)
                {
                    roomInfo.serverUrl = baseUrl;
                }
            }
        }

        private static void AddRooms(List<RoomInfo> rooms, RoomList roomList)
        {
            if (roomList?.rooms == null)
            {
                return;
            }

            rooms.AddRange(roomList.rooms);
        }
    }
}
