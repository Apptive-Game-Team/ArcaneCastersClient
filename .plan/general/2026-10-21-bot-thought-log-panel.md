# 2026-10-21 — 봇 생각 전용 로그 패널

- Date: 2026-10-21
- GitHub Issue: None
- Status: Implemented and locally verified

## Goal
Provide an admin-only expandable log in player, spectator and result scenes. Retain the latest 100 timestamped decisions, pause follow while reading, reset on a new match. Matchmaking notifications must expire with their originating scene.

## Non-goals
Server telemetry changes, permanent log export, deployment.

## Context / Constraints
Reuse existing button/panel prefabs. Keep auth failures hidden. Preserve unrelated local font changes. Ordinary session-loss notifications still cross scenes.

## Approach (Checklist)
- [x] **Step 0: Recon** Inspect notification queue, admin gate, three scenes and prefab patterns.
- [x] **Step 1: Implementation** Bounded plain log state, scene-local prefab UI, reset at connection creation and logout; scoped matching notifications including async completion.
- [x] **Step 2: Tests** Actual prefab tests for auth, retention, cap, reading snapshot, result/new-match and scene-scoped queue transitions. All four focused Unity tests passed; expanded/collapsed authored prefab previews rendered and inspected.
- [ ] **Step 3: Rollout / Rollback** Update the existing client PR after verification; revert its follow-up commits to roll back.

## Validation
- **Commands to run:** Unity 2022.3.34f1 EditMode regression tests; git diff --check; scene prefab reference checks.
- **Expected output:** Passing tests, prefab wired in GameScene/SpectatingScene/ResultScene, no matching toast after scene replacement.

## Risks & Rollback
- **Risks:** Entry eviction moving scroll position, stale localized callbacks, retained logs crossing accounts, pointer release casting a spell.
- **Mitigation:** Freeze the rendered snapshot while reading; bound each entry; token ownership and logout reset; owner-scoped callbacks; pointer capture through release frame.
- **Rollback steps:** Revert feature follow-up commits.

## Open Questions
None. Fast and medium reviews identified ownership/async/scroll issues; revised plan passed the heavy review.
