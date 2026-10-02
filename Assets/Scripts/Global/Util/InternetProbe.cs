using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Global.Util
{
    /// <summary>
    /// Tells "this device has no internet" apart from "our server is unreachable". A failed request
    /// to our own server cannot say which one it was, so the caller asks an external host instead.
    /// </summary>
    public static class InternetProbe
    {
        // Sends access-control-allow-origin: *, so the request also passes the WebGL CORS check.
        private const string ProbeUrl = "https://www.cloudflare.com/cdn-cgi/trace";
        private const int TimeoutSeconds = 5;

        public static IEnumerator IsOnline(Action<bool> onResult)
        {
            // The platform already knows the device is offline; skip the request and its timeout.
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                onResult?.Invoke(false);
                yield break;
            }

            using UnityWebRequest request = UnityWebRequest.Get(ProbeUrl);
            request.timeout = TimeoutSeconds;

            yield return request.SendWebRequest();

            bool online = request.result == UnityWebRequest.Result.Success;
            if (!online)
                WDebug.LogWarning($"[InternetProbe] Could not reach {ProbeUrl}: {request.error}");

            onResult?.Invoke(online);
        }
    }
}
