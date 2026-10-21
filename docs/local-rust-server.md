# Desktop Rust local server

The desktop local-hosting client runs the native bundle from
[ArcaneCastersGameRust #9](https://github.com/Apptive-Game-Team/ArcaneCastersGameRust/pull/9).
It starts `ac-game.exe` on Windows and `ac-game` on macOS/Linux with
`--spring.profiles.active=local`. The working directory is the extracted bundle
root. No JVM arguments or JRE are used. The existing stdin/stdout, HTTP, STOMP,
static data and result-report contracts are unchanged.

The archive is `local-server-{platform}.zip`, with a `.zip.sha256` sidecar.
Inside it are `local-server/VERSION` and `local-server/ac-game[.exe]`.
Platforms remain `windows-x64`, `macos-x64`, `macos-arm64`, and `linux-x64`.
Flat extraction is also accepted. macOS/Linux installation restores executable
permission on `ac-game` after extraction.

Rust bundles live in `Application.persistentDataPath/local-server/bundles-rust/`.
Existing Java bundles in `bundles/` are not selected or overwritten: Java and
Rust release versions are independent. Static data, settings and pending results
continue to use their existing paths. Users install the Rust bundle from the
local-server settings page.

## Lobby configuration before enabling installation

The client uses the URLs in `GET /api/offline-pack`; it does not rewrite them.
The lobby's public binary distribution URL is configured through
`local-server.asset-url-template` (Spring environment variable
`LOCAL_SERVER_ASSET_URL_TEMPLATE`) to:

```text
https://raw.githubusercontent.com/Apptive-Game-Team/ArcaneCastersPlay/local-server-bundles/v{version}/local-server-{platform}.zip
```

Set `LOCAL_SERVER_VERSION` and `LOCAL_SERVER_MINIMUM_VERSION` to published Rust
bundle versions, not Java versions. Publish all four platform archives and their
checksums before advertising that version. An empty version advertises no bundle.
Rust release CI builds native bundles, then publishes only ZIPs and checksums to
the public `ArcaneCastersPlay` distribution branch. The Rust source repository
is private: its release URLs return 404 to anonymous clients and must not be
advertised by the lobby. Verify downloads without GitHub credentials.

Installation metadata refreshes when entering the lobby. After a lobby rollout,
re-enter the lobby or restart Play Mode; reopening settings alone does not refresh it.

Until a compatible bundle is installed and ready, practice and adventures keep
using the existing shared-server fallback. A Java archive offered by a stale
lobby is rejected as missing the native Rust executable.

## Testing in Unity Editor

Windows, macOS and Linux Editors are supported by `LocalServerPlatform`, even
when the selected build target is WebGL. Open this PR's project with Unity
2022.3.34f1; another checkout already open in the Editor will not use these files.

1. Use a development lobby with `/api/offline-pack` and `/api/local-sessions`
   enabled and its minimum server version configured for the Rust bundle.
2. Install the Rust bundle from the local-server settings page. Before release
   assets are published, extract a locally built `local-server-{platform}.zip`
   into `Application.persistentDataPath/local-server/bundles-rust/<version>/`,
   using the version from the bundle's `VERSION` file. Keep its `local-server/`
   subdirectory. On macOS/Linux, give `ac-game` executable permission manually
   when bypassing the installer.
3. Enter Play Mode from the login scene and log in. The lobby supplies matching
   static data; enable local hosting in settings and wait for the Console entry
   `[LocalServer] ready on 127.0.0.1:...`.
4. Start practice or an adventure. Confirm `[LocalServer] local ... session ...`
   in the Console. A `[LocalServer] shared server for ...` entry instead means
   a prerequisite failed and the match fell back to the shared server.

This is online local hosting: login, session issuance and result reporting still
use the lobby. A manually launched server on a fixed port is not used; the
client starts its own child process and passes its token over stdin.

## Local verification (2026-10-21)

- Client PR #315 baseline: `1b3bc37e5eaad4e2c5b1ff54be84fdde3fa3e5d8`.
- Rust PR #9 baseline: `183259ec4d2355408e0480f46ad0245025468d5c`.
- 45 NUnit tests passed: existing local-hosting/tick-rate rules plus native bundle
  discovery, wrong-platform, incomplete-bundle and legacy Java bundle rejection.
- The PR's local-hosting C# sources compiled against the installed Unity DLLs
  with zero errors (one existing Newtonsoft/netstandard reference warning).
- Built `ac-game` on Windows and packaged it with the Rust PR's distribution
  script. A C# process using this client's `LocalServerBundle` paths and arguments
  extracted the archive under a path containing spaces and Korean characters,
  received `ready`, checked `/healthcheck`, sent `shutdown`, and observed exit 0.
- Rust `local_launcher_process` (4 tests) and `local_session` (7 tests) passed,
  including completed Practice/PVE matches, result reporting and token checks.
- Rust v0.1.0 release CI built all four platforms and smoke-tested Windows,
  Linux and macOS ARM. Public distribution CI downloaded all four bundles
  anonymously and verified their checksums. The published Windows bundle also
  passed a local checksum and start/stop smoke test.
- Fixed settings label formatting by passing arguments directly to Unity
  Localization, which formats placeholders during lookup. C# compilation passed.
- Unity Editor gameplay and macOS Intel execution were not tested. The lobby
  deployment is verified separately from the client. Graphify refresh was
  unavailable because its executable was not on the Windows command path.
