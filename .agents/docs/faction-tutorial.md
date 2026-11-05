# Faction tutorial

Read this before changing account onboarding, tutorial scenes, or tutorial card input.

New registrations and guest accounts call `FactionTutorialProgress.PrepareNewAccountIntro`
before opening `ComicIntroScene`. It loads the matching user's identity and persists
`pending` before the comic; recording it only in the selection scene loses onboarding
when the app closes during the comic. Comic completion and skip both open
`FactionSelectionScene`, never the legacy mock battle or the whole-onboarding skip.
Immediately after authentication, also persist the new-account intent using the
JWT's memberId solely as a local account key. Transfer it to the matching-user
checkpoint after user lookup succeeds, including on a later Lobby login. A failed
first user lookup redirects to Login; without this earlier intent it skips onboarding.
This decoded claim must never authorize a server operation or grant cards.

## Account and session state

The progression API owns faction selection, starter ownership and the starter deck.
`HELL` and `NATURE` must keep the five spell names in `FactionTutorialCards` aligned
with Lobby's progression policy. Trial spells `meteor_shower` and `tornado_strike`
are battle permissions, not card rewards.

The checkpoint is local PlayerPrefs, scoped by matching server and user ID. It
contains pending/completed, a request UUID saved before creation, and the ready
battle ID, public server URL and expiry. Preserve that UUID after an uncertain
creation response. Resume the known battle directly; only 404, 410 or explicit
expiry starts a new battle. This checkpoint does not move across devices.

Lobby checks pending after loading the user. An unfinished comic restarts the
comic; a completed/skipped comic resumes selection/connection. Persist comicSeen
before the handoff so user lookup failures cannot skip the comic entirely.
Existing users without pending are not forced through the tutorial. JWT refresh
must not invalidate a battle: compare owner and matching server, and use the
latest JWT for each request. Reject a response after the account/server changes.

## Input and rendering

`FactionTutorialBattleScene` copies GameScene's authored battlefield, card UI,
prefabs and object presentation. `StompConnector`, `PingSender` and fever-time
components are removed from that scene. Merely disabling them still runs Awake
and creates a second transport. The driver sends authenticated HTTP commands
and plays the normal object/event DTOs through `TutorialPlaybackBuffer`. Apply a
full snapshot only on initial entry, revision changes, reconnect, missing frames,
or excessive backlog/stall. A full snapshot after every poll interrupts movement
tweens and repeats transient Attack/Damaged presentation.

Keep received and displayed frame watermarks separate. HTTP can deliver several
simulation ticks at once; never dispatch all of them in one render frame. Use the
server's `tick` and `tickRate`, not capture sequence numbers: CAST creates another
capture without advancing simulation time. The buffer starts with 100ms of delay,
then plays one tick group per render update with the ordinary frame duration.
New captures at an already displayed tick still play once, deduplicated by frame.
Poll on a 50ms budget including request time; presentation continues while a
request is in flight. Use the same Unity time clock as the movement tweens.
Detect app/render stalls separately with realtimeSinceStartupAsDouble: Unity
clamps Time.time increments to maximumDeltaTime (333ms here), so that clock
cannot detect a 500ms stall. On return discard queued combat and pending terminal
panels before accepting a recovery snapshot.

A recovery snapshot discards queued history and resets both watermarks to its
lastFrame/tick. Do not replay the historical events in that response. Block input
on terminal server states immediately, but show their panel only after the final
queued frame and its movement tween finish. Cancel that pending panel on revision
changes, recovery and teardown.

Card selection/unselection stays local. Only casts go to the tutorial API.
Clear CardInputSender selection and expected mana before replacing a step's
cards. Destroying the CardUI alone leaves the previous magic and field-selection
mode alive; the next step then sends an unavailable spell.

The game server owns allowed spells, mana, formation order, combat deaths,
waves, next/retry and completion. The client must not advance on a cast, timer,
old cast history or a missing response. Do not implement damage/physics here.
Frames use a monotonically increasing watermark across step resets. Never replay
events at or below that watermark. Use the server's target coordinates for the
pointer: the first enemy-owned snapshot object can be a lingering elemental field.
Capture the world cast point when entering field-selection guidance instead of
reading the latest target in its screen-point callback. Tornado victims orbit,
so following each response makes the hand circle with them. Reproject the fixed
world point for the camera and choose a new point only on a new selection.

An uncertain command retries the same command UUID with its original revision
and payload. Keep cast input locked until it is accepted or definitively rejected.
The final lesson has no required-card pointer. A completed server response saves
the `lobby` checkpoint, separate from final `completed`; completing this battle
does not post a normal match result or grant XP. After the reward panel returns
to Lobby, load decks and start `GlobalTutorialManager` at `DeckState`. Reuse the
deck, magic book, lobby and practice-match guidance; do not restart `BattleState`.
Defer progression and quest reward initialization until this guidance finishes,
so their network requests cannot block the deck prompt. A returning Lobby scene
must preserve an existing manager rather than restart its current step.
Keep the final deck fetch after `PlayerProgressionPanel.Initialize()` as well:
initialization clears the deck cache, so fetching only before guidance leaves
the dropdown reading a null cache after the last step.

Only `FinishOnboarding` marks the checkpoint completed. Interrupting guidance
resumes at the deck button on the next login; old `completed` checkpoints stay
completed. The manager is scoped to the captured user and matching server. Check
that scope in both Update and the final callback, because a callback can precede
Update on the same frame as an account change. Stop without writing on mismatch.

## Authoring and validation

Run `Editor.FactionTutorialSetup.Build` to regenerate the two scenes and the
shared panel. Keep layout in existing UI prefab instances. `FactionTutorialUiCapture`
renders the actual authored panel with fixture card art in both locales; its
images are UI fixture renders, not a live account/server playthrough.

Run `FactionTutorialTests`, `FactionTutorialPlayModeTests` and
`ComicIntroPlayModeTests`. The Play Mode fixture disables the HTTP driver before
Start and checks real GameScene card selection across a revision change.
The buffer EditMode tests cover burst delivery, duplicate/late CAST captures,
revision and gap recovery, bounded backlog, stalls and underrun. The PlayMode
fixture also moves a real ServedObject through intermediate positions, checks
one attack presentation and waits for the final tween before showing success.
Completing a server lesson alone does not validate visual playback cadence.

Lobby guidance fixtures must create a persistent `SceneContext` before loading
LobbyScene if none exists. Disabling network startup in `sceneLoaded` is too
late for `AdminOnly.Awake`, which otherwise reports a missing context for JWKS.
Game's TutorialBattleTest separately runs all lessons against production magic,
prefab and physics code with a captured gameplay-parameter fixture.

Unity tests/rendering can dirty dynamic font atlases and the game-audio catalog
profiles through editor hooks. Compare the pre-test status and exclude only
known generated changes in this worktree; preserve pre-existing user edits.

Dependencies: Database V121, Lobby #106, Client #392/#396, Game #186 and Lobby
#107. Ship the server APIs before this client. No new database migration is needed.
