# 2026-10-03 — 기능별 대표 마법과 순차 미리보기

- Date: 2026-10-03
- GitHub Issue: #204 (client), game #68
- Status: Implemented; server suite and Unity Play Mode tests passed; MCP deck/book fixture verification complete

## Goal

Deck hover and book explanation automatically cycle through all registered situations, without selection buttons. Implement representative recordings for rock_golem (ground), wind_spirit (air), towerback (ground then air), healing_totem (support), fire_shot (projectile and Burn), lightning_explosion (area and Shock), sand_storm (persistent area), frenzy_totem (rage), earth_call (small then medium remnant conversion).

## Non-goals

No client gameplay simulation, database migration, live-balance changes, new magic/art, or claim that every magic is covered. Keep existing issue branches and unrelated changes.

## Context / Constraints

Use production server magic/prefab/components in an offline deterministic test fixture. Parameters are explicit illustrative fixtures, not live balance. Capture real objects, projections, effects, gauges and hit events. Each scenario starts with a clean battlefield and records assertions for the demonstrated mechanic. Keep legacy version-1 Fire Shot readable. Share visual mappings between scenarios of one magic. Reuse the existing UI prefab, no selection controls. Destroy all temporary visuals on switch/disable.

## Approach (Checklist)
- [x] **Step 0: Recon** Production mechanics inspected; fast/medium feedback incorporated and heavy review passed.
- [x] **Step 1: Implementation** Version-2 exporter, nine clips/fourteen situations, shared sprite mappings, attacks/projectiles/HP/effects and caption implemented. EarthCall includes the resulting creature's attack; WindSpirit uses explosion art on self-destruct.
- [x] **Step 2: Tests** Server 664 tests passed; exporter captures twice identically. Two Unity Play Mode tests passed for mappings, cycles, stage reuse, cleanup/reconfigure, legacy and malformed data. MCP verified deck Towerback air situation and book EarthCall medium conversion in the explanation scroll. Direct scene entry uses synthetic magic data because it lacks login; authenticated owned-card pointer flow and WebGL build remain manual validation.
- [x] **Step 3: Rollout / Rollback** Existing documentation and draft PR chain updated with representative scope and real captures. Unsupported spells retain existing UI. Remove only new registrations to roll back content; retain legacy playback. Graphify graph/CLI unavailable, so graph refresh could not run.

## Validation
- Version 2: root `version`, `magic`, `frameDuration`, `source`, ordered `scenarios`; each scenario has `id`, `labelKo`, `labelEn`, `duration`, `frames`. Frames contain time, production objects create/update/projectile, events, and explicit initial gauge snapshots (create DTO lacks HP). Projection reference IDs and event IDs use the same normalized IDs. Version 1 root duration/frames normalize to one scenario on load.
- Configure/reopen starts at scenario zero; advance at duration and wrap to zero; reuse camera/ground, clear all dynamic visuals. Reject invalid recordings and hide the preview output rather than retain stale content.
- Matrix: rock_golem damages and pushes a ground target, then a death scenario leaves rubble; wind_spirit attacks an aerial target and removes itself; towerback ground attack versus air RockShot/splash; healing_totem heals an injured ally; fire_shot applies Burn/ongoing HP loss and a separate Wet interaction; lightning_explosion hits multiple nearby targets and applies Shock; sand_storm persists and inflicts ongoing damage; frenzy_totem changes ownership/attack interval then restores them, compared to baseline; earth_call consumes small/medium remnants and produces MiniRock/RockGolem. Assertions must test the mechanic rather than only nonempty frames.
- Both hosts must complete at least one ordered loop. Verify disabled cleanup, unsupported content, legacy and same-magic reconfiguration. Only the representative slice is complete; unknown effects/projectiles must fail coverage tests rather than silently disappear.
- **Commands to run:** Gradle focused preview tests/export and full tests; Unity MCP compile, console, playback assertions and captures; git diff --check; graphify update . if available.
- **Expected output:** Nine registered magics play meaningful server-derived scenarios; towerback transitions ground → air → ground; effects and HP are visible; no stale objects or repeated stage leaks; unsupported spells unchanged.

## Risks & Rollback
- **Risks:** Missing projection/effect mappings, creation/start failures hidden by production catch/log, initial gauges absent from create DTOs, airborne framing, delayed attacks, deaths and spawned fields/remnants, stale illustrative data.
- **Rollback steps:** Revert scoped preview changes or remove affected clip registration; never revert unrelated work.

## Open Questions
- None blocking. This is a representative slice, not exhaustive spell coverage. Conditional elemental interactions outside the chosen scenarios remain future coverage.

## Review notes

Fast review found the incorrect frenzy ID and underspecified schema; corrected above. Source inspection found FireDrop does not create a persistent field, so SandStorm represents that feature instead. Medium review passed; accepted one v1/v2 playback path, small shared capture fixture, server-derived effects and camera reuse. No generic scenario DSL or gameplay replication.
