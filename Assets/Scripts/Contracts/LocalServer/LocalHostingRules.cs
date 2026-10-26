using System;

namespace LocalServer
{
    /// <summary>로컬 경기를 시도하기 전에 확인하는 값. 하나라도 맞지 않으면 공유 서버로 경기한다.</summary>
    public struct LocalHostingConditions
    {
        public bool PlatformSupported;
        public bool SettingEnabled;

        /// <summary>lobby <c>GET /api/offline-pack</c> 의 <c>localSessionsEnabled</c>. 응답을 못 받았으면 false.</summary>
        public bool LocalSessionsEnabled;

        public string InstalledVersion;
        public string MinimumVersion;
        public bool ServerReady;

        /// <summary>로컬 서버가 <c>ready</c> 에서 알린 정적 데이터 version.</summary>
        public string ServerStaticVersion;

        /// <summary>lobby 가 지금 요구하는 정적 데이터 version.</summary>
        public string LobbyStaticVersion;
    }

    /// <summary>로컬 경기를 시도하지 않는 이유. 진단 log 용이다.</summary>
    public enum LocalHostingBlock
    {
        None,
        PlatformNotSupported,
        SettingDisabled,
        LocalSessionsDisabled,
        NotInstalled,
        BelowMinimumVersion,
        ServerNotReady,
        StaticDataOutdated
    }

    /// <summary>
    /// 로컬 경기 조건(계약 문서 <c>2026-10-12-local-hosting-contracts.md</c> 5번)과 version 비교.
    /// Unity 에 의존하지 않아 EditMode 테스트가 직접 부른다.
    /// </summary>
    public static class LocalHostingRules
    {
        public const int SupportedSchemaVersion = 1;

        public static LocalHostingBlock Evaluate(LocalHostingConditions conditions)
        {
            if (!conditions.PlatformSupported) return LocalHostingBlock.PlatformNotSupported;
            if (!conditions.SettingEnabled) return LocalHostingBlock.SettingDisabled;
            if (!conditions.LocalSessionsEnabled) return LocalHostingBlock.LocalSessionsDisabled;
            if (string.IsNullOrEmpty(conditions.InstalledVersion)) return LocalHostingBlock.NotInstalled;
            if (!string.IsNullOrEmpty(conditions.MinimumVersion) &&
                CompareVersions(conditions.InstalledVersion, conditions.MinimumVersion) < 0)
                return LocalHostingBlock.BelowMinimumVersion;
            if (!conditions.ServerReady) return LocalHostingBlock.ServerNotReady;

            // 로컬 서버는 기동할 때 정적 데이터를 메모리에 한 번 적재한다. lobby 가 다른 version 을 요구하면
            // 밸런스가 공유 서버와 달라지므로 이번 경기는 공유 서버에서 한다.
            if (!string.IsNullOrEmpty(conditions.LobbyStaticVersion) &&
                !string.Equals(conditions.ServerStaticVersion, conditions.LobbyStaticVersion, StringComparison.OrdinalIgnoreCase))
                return LocalHostingBlock.StaticDataOutdated;

            return LocalHostingBlock.None;
        }

        /// <summary>
        /// <c>1.3.0</c> 꼴 version 을 숫자 단위로 비교한다. 빠진 자리는 0 이고, 숫자가 아닌 꼬리(<c>-rc1</c>)는 무시한다.
        /// </summary>
        public static int CompareVersions(string left, string right)
        {
            int[] a = ParseVersion(left);
            int[] b = ParseVersion(right);
            int length = Math.Max(a.Length, b.Length);
            for (int i = 0; i < length; i++)
            {
                int x = i < a.Length ? a[i] : 0;
                int y = i < b.Length ? b[i] : 0;
                if (x != y) return x < y ? -1 : 1;
            }

            return 0;
        }

        private static int[] ParseVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return Array.Empty<int>();

            string trimmed = version.Trim().TrimStart('v', 'V');
            int suffix = trimmed.IndexOfAny(new[] { '-', '+', ' ' });
            if (suffix >= 0) trimmed = trimmed.Substring(0, suffix);

            string[] parts = trimmed.Split('.');
            var numbers = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                int.TryParse(parts[i], out numbers[i]);
            }

            return numbers;
        }

        /// <summary>
        /// 32 byte 이상 무작위 값을 소문자 hex 로 바꾼다. 무작위 byte 는 호출부가 CSPRNG 로 만든다.
        /// </summary>
        public static string ToHex(byte[] bytes)
        {
            var chars = new char[bytes.Length * 2];
            const string digits = "0123456789abcdef";
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i * 2] = digits[bytes[i] >> 4];
                chars[i * 2 + 1] = digits[bytes[i] & 0xF];
            }

            return new string(chars);
        }

        /// <summary>
        /// <c>.sha256</c> 파일 내용에서 hex 값을 꺼낸다. <c>sha256sum</c> 형식(<c>hex  파일이름</c>)도 받는다.
        /// </summary>
        public static string ParseSha256File(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;

            string first = content.Trim().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
            if (first.Length != 64) return null;

            foreach (char c in first)
            {
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!hex) return null;
            }

            return first.ToLowerInvariant();
        }

        /// <summary>보고 실패로 남긴 결과를 아직 다시 보낼 수 있는지. lobby 의 세션 기록 TTL(2시간)을 넘으면 버린다.</summary>
        public static bool IsPendingResultFresh(DateTime savedAtUtc, DateTime nowUtc)
        {
            return nowUtc - savedAtUtc < TimeSpan.FromHours(2);
        }

        /// <summary>
        /// 결과 보고 응답 코드에 따라 남긴 결과를 어떻게 할지. 404(세션 없음)와 409(이미 보고됨)는 다시 보내도
        /// 받아 주지 않으므로 버린다. 그 외 4xx 도 같은 body 로는 달라지지 않는다.
        /// </summary>
        public static PendingResultAction DecideAfterReport(bool reachedServer, long responseCode)
        {
            if (!reachedServer) return PendingResultAction.Keep;
            if (responseCode >= 200 && responseCode < 300) return PendingResultAction.Drop;
            if (responseCode == 401 || responseCode == 403 || responseCode == 408 || responseCode == 429)
                return PendingResultAction.Keep;
            if (responseCode >= 500) return PendingResultAction.Keep;
            return PendingResultAction.Drop;
        }
    }

    public enum PendingResultAction
    {
        /// <summary>보고가 끝났거나 다시 보내도 소용없다. 파일을 지운다.</summary>
        Drop,

        /// <summary>다음 로비 진입 때 다시 보낸다.</summary>
        Keep
    }
}
