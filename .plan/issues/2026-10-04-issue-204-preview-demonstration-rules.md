# 2026-10-04 — 마법 미리보기 시전 수량·광역 피격·사망 후 장판 정리

- Date: 2026-10-04
- GitHub Issue: #204 (client), #68 (game exporter)
- Status: Complete — heavy review PASS

## Goal
Do not showcase death-created ground fields; show only one cast's summon quantity;
show area-attacking units hitting multiple clearly separated enemies.

## Non-goals
No production gameplay/balance/DB/art/UI layout changes. Keep shared native
presentation and sequential cases. Retain attack self-destruction, movement trails,
spell-created areas and useful secondary effects; remove generic death-field demos,
but keep distinct rock remnants and energy transfer (not ground fields).

## Context / Constraints
Game preview fixture forces every expanded quantity default to three; normal
AbstractSpawnMagic defaults to one. Use a single checked-in quantity snapshot from
database V001 parameter_values, including mini_rock=2, ember_spirit=5,
seed_spirit=4, thunder_bird=3, water_slime=3, vine_spirit=2, zap_mouse=2,
slime=10, overgrowth=2, grass_generator=6 and lightning_cloud=3; other owners=1.
This is a recording snapshot, not a claim about live DB values. Existing clips contain death/combat_death/
death_energy cases and death-field aftermath during ordinary attack cases.

## Approach (Checklist)
- [x] **Step 0: Recon** Read actual spell quantity defaults and supported attack
  masks/splash/beam/chain implementations; identify death field creation paths.
- [x] **Step 1: Implementation** Fix fixture quantity with the explicit shared
  snapshot and assert one-cast initial batch using ObjectSummoningMagic, separately
  from scenario allies, evolution materials and delayed offspring.
  Remove generic combat_death scenarios, retaining rock remnants/energy transfer.
  Test-only scoped static mocking of DeathField.spawn skips only this automatic
  ground-field creation at its source (ordinary combat can kill units, so trimming
  dedicated cases alone cannot cover it). No post-hoc DTO filtering/orphan IDs.
  Keep SeaSerpent WaterField, TreeGolem LeafField, Crater FireField and self-destruct
  attack evidence in tests. Update ThunderBird death-energy label/field assertion.
  Arrange multiple targets for every verified splash/beam/chain unit/mode in its
  normal attack case; assert hit events on two victims with the same attacker ID
  in one damage batch for splash/beam, or one chain projectile ID across frames.
  Use true attack range, spaced enemies and book/hover impact captures, not final
  HP alone or projectile sprite names as area-attack evidence.
  Regenerate bundled recordings; native renderer stays unchanged unless needed
  for suppressing unrequested prefab-only death-field presentation.
- [x] **Step 2: Tests** Full game test/export and determinism; assertions for cast
  quantity, no generic death demos/automatic death fields, multi-victim hits.
  Update Unity catalog counts and native captures; retain real absorber tests.
  Run PlayMode/EditMode via MCP.
- [x] **Step 3: Rollout / Rollback** Document explicit fixture rules and new counts,
  scoped commit/push/update existing PRs and root pointers; preserve unrelated edits.

## Validation
- **Commands to run:** game gradlew test -PpreviewExport=true; Unity MCP tests;
  inspect animated book/hover captures; git diff --check; graphify update.
- **Expected output:** 85 magics remain; no generic death-field showcases;
  initial cast counts match the explicit snapshot (not universally one/three);
  multiple victims hit by the same actual area attack.

## Risks & Rollback
- **Risks:** Removing useful trails/self-destruct attacks, reducing true swarm
  quantity, overlap obscuring enemies, fixture changes affecting determinism.
- **Rollback steps:** Revert only this fixture/data/test/doc change.

## Open Questions
- None for user. Validate which units/modes are actually area attacks from code.

## Review decisions
- Fast/medium quantity and scope findings accepted, including non-swarm quantity 2.
- Avoid DTO provenance/filter machinery: skip DeathField.spawn within fixture scope.
- Trimming alone rejected: ordinary damage/self-burn can invoke the same death-field
  path before a full attack cycle is demonstrated; production DeathField unchanged.
- Heavy second-pass review: PASS; no blockers. Renderer and production code untouched.

## Evidence
- Full game suite: 728 passed, zero failures; repeated per-magic capture deterministic.
- 85 regenerated clips / 129 cases (35 generic death-field demos removed),
  all 107 native object types retained. Real remnants/absorption still covered.
- Unity MCP first run: 12 PlayMode passed; 161 EditMode passed; console errors 0.
- Book/hover real-time impact captures inspected for MagmaSpirit, BubbleSpirit,
  SeaSerpent, DragonTower, BombSprite and FireworkTower. Capture coverage expanded
  to Tower, Towerback anti-air, WindSpirit, FireSpirit, ElectricTower and TitanRemnant.
- Graphify update attempted from root: CLI not installed; no graph.json available.
- Authenticated full deck/book pointer flow and WebGL build remain manual checks.
- Final expanded capture run: 12/12 PlayMode passed (55.9s); inspected the six
  additional impact layouts as well. 161/161 EditMode passed; console errors 0.
- PR review images: area-bubble-spirit-book.png and area-firework-tower-hover.png
  (actual Unity captures, under 400KB each); existing PR209/PR69 reused.
- Scoped publication: game feature/68 and client feature/204; root pointers only
  updated after module pushes. Unrelated fonts/settings/DiscordNotifier/DB retained.
