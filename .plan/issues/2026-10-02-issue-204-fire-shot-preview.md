# 2026-10-02 — 파이어볼 서버 기록 미리보기

- Date: 2026-10-02
- GitHub Issue: #204; game #68
- Status: Implemented; automated and isolated Unity validation passed; full scene/WebGL manual QA pending

## Goal
Show one looping fire_shot example in owned-card/deck-slot hover and the selected magic book detail.

## Non-goals
No live server sessions, CDN, database changes, balance edits, or previews for other magics. Do not change ownership gates.

## Context / Constraints
Current client dev and game feature/64 contain unrelated dirty files; preserve them. Fireball is fire_shot / FireShot. Runtime prefab behaviours depend on game singletons. Use sprite-only visual proxies, not gameplay prefab instantiation; this is a simplified presentation of actual recorded simulation, not full renderer fidelity.

## Approach (Checklist)
- [x] **Step 0: Recon** Inspect FireShotMagic, Shot, physics, ObjectsInfoDtoBuilder, deck hover and book layouts.
- [ ] **Step 1: Implementation** A game test runs real FireShotMagic/initializer and simulation systems against stationary damageable fixtures. Export schema version 1, timestep, fixture parameters, real create/update DTOs and explicit impact position only on damage-confirmed collision. Export is opt-in, not a normal test side effect. Bundle JSON in client Resources. Shared prefab RawImage and preview component use local ID map, dedicated camera/RenderTexture, sprite proxies, interpolation, hit flash and impact sprite. No global game state, audio or network. Clear on switch/loop; dispose on disable. Wire serialized prefab references in both scenes. Clamp enlarged hover to canvas; ignore raycasts.
- [ ] **Step 2: Tests** Assert creation, motion, destruction, splash to adjacent enemy and no damage to distant enemy. Validate bundled JSON, compile C#, validate scene/prefab references. Attempt Editor visual validation; distinguish unavailable visual check from compilation.
- [ ] **Step 3: Rollout / Rollback** Feature only activates for fire_shot; other magics retain current UI. Publish scoped feature PRs against their proper base; no merging.

## Validation
- **Commands to run:** game focused export test and full test; client dotnet compile with new files included, Unity visual check if available, YAML links, git diff --check, root graphify update.
- **Expected output:** faithful recorded trajectory and splash frame, looping correctly in both hosts, no rendering resources remaining when hidden.

## Risks & Rollback
- **Risks:** clipped hover, stale async hover result, prefab lifecycle dependencies, deterministic fixture differs from live balance, missing animation in proxies.
- **Rollback steps:** revert scoped commits or remove serialized preview prefab references.

## Open Questions
None. Fixed fixture values are an illustrative scenario, documented with provenance; no claim of current live balance.

## Review
Fast NONPASS resolved: explicit damage-confirmed impact; define limited visual fidelity; versioned export and fixture checks; immediate stop on switching and non-raycast UI. Medium PASS with cleanup and opt-in export recommendations accepted. Unowned book selection remains existing product behaviour rather than broadening ownership access.
