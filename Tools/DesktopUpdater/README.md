# Desktop full-build updates

The login scene checks an HTTPS manifest before server checks or login,
downloads the entire ZIP to disk, validates SHA256, and starts an external helper.
The helper stages and validates the complete installation before telling Unity
to quit, then swaps directories and starts the new game. Downloads can be
cancelled. Only patch updates can be skipped. Newer major/minor updates require
**Update / Exit**; cancellation or any installation failure returns to **Retry /
Exit**, never login. Failed/invalid version checks also require retry or exit.
There is no version-check Skip button. An exit veto keeps the login gate closed.

The existing `release.yml` workflow now builds and packages all four desktop
platforms from the requested release tag. It uploads every ZIP before publishing
update manifests. No credentials are embedded in the player. This PR does not
create a release or run deployment.

## Build and prepare

Prerequisites: Unity 2022.3.34f1 with the target build module, .NET 8 SDK, Python
3.10+. Players do **not** need .NET installed: publish the helper self-contained.
The target OS must still satisfy .NET 8's native runtime prerequisites.
Supported runtime IDs: `win-x64`, `linux-x64`, `osx-x64`, `osx-arm64`.
Build separate macOS architecture packages; do not use a universal game with a
single-architecture helper.

From the `client` directory, for example on Windows:

```powershell
# Always use fresh, empty output directories.
& 'C:/Program Files/Unity/Hub/Editor/2022.3.34f1/Editor/Unity.exe' `
  -batchmode -quit -projectPath "$PWD" -executeMethod DesktopBuildScript.Build `
  -desktopPlatform win-x64 -desktopBuildPath "$PWD/build/windows/ArcaneCasters" `
  -logFile "$PWD/build-desktop.log"

./Tools/DesktopUpdater/publish_updater.ps1 `
  -Platform win-x64 -Output "$PWD/build/helper-win-x64"

python Tools/DesktopUpdater/package_release.py prepare `
  --install-root build/windows/ArcaneCasters --helper build/helper-win-x64 `
  --repository OWNER/PUBLIC-RELEASE-REPOSITORY

python Tools/DesktopUpdater/package_release.py pack `
  --install-root build/windows/ArcaneCasters --output build/release-windows `
  --repository OWNER/PUBLIC-RELEASE-REPOSITORY --tag v1.2.3
```

Replace the repository and release tag with real values. The version comes from
Unity's `PlayerSettings.bundleVersion`, recorded by `DesktopBuildScript`; do not
maintain a second version. Only stable `X.Y.Z` versions are accepted. The release tag must be `X.Y.Z` or
`vX.Y.Z` matching that bundle version.

On Linux/macOS the equivalent helper publish is:

```sh
dotnet publish Tools/DesktopUpdater/DesktopUpdater.csproj -c Release \
  -r osx-arm64 --self-contained true -o build/helper-osx-arm64
printf %s osx-arm64 > build/helper-osx-arm64/updater-platform.txt
```

For macOS use a build path ending in **ArcaneCasters.app**. Run `prepare` before
signing the app and its embedded helper, then sign/notarize/staple as appropriate,
then run `pack` without modifying the bundle afterwards. Preserve relative
framework symlinks and executable modes; package macOS on macOS. Windows builds
may also be signed after preparation. The release workflow runs free ad-hoc signing on a native macOS runner after
preparation, then verifies the bundle before and after ZIP extraction. It does
not provide Apple Developer ID signing or notarization. Windows remains unsigned.

## GitHub Releases

Use a **public release repository**, which can be separate from the source repo.
Private repositories need authentication and are not supported. For each release,
upload each platform's three generated files:

- `ArcaneCasters-<version>-<platform>.zip` — first installation, wrapped folder
- `ArcaneCasters-<version>-<platform>-update.zip` — replacement payload
- `desktop-update-<platform>.json`

Upload all supported platforms before marking a release as the latest stable
release. The embedded configuration includes a public GitHub release-list URL and a
legacy fallback manifest URL:

```text
https://github.com/OWNER/REPO/releases/latest/download/desktop-update-win-x64.json
```

The client selects the newest published stable release among the 20 most recent
releases with all 12 expected assets fully uploaded, then checks its immutable
platform manifest. Unfinished newer releases are ignored. The manifest version
must match the selected release tag. Each manifest points to the immutable tagged
ZIP URL, not the `latest` ZIP URL.
GitHub's HTTPS asset redirects are supported. Do not replace an existing release
ZIP without regenerating its manifest. These downloads use transport security
and checksums; they are **not independently signed-update verification**.

The update ZIP contains the *contents* of the dedicated installation directory.
The first-install ZIP supplies the `ArcaneCasters` folder (`ArcaneCasters.app` on
macOS); keep that folder name. The ownership marker is `arcane-install.json` at
the install root, or `Contents/Resources/arcane-install.json` inside macOS bundles
so the bundle root remains valid for code signing. The installation and its parent
must be writable. The directory name is enforced so extracting into Downloads
cannot accidentally make the updater replace that whole folder. No administrator
elevation is attempted.

Builds created with the old WebGL build methods are unchanged. Desktop builds
without `desktop-update.json` skip checking entirely. Older desktop releases
without this feature must be replaced manually once.

## Installation and recovery

The builder preserves productName, companyName and application identifiers; only
output executable/folder names change. User preferences and auth storage remain in Unity's normal persistent-data
location. The updater does not copy them into the installation.

The helper is copied outside the installation to
`Application.persistentDataPath/DesktopUpdates/<session>/helper`. That session
contains the downloaded ZIP, `job.json`, handshake files and `result.txt`.
The client does not quit unless staging succeeds. If the helper cannot start or
prepare the installation, optional patch updates offer Continue; required updates
remain blocked with Retry / Exit. A helper timeout/abort
cannot install later without the client's explicit commit and exit.

Installation replaces the **whole dedicated directory**, so obsolete game files
are removed. Do not store personal files or saves inside it. The old directory is
retained as `<installation>.backup-<id>`. Rollback covers directory-swap and
process-start failures; it cannot detect a game that starts then crashes.

If a machine shuts down between directory renames, close the game and helper,
and restore the `.backup-*` directory to the original installation name.
Never delete the only backup until the new game has been checked. Backup folders
are retained intentionally; old `DesktopUpdates` session folders can be removed
after closing the game/helper. Allow space for the archive plus an extracted
new installation in addition to the installed game.

If another game instance holds files open, updating may fail safely; close other
instances and retry. macOS Gatekeeper and Windows SmartScreen still apply.

## Validation

```powershell
dotnet run --project Tools/DesktopUpdater/DesktopUpdater.Tests.csproj
python -m unittest discover -s Tools/DesktopUpdater -p 'test_*.py' -v
```

Tests use temporary installations, never the real game. In restricted Windows
environments set `ARCANE_TEST_ROOT` to a writable workspace temporary directory.
Set `ARCANE_UPDATER_EXECUTABLE` to a published helper to also run actual
parent-process exit/commit/abort and restart integration tests.
The restart fixture uses `/usr/bin/true` on macOS and `/bin/true` on Linux;
do not assume the Linux path exists on macOS (both native Mac CI jobs caught this).
Native release acceptance must additionally exercise a real `1.x.y -> 1.x.z`
upgrade on each OS: Update, Later, cancel, lost network, read-only install,
bad checksum, locked files, and actual application restart. Test the unsigned
and/or signed distribution route players will actually use.

The desktop publish job deliberately refuses to overwrite existing assets. A
partial upload requires operator inspection before retrying; never replace an
already-advertised payload. An incomplete newest release falls back to a previous ready release. If no ready
release is found or requests fail, show Retry / Exit. GitHub availability and its
unauthenticated API rate limits are therefore part of desktop login availability;
clients sharing one public IP share that rate limit. Existing v1.5.0 builds require one
manual installation of a release containing this feature.

## Required release versioning

Compare stable numeric versions, not strings: `1.5.0 -> 1.5.3` is optional,
`1.5.x -> 1.6.0` and `1.x -> 2.0.0` are mandatory. Equal or older releases do
not trigger an update. Once a required target is observed, a failed retry or an
older manifest cannot unlock that running client. No separate minimum version is
managed. This is a client login gate, not server-side rejection of modified clients.

Any fix that every player must install must be released as MINOR, even if its
Conventional Commit type is `fix:`. Include this trailer in that commit:

```text
fix: repair a protocol incompatibility

Required-Update: true
```

The monorepo's `.agents/skills/deploy/scripts/plan-versions.sh` recognizes this
trailer for the client only. `!` / `BREAKING CHANGE` still selects MAJOR; `feat:`
still selects MINOR; ordinary fixes select PATCH. Preserve the trailer when
squashing. Version bumps still happen once at promotion, never in a feature PR.
The preserved deploy script expects independent component checkouts and does not
run promotions from this Windows monorepo layout.
