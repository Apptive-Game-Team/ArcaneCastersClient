# 2026-10-21 — 인게임 메뉴와 항복

- Date: 2026-10-21
- GitHub Issue: None
- Status: Implemented; Play Mode visual verification pending

## Goal
Top-left hamburger menu with Continue, Settings and confirmed Surrender. Reuse settings and confirmation prefabs. In-game settings expose only three sound sliders.

## Non-goals
No deployment, version bump, account controls in gameplay, pause, or team voting.

## Context / Constraints
Existing disconnect activates a bot, not surrender. Add an authenticated surrender input to game server, queued onto game loop; preserve terminal results and normal rating/reward/result flow. Current PvE is one human on the left.

## Approach (Checklist)
- [x] **Step 0: Recon** Inspect settings, lobby menu, input queue and result checkers.
- [x] **Step 1: Implementation** Prefab-backed menu in GameScene; active controller handles Esc, balanced input capture, settings and confirmation. Gameplay settings allow only sound rows. Send surrender once and allow explicit retry after timeout; never manufacture a result locally. Server sets submitting player's loss if not already ended.
- [x] **Step 2: Tests** InputControllerTest: 8 tests, 0 failures/errors. Final Assembly-CSharp build: 0 errors. Prefab/scene local anchors and hierarchy validated; every MonoBehaviour includes script metadata. Play Mode visual verification remains manual.
- [x] **Step 3: Rollout / Rollback** Local only. Client requires matching server contract.

## Validation
- **Commands to run:** scoped Gradle tests, Unity compile/import if available, asset reference checks, git diff --check, graphify update.
- **Expected output:** no compile or reference errors; confirmed surrender follows normal result handling.
- **Results:** Gradle focused tests passed. C# compile passed with existing warnings. Running Editor reimported GameMenu.prefab without the initial missing-script warnings after explicit script metadata was added. Separate batch import was blocked by the existing Editor instance. Graphify executable is unavailable, so graph refresh could not run.

## Risks & Rollback
- **Risks:** modal input leaking; inactive controller cannot receive Esc; queue can drop requests. Guard keyboard/casting, keep controller active, show timeout and allow retry.
- **Rollback steps:** revert only this feature's files/hunks; preserve existing worktree edits.

## Open Questions
- None.

## Review
Fast: preserve terminal result, set loser rather than setEnd for PvE, timeout/retry required.
Medium: keep controller active; gate keyboard and cast entry points; balance pointer capture.
Accepted all findings. No speculative shared UI framework.
