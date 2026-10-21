# 2026-10-21 — 데스크톱 전체 빌드 업데이트

- Date: 2026-10-21
- GitHub Issue: None (local-only work)
- Status: Implemented locally

## Goal
Windows, macOS and Linux players can download an entire newer build from public
GitHub Releases, then quit, install and restart.

## Non-goals
Delta patches, publishing releases, purchasing certificates, mandatory updates,
private-repository credentials, and rollback after arbitrary game crashes.

## Context / Constraints
Keep existing dirty work intact. The current build entrypoints target WebGL.
Use the existing confirmation prefab layout for a separate update dialog.
No update URL means disabled; network failures must not prevent login.
HTTPS plus SHA256 protects transport/integrity, not independently signed releases.

## Approach (Checklist)
- [x] **Step 0: Recon** — inspected login, loading, confirmation prefab and build script.
- [x] **Step 1: Implementation** — opt-in config in StreamingAssets; stable numeric
  X.Y.Z manifest per platform; download to disk with cancellation; external
  self-contained .NET 8 helper staged outside the installation; safe ZIP extraction,
  parent-exit handshake, dedicated marked directory swap, retained backup and restart.
- [x] **Step 2: Tests** — malformed archives/paths/hash/markers, rollback and handshake,
  packaging contract and compilation; document unavailable native platforms.
- [x] **Step 3: Rollout / Rollback** — local packaging creates per-platform ZIP/JSON;
  operator later uploads to a public GitHub release. Previous install retained.

## Validation
- **Commands to run:** helper self-tests; packaging tests; Unity compile/import if
  installed; scoped diff check; graphify update from root.
- **Expected output:** no install mutation before verified staging and parent exit;
  failures preserve or restore the old installation; WebGL/editor skip updates.

## Risks & Rollback
- **Risks:** writable install parent required; Windows file locks; macOS signing and
  internal symlinks; native platforms need actual smoke tests. Backup is retained
  for manual recovery, not automatically deleted on process-start success.
- **Rollback steps:** restore the sibling backup after closing the game, or omit
  update config in future builds.

## Open Questions
- Public repository: Apptive-Game-Team/ArcaneCastersClient. The release workflow
  embeds github.repository and uses the latest stable per-platform manifest.

## Plan review
- Fast: initially NONPASS; incorporated safe internal symlinks/modes, marked root,
  exclusive lock, bounded handshake, strict archive contract and rollback boundary.
- Medium: PASS; one controller/helper, no generic updater framework, no-config is normal.
- Heavy: PASS after revision. Handshake cancellation must prevent late installation.
- User then selected GitHub Releases as an acceptable source. Host choice does not
  change the installation protocol.

## Verification results
- Unity 2022.3.34f1 imported and compiled the project. Actual login prefab
  references validated; Windows player scripts compiled via PlayerBuildInterface.
- Linux/macOS standalone preprocessor branches compiled with Unity's player
  references and Roslyn compiler.
- Windows helper: 20 tests including actual subprocess commit/abort/no-commit,
  rollback, corrupt ZIP and locked files.
- Linux helper in WSL: 21 tests including actual subprocess restart, executable
  permissions and macOS-style framework symlink chains.
- Packaging: 7 tests passed (including archive equivalence and macOS bundle layout).
- Native macOS installation/restart and a released game's end-to-end HTTPS/UI
  upgrade remain release acceptance checks, not claimed verified.
- Graphify update attempted; graph is absent and graphify is not installed.

## Release integration follow-up
- User authorized commit and PR, not a deployment or release execution.
- Four platform builds use DesktopBuildScript and ArcaneCasters output names.
  Product identity/version remain unchanged; exact release tag is checked out.
- Native macOS packaging ad-hoc signs after preparation and verifies extracted ZIP.
  The marker lives under Contents/Resources to keep a valid signed bundle root.
- Shared packaging writes wrapped first-install and unwrapped update ZIPs.
  A fan-in job uploads all ZIPs before JSON and never clobbers desktop assets.
- Native Windows/Linux/macOS helper and packaging checks added for PRs.
- Medium PASS and heavy PASS; preserve product identity, verify executable and
  macOS signature, keep published payloads immutable, verify remote base.
- Separate issue creation was rejected by automatic approval review, so omitted.
