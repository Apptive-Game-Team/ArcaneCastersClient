# 2026-10-19 — 재질별 유닛·건물 피격음 연결

- Date: 2026-10-19
- GitHub Issue: #348
- Status: Verified; ready for pull request

## Goal

Import the user's three approved Stable Audio WAVs and play at most one eligible
target-material sound per live HitEvent, subject to cooldown and shared voice cap.

## Non-goals

No generated replacements, attack/explosion changes, new prefab AudioSources,
server protocol changes, version bumps, or automatic merging.

## Context / Constraints

Work in isolated feature/348 from origin/dev. The original checkout has unrelated
user changes. Existing lifecycle groups describe elements, not physical material:
MagmaSpirit and CraterCarrier have stone bodies, while PveVineWitch is a creature.
Reuse ObjectSfxProfile and add a hit-only override alongside attackProfile.
Preview presentation stays silent. The user explicitly selected these sources;
process copies only (mono PCM16 44.1 kHz, trim silence, 15 ms tail fade, -3 dBFS).
Start all three Hit slots at volume 0.55 and pitch 1 (quieter than attacks at 0.7).

### Material mapping

One builder HitGroups table is the source for these explicit defaults. An absent
hitProfile falls back to lifecycle Hit, a disabled override stays disabled and
intentional silence suppresses both. Rebuilding preserves non-null hit overrides.

- WoodHit: BeanShooter, EvilEnt, PveEvilEnt, TreeGolem, VineSpirit,
  PveCorruptedVineSpirit, GiantVine, LifeTree, PveNatureSlimeNest, PveVineColony,
  SeedNest, Vine, VineColony, ElectricSummon, FireSummon, RockSummon, WindSummon,
  FireRune, FrenzyTotem, HealingTotem, ManaWell, RallyingTotem, GrassGenerator,
  RepairTotem, ShockTrap.
- RockHit: CraterCarrier, MagmaSpirit, DimensionToad, PveDimensionToad, RockGolem,
  RockMage, RockSlime, MiniRock, WallGolem, PveBrickGolem, Crater, FireworkTower,
  GroundCannon, GroundTower, RockTurret, Towerback, DragonTower, TitanRemnant,
  BubbleGenerator, ElectricTower, LightningRune, NatureRune, RockRune, WaterRune,
  WindRune, WindTotem.
- CreatureHit: ArcaneInfantry, ChickenCommando, Player, EmberSpirit,
  FireChildSpirit, FireLordSpirit, FireSlime, FireSpirit, FireTadpole,
  PveFireTadpole, AquaArcher, BubbleSpirit, SeaSerpent, WaterSlime, LeafSlime,
  PveVineWitch, SeedSpirit, WillOWisp, ElectricSlime, LightningTadpole,
  PveLightningTadpole, StormRider, StormStag, ThunderBird, ThunderSpirit, ZapMouse,
  CloudDragon, PveCorruptedWindSpirit, WindSlime, WindSpirit, BombSprite.
- Excluded: ice structure PveWaterSlimeNest; all projectile/transient profiles,
  ElectricAbsorb and LightningDrop aliases; HP bars, RiverWater, RiverBridge,
  RiverOverlay. No unknown-type runtime fallback.

These defaults use the inspected material of the visible body: stone armor/shells
use RockHit; plant bodies use WoodHit except gelatinous LeafSlime and seed-bodied
SeedSpirit. The visual FireRune disc is wood, WindTotem is stone, and the four
summoning aliases resolve to the woody SeedNest compatibility body.

## Approach (Checklist)

- [x] **Step 0: Recon** Inspect current dev, material sprites, sound ownership,
  frame order and server HitEvent emission; create assigned/labeled issue.
- [x] **Step 1: Implementation** Add CreatureHit/WoodHit/RockHit profiles and
  hitProfile/EffectiveHit catalog resolution. Classify wood plants/structures,
  stone bodies/structures and remaining creatures explicitly in builder; preserve
  overrides on regeneration. Leave projectiles, fields, scenery and the ice nest
  outside the three supported target materials. Route live HitEvent to the target
  controller before visual-effect early returns; remove HP-decrease subscription.
  Apply a fixed 0.2-second per-target cooldown and existing shared HitHeal voice cap.
- [x] **Step 2: Tests** Check material exceptions and complete mapping against the
  intended list, hit fallback/disabled/silence semantics, regeneration preservation,
  actual clip-specific live HitEvent dispatch (including missing attacker/visual
  and charged StormStag), HP-only silence, preview silence and cooldown behavior.
  Run existing sound regression tests and catalog validation when Unity is available;
  otherwise compile against locally available assemblies and state runtime limits.
- [ ] **Step 3: Rollout / Rollback** Commit only task files, push feature/348,
  open assigned/labeled PR against dev and attach it. Do not merge or update root
  submodule pointer before merge. Revert the feature commit for rollback.

## Validation

- **Commands to run:** audio_probe.py, focused Unity ObjectSfxTests /
  ExplosionSfxTests / MaterialHitSfxTests, ObjectSfxCatalogValidator, local compile
  and serialized GUID/coverage checks, git diff --check, graphify update . at root.
- **Expected output:** three valid mono clips; correct target hit material without
  duplicate HP/destruction playback; previews silent; existing attacks/explosions
  preserved; builder regeneration retains material overrides.

## Risks & Rollback

- **Risks:** Element classification can misclassify material; inspect sprites and
  document explicit exceptions. Hit events precede object updates, so dispatch must
  happen before visual early returns and cannot depend on an attacker still existing.
The Hub registry has no Editor entry, but existing Bee response files locate
  C:/Tools/2022.3.34f1/Editor/Unity.exe. Use that exact Editor for batch validation;
  do not claim listening from automated checks.
- **Rollback steps:** Revert the feature commit; original Downloads files untouched.

## Open Questions

None blocking. Ice-only structures stay silent until a material sound is selected.

## Review feedback

Fast NONPASS: accepted explicit complete mapping, eligible-event wording and initial
volume/pitch policy. Medium PASS: accepted direct controller dispatch and exact
override/fallback semantics; no additional runtime material inference. Heavy PASS:
revised scope, explicit mapping and actual-dispatch validation are implementable.

## Verification results

Unity 2022.3.34f1 imported and compiled the project, generated the catalog and
validated 140 rows. All 81 focused tests passed: MaterialHitSfxTests 33,
ObjectSfxTests 31, ExplosionSfxTests 12 and GameEventTests 5. This includes real
Play Mode entry/exit and clip-specific active-voice assertions. Initial package
import hit a temporary EBUSY lock; retry succeeded. The first compile caught an
obsolete test API; it now checks defaultSampleSettings.preloadAudioData.

Static checks confirm all 82 intended material mappings and all original
lifecycle/attack/silence/alias assignments, plus unchanged Downloads originals.
Audio probes confirm three mono PCM16 44.1 kHz clips at -3 dBFS with decayed tails.
git diff --check passes after removing unrelated Editor-generated changes.
Graphify update was attempted from the monorepo root; its CLI is not installed.
In-game listening and WebGL playback have not been performed.
