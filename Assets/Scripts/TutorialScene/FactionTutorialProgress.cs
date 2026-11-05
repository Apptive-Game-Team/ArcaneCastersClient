using System;
using System.Collections;
using Data;
using Global;
using Global.Serialization;
using UnityEngine;

namespace TutorialScene
{
    [Serializable, UnityEngine.Scripting.Preserve]
    public sealed class FactionTutorialCheckpoint
    {
        [UnityEngine.Scripting.Preserve] public FactionTutorialCheckpoint() { }
        public string state = "pending";
        public string requestId;
        public bool comicSeen;
        public TutorialReadyDto battle;
    }

    public static class FactionTutorialProgress
    {
        public const string SelectionScene = "FactionSelectionScene";
        public const string BattleScene = "FactionTutorialBattleScene";
        private static string Key(long owner) => $"faction-tutorial-v1:{ServerList.MatchingServer.url}:{owner}";

        private static string AccountIntentKey()
        {
            string payload = Global.Util.JwtHelper.DecodePayload(SceneContext.JwtToken);
            return JsonCodec.TryDeserialize(payload, out AccountIdentity identity) && identity.memberId > 0
                ? $"new-account-tutorial-v1:{ServerList.MatchingServer.url}:{identity.memberId}" : null;
        }

        public static void MarkNewAccountIntent()
        {
            string key = AccountIntentKey();
            if (key == null) return;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }

        public static void AdoptNewAccountIntent(long owner)
        {
            string key = AccountIntentKey();
            if (key == null || !PlayerPrefs.HasKey(key)) return;
            Begin(owner);
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        public static IEnumerator PrepareNewAccountIntro()
        {
            SceneContext.User = null;
            yield return LobbyScene.UserInfoGetter.GetUserInfo();
            if (SceneContext.User != null)
            {
                AdoptNewAccountIntent(SceneContext.UserID);
                Begin(SceneContext.UserID);
            }
        }

        public static FactionTutorialCheckpoint Read(long owner)
        {
            string json = PlayerPrefs.GetString(Key(owner), "");
            if (string.IsNullOrEmpty(json)) return null;
            return JsonCodec.TryDeserialize(json, out FactionTutorialCheckpoint result) ? result : null;
        }

        public static bool IsPending(long owner) => owner > 0 && Read(owner)?.state == "pending";
        public static bool IsLobbyPending(long owner) => owner > 0 && Read(owner)?.state == "lobby";
        public static string ResumeScene(long owner) => Read(owner)?.comicSeen == true ? SelectionScene : "ComicIntroScene";

        public static void MarkComicSeen(long owner)
        {
            var checkpoint = Begin(owner);
            checkpoint.comicSeen = true;
            Save(owner, checkpoint);
        }

        public static FactionTutorialCheckpoint Begin(long owner)
        {
            var checkpoint = Read(owner);
            if (checkpoint == null || checkpoint.state != "pending")
                checkpoint = new FactionTutorialCheckpoint { requestId = Guid.NewGuid().ToString() };
            if (string.IsNullOrEmpty(checkpoint.requestId)) checkpoint.requestId = Guid.NewGuid().ToString();
            Save(owner, checkpoint);
            return checkpoint;
        }

        public static void Save(long owner, FactionTutorialCheckpoint checkpoint)
        {
            PlayerPrefs.SetString(Key(owner), JsonCodec.Serialize(checkpoint));
            PlayerPrefs.Save();
        }

        public static void CompleteBattle(long owner)
        {
            Save(owner, new FactionTutorialCheckpoint { state = "lobby", comicSeen = true });
        }

        public static void Complete(long owner)
        {
            Save(owner, new FactionTutorialCheckpoint { state = "completed" });
        }

        public static bool IsCurrentOwner(long owner, string matchingServer) => SceneContext.User != null
            && SceneContext.UserID == owner && !string.IsNullOrEmpty(SceneContext.JwtToken)
            && ServerList.MatchingServer.url == matchingServer;

        public static bool IsValidReady(TutorialReadyDto ready, long owner, string faction)
        {
            return ready != null && ready.ownerId == owner && ready.faction == faction
                && ready.battleId != null && ready.battleId.StartsWith("tutorial-", StringComparison.Ordinal)
                && Uri.TryCreate(ready.server, UriKind.Absolute, out var uri)
                && (uri.Scheme == "https" || uri.Scheme == "http") && string.IsNullOrEmpty(uri.UserInfo)
                && DateTimeOffset.TryParse(ready.expiresAt, out var expiry) && expiry > DateTimeOffset.UtcNow;
        }

        // 서명 검증이나 서버 권한 판단이 아니라, 신규 진입 의도를 같은 계정에만 연결하는 표식이다.
        [Serializable, UnityEngine.Scripting.Preserve]
        private sealed class AccountIdentity
        {
            [UnityEngine.Scripting.Preserve] public AccountIdentity() { }
            public long memberId;
        }
    }
}
