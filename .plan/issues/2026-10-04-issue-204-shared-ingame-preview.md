# 2026-10-04 — 미리보기와 실제 인게임 표시 코드 공유

- Date: 2026-10-04
- GitHub Issue: #204
- Status: Complete

## Goal
Replay existing recordings through the real runtime prefab, ServedObject,
projectile, effect, gauge, spawn/death and hit presentation paths. Replace the
parallel sprite-only preview renderer, preserving deck/book UI and sequential cases.

## Non-goals
No server simulation, gameplay/DB/balance/login changes, full GameScene loading,
global singleton/camera swapping, invented missing artwork or new UI layout.
Silent preview audio remains a policy, not a separate visual implementation.

## Context / Constraints
Work on existing client feature/204 / PR209. 85 recordings / 164 scenarios retained.
Runtime components depend on ObjectContainer.Instance and Camera.main; recursive
effects/death clones can escape the owning root. Existing user fonts/settings stay.

## Approach (Checklist)
- [x] **Step 0: Recon** Audit runtime spawn/update/hit/projectile paths and spawned
  effects, camera references, audio/input/player subscriptions and asset availability.
- [x] **Step 1: Implementation** Introduce an instance-owned presentation world
  for camera, origin transform, object lookup and transient ownership. Live calls
  retain default ObjectContainer/Camera.main behavior; preview calls supply their
  own world without changing singleton instances. Extract a single object-frame
  dispatcher (create → events → projectiles → update) from DeltaFrameHandler; keep
  HUD/cards/timer there. Both sources call it with actual runtime DTOs. Recording
  envelope/legacy conversion and fixture substitutions stay in the preview adapter.
  Extract shared spawn/projectile entry points used by that dispatcher. Bind actual ServedObject
  listeners, feed actual DTOs and share GameEventHandler hit dispatch. Update camera
  and lookup dependencies in projectile/visual components, parent spawned effects,
  afterimages, trails and death clones to the correct world. Cleanup suppresses
  cleanup-only death spawns and kills only that world's linked tweens.
  Instantiate preview prefabs under an inactive initialization parent, configure
  world/DTOs and disable Selectable, PlayerNameSetter, old audio components and
  AudioSources before activation; retain PlayerActionController and visual listeners.
  One scaled Unity clock at 1x drives frames/tweens/coroutines, with no arbitrary
  seek or global manual tween updates. Large hitch restarts the same scenario rather
  than replaying all overdue attacks simultaneously. Loop reset clears pending
  effects/coroutines without global KillAll.
  Replace MagicPreview's hand-built sprite logic with these entry points and remove
  obsolete style arrays/manual gauges/transients from script and prefab. Only
  fixture targets map ElectricSlime IDs to MiniRock/ThunderBird runtime prefabs;
  add explicit fixtureTargetIds to every v2 scenario in the test exporter. Initial
  ElectricSlime absorbers are not fixtures; test this distinction. Also preserve
  gizmos and export scenario-local parameter reads; AuraRadiusScaler prioritizes
  these in preview before activation and never reads/writes the live cache there.
  Original v1 envelopes remain readable; only that example's known initial passive
  ElectricSlime convention is used as its legacy adapter.
  Preserve one-second Burn/SandStorm flash policy via the shared ServedObject path.
  Use shared runtime SeedNest aliases for four legacy missing nest bodies, fix
  MeteorShower's stale sprite in its runtime prefab, and retain actual native
  StormStag tier components rather than invented marker sprites. Missing future
  prefab dependencies fail preflight with a visible unavailable state/warning.
- [x] **Step 2: Tests** Compile in Unity and exercise live default path plus two
  concurrent preview worlds with overlapping IDs. Assert actual prefab component
  types, beam/arm implementations, real effects/gauges, hit dispatch, destroy/reset
  ownership and unchanged singleton/main-camera references. Verify all recording
  prefab worlds with null match/login context. Add same-DTO live/preview equivalence
  and reset/disable stress with pending delayed destruction and death spawns.
  Verify all recording
  runtime asset dependencies. Inspect actual animated book/hover captures, not just
  generic sprite presence; retain malformed/version1/resource-reuse tests.
- [x] **Step 3: Rollout / Rollback** Document shared-path architecture and asset
  gaps, update existing PR with current captures, publish client before root pointer.
  Roll back scoped commit if needed; no production data migration.

## Validation
- **Commands to run:** Unity PlayMode/edit mode presentation regressions via MCP,
  static DTO/prefab reference audit, git diff --check, graphify update at root.
- **Expected output:** Runtime prefab/presentation components really execute;
  preview and live identities/cameras/effects never leak, no scene services or
  login/network required for preview; complete supported catalog cycles.

## Risks & Rollback
- **Risks:** Awake/Start ordering, DTO origin translations, late transient creation,
  audio/play-on-awake, player input/network subscriptions, global tween cleanup,
  source art gaps and recorded fixture/live parameter mismatch.
- **Rollback steps:** Revert only this shared-path change; retain recorded assets.

## Open Questions
- None for user; code audit and asset tests determine missing runtime support.

## Evidence / deviations
- Unity 2022.3.34f1 MCP: 10 shared-path PlayMode tests passed; 161 EditMode
  regressions passed. Actual real-time native captures inspected for SeaSerpent,
  EvilEnt, SpiritBomb and CloudDragon at both viewport dimensions.
- All 85 clips / 164 initial scenario worlds load native assets, and all 107 unique
  recorded object prefab executes Awake/Start without preview match dependencies.
  This is initialization coverage, not a timed full-length test of every scenario.
- Same DTO runs through both live and isolated registries with matching position,
  HP and native hit dispatch. Concurrent IDs/main camera and cleanup are isolated.
- Game full suite: 728 passed. Explicit fixture IDs and scenario parameter reads
  exported in test code only; all 85 bundled recordings regenerated.
- Native Inspired effect prefab is absent; retain actual state/motion and document
  the missing decal rather than invent preview-only art. Knockback uses motion.
  HUD mana counter remains out of scope; fabricated preview mana bar removed.
- Debug lines and placeholder player name were visible in the first native capture;
  suppress both while retaining gizmos/name component's live behavior.
- Graphify update attempted: CLI unavailable. Authenticated pointer flow and WebGL
  build remain manual checks. Unrelated fonts/settings/DiscordNotifier preserved.

## Review
- Fast/medium NONPASS accepted: explicit fixture identity, initialization/suppression,
  shared dispatcher/real DTO boundary, clock policy, local recorded parameters,
  asset gaps and cleanup/equivalence tests. Rejected suggestions: none.
- Heavy final gate: PASS. Proceed with the revised boundaries above.
