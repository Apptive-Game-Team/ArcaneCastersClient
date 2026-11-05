# 2026-10-03 — 마법 미리보기 공통 구조와 풀바닥

- Date: 2026-10-03
- GitHub Issue: #204
- Status: Implemented; Unity MCP deck/book Play Mode validation passed with synthetic spell data

## Goal

Use the existing forest battlefield ground in the deck/book preview, and turn the Fire Shot-only player into a reusable, data-driven replay path. Fire Shot remains the first verified clip.

## Non-goals

Do not simulate gameplay on the client, download previews at hover time, migrate the database, change live spell balance, or claim every spell is already covered. New spells need their own recorded clips and visual mappings.

## Context / Constraints

The game server owns mechanics. The existing `fire_shot.json` is a server-generated 20 Hz fixture; the client only replays create/update/damage/impact presentation. Both deck hover and book use one UI prefab. The client checkout has unrelated dirty font and `.meta` files; leave them untouched. The forest battle theme and `ground.mat` already use `Assets/Art/Images/Background/background.png` as grass ground. Preserve current PR/branch chain.

## Approach (Checklist)

- [x] **Step 0: Recon** Confirmed forest `ground.mat` and its grass texture; Unity's built-in Quad faces local -Z and needs +90° X rotation to face the preview camera.
- [x] **Step 1: Implementation** Render a small XZ grass field behind the fixture without mutating the shared ground material. Separate clip selection (`magic.serverName`) and object-type sprite mapping from the replay engine. Keep the versioned bundled recording and a safe no-preview fallback. Use one prefab in both hosts; no per-hover network call.
- [x] **Step 2: Tests** Unity MCP imported the renamed prefab/component with one clip and no compile errors. Deck Play Mode showed grass, units and replay; book Play Mode showed it after the description when scrolled. Prefab supports `fire_shot` and rejects unregistered `water_shot`. Synthetic spell data was needed because direct scene entry lacks login and returns 401.
- [x] **Step 3: Rollout / Rollback** Keep unsupported spells on their present text/icon UI. Ship via the existing draft PR with actual Unity deck/book captures and document how a new magic adds a recording and visual mapping. Roll back by removing the preview prefab references or reverting scoped commits.

## Validation

- **Commands to run:** Unity import/compile and scene Play Mode through MCP, focused edit/play tests where available, `git diff --check`, game export test only if exporter changes, `graphify update .` if installed.
- **Expected output:** grass ground visible beneath units and projectile; Fire Shot replay visible in deck and book; no missing-reference or compile errors; unsupported spells unchanged.

## Risks & Rollback

- **Risks:** ground orientation/culling, RenderTexture draw order, data/visual schema coupling, preview lifecycle leaks, unsupported DTO types, stale fixture vs live balance.
- **Rollback steps:** revert the UI/player change and leave the current Fire Shot-only implementation; the recording JSON remains usable.

## Open Questions

- This phase uses the reusable foundation plus Fire Shot as its first verified clip. Additional spells need their own server fixture recordings and sprite mappings; choose the next spell explicitly.
