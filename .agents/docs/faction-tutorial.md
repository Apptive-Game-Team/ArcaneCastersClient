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
and applies the normal object/event DTOs, then reconciles a full snapshot.

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

An uncertain command retries the same command UUID with its original revision
and payload. Keep cast input locked until it is accepted or definitively rejected.
The final lesson has no required-card pointer. A completed server response alone
marks the local tutorial complete; completing this battle does not post a normal
match result or grant XP.

## Authoring and validation

Run `Editor.FactionTutorialSetup.Build` to regenerate the two scenes and the
shared panel. Keep layout in existing UI prefab instances. `FactionTutorialUiCapture`
renders the actual authored panel with fixture card art in both locales; its
images are UI fixture renders, not a live account/server playthrough.

Run `FactionTutorialTests`, `FactionTutorialPlayModeTests` and
`ComicIntroPlayModeTests`. The Play Mode fixture disables the HTTP driver before
Start and checks real GameScene card selection across a revision change.
Game's TutorialBattleTest separately runs all lessons against production magic,
prefab and physics code with a captured gameplay-parameter fixture.

Unity tests/rendering can dirty dynamic font atlases and the game-audio catalog
profiles through editor hooks. Compare the pre-test status and exclude only
known generated changes in this worktree; preserve pre-existing user edits.

Dependencies: Database V121, Lobby #106, Client #392/#396, Game #186 and Lobby
#107. Ship the server APIs before this client. No new database migration is needed.
