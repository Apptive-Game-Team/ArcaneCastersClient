#nullable enable
using System;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Global
{
    /// <summary>Login needs a successful version check with no required upgrade.</summary>
    public sealed class DesktopUpdatePolicy
    {
        private bool checkedVersion;
        private Version? requiredTarget;
        public bool Required => requiredTarget != null;
        public bool CanContinue => checkedVersion && !Required;

        public void BeginCheck() => checkedVersion = false;

        public void Observe(Version current, Version latest)
        {
            if (requiredTarget != null && latest < requiredTarget)
                throw new InvalidOperationException("Previously required release is unavailable");
            if (latest > current && (latest.Major != current.Major || latest.Minor != current.Minor))
                requiredTarget = latest;
            checkedVersion = true;
        }

        // Published release events start builds; do not advertise an unfinished release.
        public static (string Url, string Version) FindReadyRelease(DesktopRelease[] releases, string platform)
        {
            string[] platforms = { "win-x64", "linux-x64", "osx-x64", "osx-arm64" };
            if (!platforms.Contains(platform)) throw new ArgumentException("Unsupported platform");
            foreach (var release in releases.Where(r => r != null && !r.draft && !r.prerelease
                         && Regex.IsMatch(r.tag_name ?? "", @"^v?(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$")
                         && DateTimeOffset.TryParse(r.published_at, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                         .OrderByDescending(r => DateTimeOffset.Parse(r.published_at, CultureInfo.InvariantCulture)))
            {
                var assets = (release.assets ?? Array.Empty<DesktopReleaseAsset>())
                    .Where(a => a != null && a.state == "uploaded").ToArray();
                string version = release.tag_name.TrimStart('v');
                string prefix = "ArcaneCasters-" + version;
                if (!platforms.All(rid => assets.Any(a => a.name == prefix + "-" + rid + ".zip")
                    && assets.Any(a => a.name == prefix + "-" + rid + "-update.zip")
                    && assets.Any(a => a.name == "desktop-update-" + rid + ".json"))) continue;
                return (assets.First(a => a.name == "desktop-update-" + platform + ".json").browser_download_url, version);
            }
            throw new InvalidOperationException("No complete desktop release available");
        }
    }

    [Serializable]
    public sealed class DesktopRelease
    {
        public bool draft, prerelease;
        public string published_at = "";
        public string tag_name = "";
        public DesktopReleaseAsset[] assets = Array.Empty<DesktopReleaseAsset>();
    }

    [Serializable]
    public sealed class DesktopReleaseAsset
    {
        public string name = "";
        public string browser_download_url = "";
        public string state = "";
    }
}
