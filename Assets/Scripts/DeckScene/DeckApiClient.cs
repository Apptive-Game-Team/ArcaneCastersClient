using System;
using System.Collections;
using System.Text;
using Data;
using Data.Deck;
using Global;
using Global.Util;
using UnityEngine;
using UnityEngine.Networking;
using Global.Serialization;

namespace DeckScene
{
    public class DeckApiClient
    {
        public IEnumerator GetOwnedCards(Action<CardDto[]> callback)
        {
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/cardLists";
            using var request = UnityWebRequest.Get(url);
            request.timeout = 8;

            Server.SetAcceptLanguage(request);
            Server.SetAuthorization(request);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogError(
                    $"보유 카드 풀 로드 실패: {request.responseCode} / {request.error}\n{request.downloadHandler.text}"
                );
                callback?.Invoke(null);
                yield break;
            }

            WDebug.Log($"보유 카드 리스트: {request.downloadHandler.text}");
            string poolBody = request.downloadHandler.text;
            if (!JsonCodec.TryDeserialize(poolBody, out CardPoolDto poolDto, out string poolError))
            {
                WDebug.LogError($"보유 카드 풀 파싱 실패: {poolError}\n{JsonCodec.Excerpt(poolBody)}");
                callback?.Invoke(null);
                yield break;
            }

            callback?.Invoke(poolDto?.cards ?? Array.Empty<CardDto>());
        }

        public IEnumerator GetDecks(Action<DeckResponseDto[]> callback)
        {
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/decks";
            using var request = UnityWebRequest.Get(url);

            Server.SetAcceptLanguage(request);
            Server.SetAuthorization(request);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogError(
                    $"유저 덱 리스트 로드 실패: {request.responseCode} / {request.error}\n{request.downloadHandler.text}"
                );
                callback?.Invoke(null);
                yield break;
            }

            WDebug.Log($"유저 덱 리스트: {request.downloadHandler.text}");
            string decksBody = request.downloadHandler.text;
            if (!JsonCodec.TryDeserialize(decksBody, out DeckResponseDto[] decks, out string decksError))
            {
                WDebug.LogError($"유저 덱 리스트 파싱 실패: {decksError}\n{JsonCodec.Excerpt(decksBody)}");
                callback?.Invoke(null);
                yield break;
            }

            callback?.Invoke(decks ?? Array.Empty<DeckResponseDto>());
        }

        public IEnumerator CreateDeck(DeckRequestDto dto, Action<bool> callback)
        {
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/decks";
            yield return SendDeckRequest(url, "POST", dto, "POST 덱 페이로드", "덱 생성 실패", callback);
        }

        public IEnumerator CreateDeck(DeckRequestDto dto, Action<bool, DeckResponseDto> callback)
        {
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/decks";
            yield return SendDeckRequest(
                url,
                "POST",
                dto,
                "POST deck payload",
                "deck creation failed",
                (isSuccess, responseText) => callback?.Invoke(isSuccess, ParseDeckResponse(responseText)));
        }

        public IEnumerator UpdateDeck(long deckId, DeckRequestDto dto, Action<bool> callback)
        {
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/decks/{deckId}";
            yield return SendDeckRequest(url, "PUT", dto, "PUT 덱 페이로드", "덱 수정 실패", callback);
        }

        public IEnumerator DeleteDeck(long deckId, Action<bool> callback)
        {
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/decks/{deckId}";
            using var request = new UnityWebRequest(url, "DELETE")
            {
                downloadHandler = new DownloadHandlerBuffer()
            };

            Server.SetAuthorization(request);
            Server.SetAcceptLanguage(request);

            yield return request.SendWebRequest();

            bool isSuccess = request.result == UnityWebRequest.Result.Success;
            if (!isSuccess)
            {
                WDebug.LogError($"덱 삭제 실패: {request.responseCode} / {request.error}\n{request.downloadHandler.text}");
            }
            else
            {
                WDebug.Log($"DELETE 덱 성공: {request.downloadHandler.text}");
            }

            callback?.Invoke(isSuccess);
        }

        public IEnumerator SelectDeck(long deckId, Action<bool> callback)
        {
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/decks/{deckId}";
            using var request = UnityWebRequest.Post(url, new WWWForm());

            Server.SetAuthorization(request);
            Server.SetAcceptLanguage(request);

            yield return request.SendWebRequest();

            bool isSuccess = request.result == UnityWebRequest.Result.Success;
            if (!isSuccess)
            {
                WDebug.LogError($"Deck selection failed: {request.responseCode} / {request.error}\n{request.downloadHandler.text}");
            }
            else
            {
                WDebug.Log($"Deck selection succeeded: {request.downloadHandler.text}");
            }

            callback?.Invoke(isSuccess);
        }

        private IEnumerator SendDeckRequest(
            string url,
            string method,
            DeckRequestDto dto,
            string payloadLogPrefix,
            string failureLogPrefix,
            Action<bool> callback)
        {
            string json = JsonCodec.Serialize(dto);
            WDebug.Log($"{payloadLogPrefix}: {json}");

            using var request = new UnityWebRequest(url, method)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };

            Server.SetAuthorization(request);
            Server.SetAcceptLanguage(request);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            bool isSuccess = request.result == UnityWebRequest.Result.Success;
            if (!isSuccess)
            {
                WDebug.LogError($"{failureLogPrefix}: {request.responseCode} / {request.error}\n{request.downloadHandler.text}");
            }
            else
            {
                WDebug.Log($"{method} 덱 성공: {request.downloadHandler.text}");
            }

            callback?.Invoke(isSuccess);
        }

        private IEnumerator SendDeckRequest(
            string url,
            string method,
            DeckRequestDto dto,
            string payloadLogPrefix,
            string failureLogPrefix,
            Action<bool, string> callback)
        {
            string json = JsonCodec.Serialize(dto);
            WDebug.Log($"{payloadLogPrefix}: {json}");

            using var request = new UnityWebRequest(url, method)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };

            Server.SetAuthorization(request);
            Server.SetAcceptLanguage(request);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            bool isSuccess = request.result == UnityWebRequest.Result.Success;
            if (!isSuccess)
            {
                WDebug.LogError($"{failureLogPrefix}: {request.responseCode} / {request.error}\n{request.downloadHandler.text}");
            }
            else
            {
                WDebug.Log($"{method} succeeded: {request.downloadHandler.text}");
            }

            callback?.Invoke(isSuccess, request.downloadHandler.text);
        }

        private static DeckResponseDto ParseDeckResponse(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return null;
            }

            // The old catch only covered ArgumentException, which a JsonSerializationException is not.
            if (!JsonCodec.TryDeserialize(responseText, out DeckResponseDto deck, out string error))
            {
                WDebug.LogWarning($"Deck response parse failed: {error}\n{JsonCodec.Excerpt(responseText)}");
                return null;
            }

            return deck;
        }
    }
}
