# 2026-10-04 — 나머지 마법 녹화 미리보기

- Date: 2026-10-04
- GitHub Issue: #204 (game #68)
- Status: Complete — implemented and verified

## Goal
Cover every currently implemented server magic missing from the 21-clip client
catalog (64 additional Spring component names, including existing PVE nests).
Automatically play supported ground/air attacks then secondary-effect situations.

## Non-goals
No production mechanics, database, balance, art generation, UI layout, audio or
login changes. No claim that all elemental permutations are shown. Never invent
an attack based on airborne art. Existing dirty fonts/alert/database stay untouched.

## Context / Constraints
Reuse feature/204 PR209 and feature/68 PR69. Read actual initializers/components;
record production mechanics at 20Hz with illustrative parameters/passive targets.
Reuse MagicPreview.prefab and existing runtime sprites. Unity MCP reconnected;
the user's open Editor runs the PlayMode tests and isolated viewport captures.

## Approach (Checklist)
- [x] **Step 0: Recon** Inventory all annotated server magic names and compare
  with bundled clips. Create a per-magic scenario matrix from production target
  masks, ranged/melee modes, spawned fields/units, death effects and support rules.
- [x] **Step 1: Implementation** Extend the existing test-only capture fixture
  with production prefab discovery/constructor wiring (Parameters/no-arg and
  VineFan dependency only; unknown dependency fails). Keep scenario choices and
  acceptance assertions explicit in a remaining-magic test/catalog, not inferred
  by visuals. Supported attack categories each get their own passive target.
  Support scenes use injured allies/buildings, mana scenes track mana, spawn
  buildings assert emitted units and their attacks; death scenarios show remnants,
  fields/transfers/explosions. Special shots show chains, control ownership,
  absorption/channel/beam, vine growth, pull/push and secondary fields. Stage new
  random spawns only before lifecycle/capture, preserving spawn height and create
  DTO coordinates; avoid simultaneous collision ties. Export twice identically.
  Register clips and exact existing sprite/projectile/effect references in the
  shared prefab, adding only renderer support needed by actual recorded payloads.
- [x] **Step 2: Tests** Assert catalog equality with all annotated magics, all
  declared scenarios/attacks/secondary signals, no missing fixture parameters or
  prefab dependencies, deterministic repeated capture, complete visual mapping
  and unique valid GUIDs. Run full server suite and Unity cycles/regressions when
  available; inspect real book and hover frames for complex spells when available.
- [x] **Step 3: Rollout / Rollback** Document per-magic coverage, illustrative
  staging and genuinely unsupported production/visual cases. Update draft PRs,
  publish submodule commits before root pointers. Remove only new registrations
  and recordings to disable this expansion.

## Validation
- Registered clips + explicit unfinished/unsupported set must equal the annotated
  catalog. An unfinished entry never counts as implemented.
- The matrix must list each attack mode, not only ground/air. Required exceptions:
  EvilEnt punch/GrabArm pull/FireFist Burn and heavy-target no-pull; CloudDragon
  WaterShot/chain/Wet aura; RockMage dual-target and concentrated double shot;
  SeaSerpent beam aligned multi-hit/off-axis miss and WaterField trail; TreeGolem
  self-heal/LeafField ally-heal; FireLord delayed five-second child spawn and child
  ground/air attacks; DimensionToad alternating tadpole attacks and panic; BombSprite
  ground-only bombing; StormStag movement tiers/Overcharge; Fire/Magma auras and
  all applicable Rock/electric death aftermath; Vine/GiantVine SeedSpirit evolution.
- RepairTotem freezes building lifetimes, not HP: compare an allied building and
  control, then remove aura and assert eventual expiry/unchanged HP. BubbleGenerator
  targets allies and grants Bubble. Rallying uses a real moving BehaviorMob and
  Inspired. LifeTree uses negative healing damage. ManaWell uses real ManaCharger,
  rate rise/restoration plus a recorded observable indicator. FireworkTower aims
  at its fixed forward offset and delays shell/damage until after projection.
- Crater/Meteor and other randomized components use test-only controlled random
  draws, retaining real start/update algorithms; staging positions cannot repair
  CraterEmber's already cached trajectory. Crater covers enemy collision versus
  neutral FireField landing. SpiritBomb asserts >50% ally drain, absorption, four
  ticks totaling rounded 70% and persistent beam/width rendering. WillOWisp shows
  ownership change followed by attacking the former side.
- **Commands to run:** Gradle focused exporter and full tests; static JSON/meta/
  prefab checks; Unity MagicPreviewTests; graphify update at root.
- **Expected output:** Every current server magic registered or explicitly
  evidenced as unrecordable; no silent omissions, no unsupported visual names;
  two equivalent recordings for every exported scenario; existing 21 clips pass.

## Risks & Rollback
- **Risks:** Random spawning, collision order, long buildup, support targeting,
  unsupported services, silent runtime errors, beam widths and off-screen events.
  Require actual mechanic assertions; do not substitute a spawn-only demo for
  an attack/support spell. Keep failed cases unregistered until solved.
- **Rollback steps:** Remove just this batch's clips/mappings and test cases.

## Open Questions
- None needed for implementation. Full authenticated deck/book scene flow was
  not exercised; actual shared renderer captures use the two viewport dimensions.

## Evidence
- 64 new clips; catalog total 85, 164 ordered scenarios. No remaining annotated
  magic is omitted. The Java test is the explicit per-magic scenario matrix.
- Server full suite: 728 passed / 0 failed; each new magic captured twice identically
  within the default 512MB test heap, without retaining all recordings at once.
- Unity PlayMode: 8 passed / 0 failed after fixing missing MeteorShower sprite and
  flattened positional projectile endpoints. Mid-beam assertion prevents the
  formerly invisible SeaSerpentHydroPump from passing on style coverage alone.
- Actual preview-camera book/hover captures inspected for SeaSerpent, EvilEnt,
  SpiritBomb and CloudDragon. Kept SeaSerpent book / EvilEnt hover evidence in PR
  media. These are isolated preview captures, not whole-scene screenshots.
- Four legacy nest bodies reuse SeedNest art; empty StormStag charge effects reuse
  increasingly sized Overcharge markers. Production gameplay and DB are unchanged.
- Graphify update attempted from the root; CLI is not installed. No graph was
  available for navigation. Authenticated UI flow / WebGL remain manual checks.

## Review
- Fast/medium NONPASS: accepted attack-mode matrix, support exceptions,
  constructor-cached RNG, persistent beams and explicit unfinished coverage.
- Rejected feedback: none. Heavy final gate: PASS.
