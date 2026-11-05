using System;
using System.Collections;
using System.Text;
using Data;
using Global.Serialization;
using UnityEngine.Networking;

namespace TutorialScene
{
    public static class TutorialBattleApi
    {
        public static IEnumerator Request<T>(string method, string url, string jwt, object body,
            Action<T, long> complete) where T : class
        {
            using var request = new UnityWebRequest(url, method);
            request.timeout = 5;
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + jwt);
            if (body != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonCodec.Serialize(body)));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            Server.SetAcceptLanguage(request);
            yield return request.SendWebRequest();
            T result = null;
            if (request.result == UnityWebRequest.Result.Success)
                JsonCodec.TryDeserialize(request.downloadHandler.text, out result);
            complete(result, request.responseCode);
        }
    }
}
