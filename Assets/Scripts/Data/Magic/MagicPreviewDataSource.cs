using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Data.Preview;
using Global;
using Global.Serialization;
using UnityEngine;
using UnityEngine.Networking;

namespace Data.Magic
{
    /// <summary>Background startup fetch. A failed preview never holds up lobby initialization.</summary>
    public sealed class MagicPreviewDataSource : MonoBehaviour
    {
        private const int ParallelDownloads = 3;
        private static MagicPreviewDataSource instance;
        public static event Action<string> Changed;
        private readonly Dictionary<string, Saved> recordings = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<UnityWebRequest> requests = new();
        private string source;
        private int generation;
        private bool refreshing;
        private bool catalogVerified;
        private ReplayFileCache files;

        private sealed class Saved { public string hash, json; }

        public static void Refresh()
        {
            if (instance == null) {
                var root = new GameObject("MagicPreviewDataSource");
                DontDestroyOnLoad(root);
                instance = root.AddComponent<MagicPreviewDataSource>();
            }
            string url = ServerList.MatchingServer.url;
            if (instance.refreshing && instance.source == url) return;
            if (instance.source != url) instance.Cancel();
            instance.source = url;
            instance.files = new ReplayFileCache(Application.persistentDataPath, url);
            instance.refreshing = true;
            instance.StartCoroutine(instance.LoadCatalog(++instance.generation, url));
        }

        public static bool TryGet(string name, out string json)
        {
            json = null;
            if (instance == null || !instance.catalogVerified || instance.source != ServerList.MatchingServer.url ||
                string.IsNullOrEmpty(name) || !instance.recordings.TryGetValue(name, out Saved saved)) return false;
            json = saved.json;
            return true;
        }

        public static void Clear() { if (instance != null) instance.Cancel(); }

        private void Cancel()
        {
            generation++;
            foreach (UnityWebRequest request in requests.ToArray()) request.Abort();
            StopAllCoroutines();
            // Also release any iterator Unity stopped without running its finally block.
            foreach (UnityWebRequest request in requests.ToArray()) request.Dispose();
            requests.Clear();
            recordings.Clear();
            catalogVerified = false;
            refreshing = false;
            source = null;
            Changed?.Invoke(null);
        }

        private bool Current(int stamp, string url) => generation == stamp && source == url &&
            ServerList.MatchingServer.url == url && !string.IsNullOrEmpty(SceneContext.JwtToken);

        private UnityWebRequest Get(string url)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 10;
            Server.SetAuthorization(request);
            requests.Add(request);
            return request;
        }

        private IEnumerator LoadCatalog(int stamp, string url)
        {
            // Each retry starts with a fresh manifest; a rolling deployment cannot mix revisions.
            for (int attempt = 0; attempt < 3 && Current(stamp, url); attempt++) {
                PreviewCatalog catalog = null;
                using (UnityWebRequest request = Get(url + "/api/data/magic-previews")) {
                    try {
                        yield return request.SendWebRequest();
                        if (!Current(stamp, url)) yield break;
                        if (request.result == UnityWebRequest.Result.Success)
                            JsonCodec.TryDeserialize(request.downloadHandler.text, out catalog);
                    } finally { requests.Remove(request); }
                }
                if (catalog == null || catalog.status != "ready" || catalog.serverId == null ||
                    !ReplayFileCache.ValidHash(catalog.revision) || catalog.magics == null || catalog.magics.Length > 512) {
                    catalogVerified = false;
                    Changed?.Invoke(null);
                    if (attempt < 2) yield return new WaitForSecondsRealtime(5f * (attempt + 1));
                    continue;
                }
                var entries = catalog.magics.Where(ReplayFileCache.Valid).GroupBy(entry => entry.name)
                    .Select(group => group.First()).ToArray();
                var hashes = entries.ToDictionary(entry => entry.name, entry => entry.hash, StringComparer.OrdinalIgnoreCase);
                foreach (string name in recordings.Keys.ToArray())
                    if (!hashes.TryGetValue(name, out string hash) || hash != recordings[name].hash) recordings.Remove(name);
                catalogVerified = true;
                Changed?.Invoke(null);
                int active = 0;
                bool failed = false;
                bool outdated = false;
                foreach (PreviewEntry entry in entries) {
                    while (active >= ParallelDownloads && Current(stamp, url)) yield return null;
                    if (!Current(stamp, url)) yield break;
                    if (recordings.TryGetValue(entry.name, out Saved saved) && saved.hash == entry.hash) continue;
                    if (files.TryLoad(entry, out byte[] cached) && Adopt(entry, cached)) continue;
                    active++;
                    StartCoroutine(Download(entry, catalog, stamp, url, (success, changed) => {
                        active--; failed |= !success; outdated |= changed;
                    }));
                }
                while (active > 0 && Current(stamp, url)) yield return null;
                if (!Current(stamp, url)) yield break;
                files.Prune(entries.Select(entry => entry.hash));
                SyncFiles();
                if (!failed) { refreshing = false; yield break; }
                // Includes 409: validate again before using any recordings fetched under the old catalog.
                if (outdated) { catalogVerified = false; Changed?.Invoke(null); }
                if (attempt < 2) yield return new WaitForSecondsRealtime(5f * (attempt + 1));
            }
            if (Current(stamp, url)) refreshing = false;
        }

        private IEnumerator Download(PreviewEntry entry, PreviewCatalog catalog, int stamp, string url, Action<bool, bool> done)
        {
            bool success = false;
            string endpoint = url + "/api/data/magic-previews/" + UnityWebRequest.EscapeURL(entry.name) +
                "?serverId=" + catalog.serverId + "&revision=" + catalog.revision + "&hash=" + entry.hash;
            using (UnityWebRequest request = Get(endpoint)) {
                try {
                    yield return request.SendWebRequest();
                    if (Current(stamp, url) && request.result == UnityWebRequest.Result.Success) {
                        byte[] bytes = request.downloadHandler.data;
                        if (ReplayFileCache.Verify(entry, bytes)) {
                            success = Adopt(entry, bytes);
                            if (success) files.TrySave(entry, bytes);
                        }
                    }
                } finally { requests.Remove(request); done(success, request.responseCode == 409); }
            }
        }

        private bool Adopt(PreviewEntry entry, byte[] bytes)
        {
            try {
                string json = new UTF8Encoding(false, true).GetString(bytes);
                recordings[entry.name] = new Saved { hash = entry.hash, json = json };
                Changed?.Invoke(entry.name);
                return true;
            } catch (DecoderFallbackException) { return false; }
        }

        private static void SyncFiles()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SyncMagicPreviewCache();
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void SyncMagicPreviewCache();
#endif
        private void OnDestroy() { Cancel(); if (instance == this) instance = null; }
    }
}
