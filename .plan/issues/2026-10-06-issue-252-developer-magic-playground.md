# 2026-10-06 — 개발자 마법 연습장

- Date: 2026-10-06
- GitHub Issue: #252 (client), ArcaneCastersGame#88, ArcaneCastersLobby#55
- Status: Implemented; draft PRs for review

## Goal
An editor-only client operates its own server-authoritative playground, casting registered magics for either side and controlling damage immunity/clearing units. Both players have 99,999,999 HP; the normal world and systems are reused, bots/results/rewards are absent, and the server removes the session after 300 seconds.

## Non-goals
No database migration, account creation, normal match recovery/status changes, version bump, release deployment, or player-build playground content.

## Context / Constraints
Worktrees branch from origin/dev; existing dirty checkouts remain untouched. Existing debug casting mutates the world outside the loop, so commands must be queued. The two simulated players are not two authenticated users. Only the authenticated administrator owning the session can subscribe/control it. Feature endpoints require an opt-in server setting, default false. Client host assembly is constrained by UNITY_EDITOR, game-specific bridge code lives in Editor, and exclusive assets live under Assets/DevPlayground, outside Resources, StreamingAssets and Addressables.

## Approach (Checklist)
- [x] **Step 0: Recon** Inspect session factories, result/teardown paths, STOMP security, existing GameScene and UI prefabs.
- [x] **Step 1: Implementation** Game: specialized loop/session and development APIs; reuse normal systems and magic parser, drain commands on loop thread. Clear summons/buildings silently, preserving players/boundaries and preventing pending/death spawns. Faction immunity covers present/future units, blocks damage rather than lifetime/consumption. Lobby: opt-in administrator creation route, select only servers that accept playground requests, return ready URLs and expiresAt without normal recovery/ticket/user mutations. Client: AdminScene EditorOnly button, automatic preparation and generated editor-only scene derived from existing GameScene; prefab-backed panels/buttons/dropdown with dynamic repeated icon grid; reuse renderer and transport, set target by field click (initial center), icon click casts immediately. Dedicated client transport avoids normal result/recovery flows.
- [x] **Step 2: Tests** Server unit/integration tests cover TTL, HP, commands, ownership, feature off, no stats/MMR/status mutations, normal matches unchanged. Client editor validation checks compilation, scene/UI wiring and player scene/asset exclusion. Run module test suites and report environment limitations honestly.
- [x] **Step 3: Rollout / Rollback** Draft PRs to dev, linked across repositories; setting enables only development servers. Parent gitlinks remain unchanged until PRs merge into dev. Revert PRs or disable playground setting to roll back.

## Validation
- **Commands to run:** game/lobby gradlew test; Unity editor import/compile and focused validation; scoped git diff --check; graphify update . if available.
- **Expected output:** Independent sessions, 300-second expiry, immediate server-side casts and synchronized clearing, faction damage immunity, no normal match side effects or client playground content in ordinary builds.

## Risks & Rollback
- **Risks:** Queued births/death callbacks can recreate cleared objects; result/failure/watchdog cleanup can write stats or user status; STOMP subscriptions must enforce ownership; introducing a custom asmdef cannot reference Assembly-CSharp, so use a small define-constrained host assembly and an Editor bridge that can reference Assembly-CSharp. Ordinary player builds must fail on playground references rather than silently leak assets.
- **Rollback steps:** Disable development setting; revert component commits in dependency order client/lobby/game.

## Open Questions
- None blocking. Left/right selection defines ally/enemy at the instant a command is submitted; immunity is a faction toggle until changed, not reassigned on dropdown change. Test target is selected independently of icons.

## Contract and review decisions
- Creation: client POST /api/dev/playgrounds on lobby with authenticated administrator JWT. Lobby sends POST /api/server/playgrounds {ownerId} using its service credential; only that trusted route accepts ownerId. Game returns {sessionId, server, webSocketUrl, ownerId, expiresAt}. HTTP control/snapshot routes validate administrator plus session owner; STOMP SUBSCRIBE validates owner and live playground, including the spectator destination.
- Game controls: POST /api/dev/playgrounds/{sessionId}/cast {magicId, master: LeftPlayer|RightPlayer, position:{x,y,z}}; /clear {master: LeftPlayer|RightPlayer|None} (None=both); /immunity {master, enabled}. Capture immutable request values, queue on the loop, return completion {success,message,leftImmune,rightImmune}; reject saturated/expired sessions and report spell exceptions without killing the loop.
- Snapshot: GET /api/dev/playgrounds/{sessionId}/snapshot; subscription /game/{sessionId}/frameInfos/{ownerId}, existing frame/sync DTOs. End notification {type:playgroundEnded,reason:EXPIRED|CLOSED|FAILED}. Explicit DELETE session route closes early; client disconnects/unsubscribes, clears match context, and returns to AdminScene or stops Play Mode.
- TTL starts at loop initialization just before readiness, uses injected Clock/monotonic equivalent and does not depend on subscriber, heartbeat, reconnect or frame count. expiresAt is authoritative; stale requests fail. Normal GameTimer duration is overridden to 300 seconds so fever and timer systems remain coherent.
- Clearing: remove every faction-owned non-player object (units, buildings, projectiles, spell areas and pending creates) for the requested side, leaving neutral world/environment objects intact. Silent removal skips combat death, destruction and status callbacks, stops sources, and removes queued creations for that side; other-side pending creates survive. This prevents leftover attacks from immediately repopulating the cleared side.
- Build guard: reject DevPlayground scenes in build scene list, reject dependencies from ordinary build scenes/Resources/Addressables entries into DevPlayground, and inspect packed assets after build; guard both dev/release entrypoints. Generated scene stays out of build settings and Addressables. A UNITY_EDITOR-constrained host assembly is absent in players; Editor folder hosts the bridge/generator/guards. A real build proved that wrapping a script in Assembly-CSharp leaves its MonoScript packed, and placing the behaviour in Editor prevents scene attachment. Distinguish real build verification from static checks if licensing blocks Unity.
- Review: fast NONPASS fixed by explicit contracts, injectable TTL, clear policy and exclusion checks; medium PASS guardrails accepted (inherit system pipeline, reuse native transport/handlers, generate scene from current GameScene).

## Results
- Game: 793 tests passed, no failures or skips; a real prefab/physics/frame pipeline fixture covers players, casts, snapshots and clearing.
- Lobby: 266 tests, no failures; 21 existing database-dependent tests skipped.
- Unity 2022.3.34f1: scene reload/UI wiring, intentional scene and Resources leaks rejected, actual Play Mode bootstrap passed. Both BuildDevWebGL and BuildWebGL completed successfully with packed-asset checks; no playground player assembly or types.
- UI image is an Editor layout render with fixture icons, not a live authenticated session. Live administrator login and game/lobby integration were not exercised.
- Game draft PR #89 and lobby draft PR #56; client draft PR accompanies this plan. No merge, deployment, activation, version bump or parent gitlink change.
- Graphify graph and executable are absent in the root checkout.

## Follow-up: Admin entry
- Replace menu entry with a prefab-backed Magic Playground button in AdminScene. Prepare the ignored scene automatically before Play Mode; return to AdminScene on close/expiry.
- Keep the entry EditorOnly and bind its listener exclusively from Editor. Scene-build processing must distinguish player builds from Editor Play Mode (Unity passes a null BuildReport in Play Mode).
- Actual Editor Play Mode click/scene/return flow passed with HTTP suppressed; build-scene stripping and keeping the entry in Play Mode passed. Update client PR #253 with the new admin layout render.
- Final Admin entry state: both BuildDevWebGL and BuildWebGL succeeded after the entry change, including build stripping and packed-asset checks.

## Follow-up: fullscreen field and targeting (2026-10-06)
- Keep GameScene camera transform, projection and aspect fitter; use its full viewport. Existing prefab-backed side panels overlay the field. Tab toggles both panels so every ground position is reachable; status/help stays visible.
- Clicking a magic icon arms that magic without sending HTTP. Move the pointer over the field to show the ordinary ground aim circle, then left-click to submit its exact X/Z position with Y=0 and the current caster dropdown. Keep selection after a cast for repeated testing; Escape/right-click cancels it. No range, mana or deck restrictions are added to this developer tool.
- Cache the disabled FieldSelector raycast utility. Reject points outside X[0,18]/Z[0,10] rather than clamping a missed ground hit; reject UI clicks and UI pointer capture. Hide the preview on UI/outside the field, disconnect, cancellation and teardown. Do not enable normal card/network input or add magic metadata requests merely for aiming.
- Snapshot magic id/side/position at submission. Block overlapping casts until the HTTP coroutine completes; clear state on exit. Selection should have a visible icon tint and text; panels can be shown again with Tab.
- Validate generated camera/UI wiring and real Play Mode input decisions with a local no-HTTP command sink: selecting sends nothing, valid field click carries id/side/XZ, UI/out-of-bounds clicks send nothing, Right side and cancellation work, panel visibility does not remove world rendering. Render the new layout and aim marker. Re-run normal/development packed-asset build guards, then sync only changed playground files into the user's active client.
- Review: fast/medium/heavy PASS. Incorporated camera pixel bounds, captured UI press/drag-release checks, failed-command pending recovery and dropdown-aware Tab handling. The aim circle uses the ordinary .18 radius/style; status text does not raycast. No new transport abstraction or magic metadata request.
- Results: Unity Play Mode targeting fixture passed all input/payload/cancellation checks. Fullscreen generated-scene wiring and intentional build-scene/Resources leaks passed. BuildDevWebGL and BuildWebGL both exited successfully with packed-asset guards. UI evidence is a fixture layout render, not authenticated server gameplay. Changed playground sources synced into the active root/client project; restart Play Mode to regenerate its scene.
