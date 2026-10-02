using System;
using System.Collections;
using System.Collections.Generic;
using Data;
using UnityEngine;
using UnityEngine.Networking;
using Global.Serialization;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WordOnline.Tests.EditMode")]

namespace Global.Util
{
    /// <summary>
    /// Fetches and caches the JSON Web Key Set (JWKS) from the account server.
    /// Call FetchJwks() as a coroutine to load the keys, then use GetKey() or
    /// GetFirstKey() for synchronous access during JWT signature verification.
    /// </summary>
    public static class JwksService
    {
        private const string JwksPath = "/.well-known/jwks";
        private const int JwksTimeoutSeconds = 10; // seconds

        private static readonly Dictionary<string, JwksKey> _cachedKeys =
            new Dictionary<string, JwksKey>();

        private static bool _isFetched;

        internal static Func<string, string> WebRequestOverride { get; set; }

        /// <summary>True when JWKS has been successfully fetched and cached.</summary>
        public static bool IsFetched => _isFetched;

        internal static void Reset()
        {
            _cachedKeys.Clear();
            _isFetched = false;
            WebRequestOverride = null;
        }

        /// <summary>
        /// Coroutine that fetches the JWKS from the account server and caches
        /// the keys. On failure the cache remains empty and IsFetched stays false.
        /// </summary>
        public static IEnumerator FetchJwks()
        {
            string url = ServerList.AccountServer.url + JwksPath;

            if (WebRequestOverride != null)
            {
                string overrideText = WebRequestOverride(url);
                if (overrideText != null)
                {
                    ParseJwksResponse(overrideText);
                }
                else
                {
                    WDebug.LogWarning("[JwksService] Failed to fetch JWKS: Network error");
                }
                yield break;
            }

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = JwksTimeoutSeconds;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogWarning($"[JwksService] Failed to fetch JWKS: {request.error}");
                yield break;
            }

            ParseJwksResponse(request.downloadHandler.text);
        }

        internal static bool ParseJwksResponse(string json)
        {
            try
            {
                JwksResponse response = JsonCodec.Deserialize<JwksResponse>(json);
                if (response?.keys != null)
                {
                    _cachedKeys.Clear();
                    foreach (JwksKey key in response.keys)
                    {
                        if (!string.IsNullOrEmpty(key.kid))
                            _cachedKeys[key.kid] = key;
                    }
                    _isFetched = true;
                    WDebug.Log($"[JwksService] Fetched {_cachedKeys.Count} JWKS key(s).");
                    return true;
                }
            }
            catch (Exception ex)
            {
                WDebug.LogWarning($"[JwksService] Failed to parse JWKS response: {ex.Message}");
            }
            return false;
        }

        /// <summary>Returns the cached key matching <paramref name="kid"/>, or null.</summary>
        public static JwksKey GetKey(string kid)
        {
            if (string.IsNullOrEmpty(kid) || !_cachedKeys.TryGetValue(kid, out JwksKey key))
                return null;
            return key;
        }

        /// <summary>Returns the first cached key, or null when the cache is empty.</summary>
        public static JwksKey GetFirstKey()
        {
            foreach (var kvp in _cachedKeys)
                return kvp.Value;
            return null;
        }

        /// <summary>Returns all cached keys.</summary>
        public static IEnumerable<JwksKey> GetAllKeys()
        {
            return _cachedKeys.Values;
        }
    }
}
