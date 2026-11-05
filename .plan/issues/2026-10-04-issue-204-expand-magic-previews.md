# 2026-10-04 — 다른 마법 미리보기 확장

- Date: 2026-10-04
- GitHub Issue: #204 (game #68)
- Status: Implemented — Unity verification pending (MCP unavailable)

## Goal
Extend the reviewed server-recorded pipeline with twelve more magics:
water_shot, lightning_shot, wind_blade, rock_rolling, fire_drop, wind_drop,
water_explosion, wind_explosion, rock_blast, mini_rock_swarm,
thunder_bird_swarm, water_slime_swarm. This is the next batch, not all-magics completion.

## Non-goals
No production mechanics, DB balance, login/network, new art, or UI layout changes.
Do not infer mechanics from appearance or replace server simulation with client logic.

## Context / Constraints
Reuse client feature/204 PR209 and server feature/68 PR69. Preserve unrelated dirty files.
Current pipeline contains nine magics/fourteen scenarios. UI uses ground MiniRock,
air ThunderBird enemy fixtures, hidden captions, 1.35x book zoom, one-second DOT flashes.

## Approach (Checklist)
- [x] **Step 0: Recon** Inspect real spell and initializer code and document ground/air attacks and secondary effects for each selected magic.
- [ ] **Step 1: Implementation** Extend MagicScenarioPreviewTest with explicit per-mechanic fixtures and real initializers; assert damage/status/projectiles/conversion for each scenario. Record every supported target category for summons. Include WaterSlime trail/Wet, ThunderBird dive/death-energy/Overcharge, LightningShot enemy Shock/friendly Overcharge, WindBlade piercing decay, RollingRock bounces, drop/explosion effects, and both RockBlast remnant sizes. Export deterministic recordings and add shared prefab visuals from existing runtime sprites.

Swarm fixtures use quantity 3. Immediately after the real spell runs and before
lifecycle/capture, stage only newly pending swarm objects at fixed positions
within the production ±1 spawn range, preserving spawn height. Attack, movement,
targeting and effects remain production mechanics. Recording source/docs identify
the illustrative deterministic formation. Unexpected unsupported secondary
effects require explicit coverage or a documented limitation, never silent omission.
- [ ] **Step 2: Tests** Capture twice identically, run server suite and Unity coverage/cycle tests with dynamic expected registrations. Inspect new recordings in Unity with actual art; verify no unsupported visual mappings and maintain enemy target distinction and DOT cadence.
- [ ] **Step 3: Rollout / Rollback** Update coverage documentation and both draft PRs with tests and screenshots. Keep special-building magics as explicitly unimplemented follow-up; remove new registrations/assets to disable only this batch.

## Validation

Completed: 21 recordings / 30 scenarios exported, twice identical; full server
suite 664 passed; static visual coverage and all recording GUIDs validated.
Pending: Unity Play Mode test rerun and new-batch screenshots. MCP HTTP server
is unavailable and no Editor process/batch executable was found in standard paths.
Graphify refresh was attempted; the CLI is not installed.
- **Commands to run:** Gradle MagicScenarioPreviewTest with previewExport; full server tests; Unity MagicPreviewTests; scoped git diff --check; graphify update at root (CLI currently unavailable).
- **Expected output:** 21 registered magics; each new scenario proves its declared mechanic, deterministic repeated capture, no missing sprite/effect/projectile, existing regressions pass.

## Risks & Rollback
- **Risks:** Missing fixture parameters, swarm spawn layout/time-to-contact, view cropping, unexpected effect combinations and target category assumptions. Inspect production code and add evidence-based scenarios; never omit a failing attack category silently.
- **Rollback steps:** Remove only newly added clips/mappings/exporter cases; retain existing nine recordings and user-requested presentation changes.

## Open Questions
- Remaining magic families are not included in this batch and must be listed as pending rather than claimed complete.

## Review
- Fast: accepted explicit swarm formation and secondary-effect coverage findings.
- Medium: accepted swarm fixture clarification; keep existing explicit exporter.
- Rejected feedback: none.
- Heavy: PASS; validate observed WaterExplosion Burn/launch and plain Drop damage without inventing effects.
