# 2026-10-07 — Shared attack sounds and removal of legacy muting

- Date: 2026-10-07
- GitHub Issue: https://github.com/Apptive-Game-Team/ArcaneCastersClient/issues/256
- Status: Implemented; ready for pull request

## Goal

Use the five approved WAVs with one attack sound per event. Towers use TowerAttack; water/fire projectile attackers use WaterLaunch/FireLaunch; other attackers use SmallAttack or SwingAttack according to the database size tags. Replace blanket legacy muting with removal of obsolete prefab sound owners.

## Non-goals

No new sound generation, layering, server contracts, voice-pool replacement, version bump, or changes to movement/HP/death sound policy.

## Context / Constraints

The existing lifecycle profiles and GameSfxPlayer already provide shared volume, voice limits and scene cleanup. Attack grouping crosses lifecycle element/archetype grouping. Add an optional attackProfile to each catalog entry, whose Attack overrides only that slot. A missing override falls back to the lifecycle Attack; an explicitly disabled override stays silent. IntentionalSilent suppresses both. Attach must support attack-only entries; avoid duplicate controllers.

Runtime mappings use authoritative attack implementations and database size tags, never sprite/radius heuristics. Towers take priority over their launch element. SeaSerpent uses large SwingAttack because its beam is not a projectile launch. CloudDragon uses WaterLaunch for its regular shot; the attack notification does not distinguish its secondary lightning attack. Nature/lightning/rock/wind projectile attackers use size fallback until approved elemental clips exist. Non-attacking producers/supports have no attack override.

Unit-produced FireShot/WaterShot/ElectricShot/LeafShot/MagmaFist and newer unit projectiles remain silent on spawn to prevent doubled launch sounds. Keep the existing TransientShot release for standalone ChainLightning/TideCall/WindBlade. Keep centralized explosion spawn audio. New transient objects receive explicit catalog rows. Four nest aliases and ElectricAbsorb get explicit alias rows. Tutorial mock units have no actual attack loop; attach their central controller, and route the mock projectile cast through the approved FireLaunch slot once.

## Approach (Checklist)

- [x] **Step 0: Recon** — Read client instructions and audio, asset, preview and planning skills/docs. Identify real attack implementations, size tags, legacy components, nested HitSoundPlayer instances and variant overrides. Isolate feature/256 from unrelated changes.
- [x] **Step 1: Implementation** — Import five mono 44.1 kHz processed copies, preserving originals and recording provenance. Add five shared attack-only profile assets and one entry-level EffectiveAttack resolver. Update controller, catalog, builder and validator. Builder keeps explicit mapping in one table, preserves existing customized attack assignments and aliases while setting defaults for new rows. Remove LegacySfxMuter/OnAttackSoundPlayer and obsolete game-prefab AudioSources plus all dependent YAML documents, component/child links and variant overrides. Preserve BGM and preview-specific suppression.
- [x] **Step 2: Tests** — Validate catalog coverage, one effective slot, fallback/disabled/silent semantics, regeneration preservation, attack-only profile validity, absence of prefab AudioSources and missing references, source/processed audio properties, C# compilation and focused Unity tests where available. Manual acceptance: tower, small melee, large melee, water/fire shooter attacks each dispatch once; projectile spawn adds no second release; independent spell releases and explosion playback remain; previews stay silent; tutorial casts use central playback.
- [ ] **Step 3: Rollout / Rollback** — Commit/push client branch, open labeled assigned PR against feature/252, attach PR. Do not update root pointer before client merge. Roll back with one feature commit revert.

## Validation

- **Commands to run:** ObjectSfxCatalogValidator.ValidateFromMenu, focused ObjectSfx tests, Roslyn or generated-project compile against local Unity assemblies, asset/GUID/fileID scan, audio_probe.py, git diff --check, graphify update . from monorepo root.
- **Expected output:** no missing catalog rows, duplicate attack owners, invalid enabled clips, stale references or changed-source compile errors; five mono PCM16 44.1 kHz clips near -3 dBFS; honest report if Editor or Graphify is unavailable.

## Risks & Rollback

- **Risks:** Hand-edited prefab inheritance can retain stale overrides; inspect all assets referencing removed GUID/fileIDs, not only base prefabs. Mono conversion changes amplitude; normalize approved runtime copies and use conservative shared attack volume. Attack notifications expose no secondary attack identity, so use each type's regular attack policy. Unit projectile sounds move to authoritative attacker events; independent spell profiles stay separate.
- **Rollback steps:** Revert feature commit, restoring assets, catalog and old owners together. User WAV originals remain untouched.

## Open Questions

None blocking. CloudDragon secondary attacks need a future server event discriminator for distinct element audio.

## Review feedback

Fast NONPASS findings addressed: effective-slot validation, explicit fallback/non-attacker policy, standalone versus unit projectile split, tutorial routing and full audio measurements. Medium PASS recommendations accepted: reuse ObjectSfxProfile, one EffectiveAttack resolver, validate silent rows against both references, attack-only slots, localized cleanup. No rejected feedback.

## Verification results

Heavy review: PASS. Unity 2022.3.34f1 imported the worktree and compiled the runtime/editor/test assemblies. All 31 ObjectSfxTests passed, including a real Play Mode entry/exit for controller destruction. Catalog validation and regeneration passed for 128 rows; 45 types select approved attack overrides. The 19 changed prefabs have no newly dangling local fileID references. Five runtime WAVs probe as mono PCM16 44.1 kHz at -3 dBFS; source files were not modified. `git diff --check` passed. Graphify update was attempted from the monorepo root but its CLI is not installed. Final in-game listening/mix review was not performed in headless tests.
