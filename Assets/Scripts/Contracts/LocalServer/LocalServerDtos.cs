using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LocalServer
{
    /// <summary>
    /// lobby <c>GET /api/offline-pack</c> 응답. 계약 문서 <c>2026-10-12-local-hosting-contracts.md</c> 2번.
    /// </summary>
    public sealed class OfflinePackResponse
    {
        public int schemaVersion;
        public string staticVersion;

        /// <summary>
        /// 정적 데이터(계약 1번 형식) 전체. 요청의 <c>staticVersion</c> 이 같으면 null 이다. client 는 내용을
        /// 읽지 않고 파일로 저장해 로컬 서버에 넘기므로 형태를 고정하지 않는다.
        /// </summary>
        public JObject staticData;

        public bool localSessionsEnabled;

        /// <summary>lobby 설정에 서버 version 이 없으면 null.</summary>
        public LocalServerRelease localServer;
    }

    public sealed class LocalServerRelease
    {
        public string version;
        public string minimumVersion;

        /// <summary>platform(<c>windows-x64</c>, <c>macos-x64</c>, <c>macos-arm64</c>, <c>linux-x64</c>) 별 zip 주소.</summary>
        public Dictionary<string, string> assets;

        public string AssetUrl(string platform)
        {
            if (assets == null || string.IsNullOrEmpty(platform)) return null;
            return assets.TryGetValue(platform, out string url) ? url : null;
        }
    }

    /// <summary>lobby <c>POST /api/local-sessions</c> 요청.</summary>
    public sealed class LocalSessionRequest
    {
        public const string Practice = "PRACTICE";
        public const string Pve = "PVE";

        public string type;
        public long? scenarioId;
    }

    /// <summary>로컬 서버 <c>POST /api/local/sessions</c> 응답.</summary>
    public sealed class LocalServerSessionResponse
    {
        public string sessionId;
        public string webSocketUrl;
    }

    /// <summary>로컬 서버 stdin 첫 줄.</summary>
    public sealed class LocalServerInitCommand
    {
        public string type = "init";
        public string token;
        public string dataDir;
        public string staticDataPath;
    }

    /// <summary>
    /// 로컬 서버 stdout 한 줄. <c>type</c> 에 따라 쓰는 필드가 다르다. <c>matchResult</c> 는 본문을 그대로 lobby 에
    /// 넘겨야 하므로 이 type 으로 읽지 않고 <see cref="JObject"/> 로 다룬다.
    /// </summary>
    public sealed class LocalServerMessage
    {
        public const string Ready = "ready";
        public const string Error = "error";
        public const string MatchStats = "matchStats";
        public const string MatchResult = "matchResult";

        public string type;

        // ready
        public int port;
        public string serverVersion;
        public int schemaVersion;
        public string staticVersion;

        // error
        public string reason;

        // matchStats
        public string sessionId;
        public int targetTickRate;
        public double meanFps;
        public double p10Fps;
    }

    /// <summary>
    /// lobby 에 보고하지 못한 결과. <c>pending-results/&lt;sessionId&gt;.json</c> 에 저장하고 다음 로비 진입 때 다시 보낸다.
    /// </summary>
    public sealed class PendingLocalResult
    {
        public string sessionId;

        /// <summary>저장한 시각(UTC, ISO-8601). 2시간이 지나면 lobby 세션 기록이 사라지므로 버린다.</summary>
        public string savedAtUtc;

        /// <summary>lobby 에 보낼 body. <c>sessionId</c> 를 뺀 <c>matchResult</c> 그대로다.</summary>
        public JObject body;
    }

    /// <summary>정적 데이터 파일 앞머리에서 읽는 두 값.</summary>
    public struct StaticDataHeader
    {
        public int SchemaVersion;
        public string StaticVersion;

        [JsonIgnore]
        public bool IsUsable => SchemaVersion == LocalHostingRules.SupportedSchemaVersion && !string.IsNullOrEmpty(StaticVersion);
    }
}
