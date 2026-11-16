# Editor developer playground

Enter Play Mode, log in as an administrator, open AdminScene and click
Magic Playground. The scene is prepared automatically before Play Mode starts;
no Editor menu or manual preparation is needed. Closing or expiry returns to AdminScene.
The editor derives Assets/DevPlayground/Generated/Playground.unity from the current
GameScene, removes ordinary match/coach/card/network behaviours, and adds panels and
buttons instantiated from the existing UI prefabs. The generated scene is ignored;
regenerate it after GameScene changes rather than maintaining a copied battle scene.

Both servers must explicitly enable
PLAYGROUND_ENABLED=true. Select Left/Right and click a magic icon to select it.
Move the pointer to aim using the ordinary ground circle, then left-click the field
to cast at that location. Selection stays active for repeated casts; Escape or
right-click cancels it. Tab hides/shows both overlay panels to reach the whole field.
The camera retains GameScene's fullscreen viewport and aspect fitting. UI clicks,
captured UI presses and points outside the field/screen cannot cast. There is no
range restriction, and overlapping HTTP casts are blocked until the request finishes.
The server catalog includes every
registered magic, including rows omitted from the player-facing lobby catalog.
Missing art uses a named button. No hand, mana, ownership or deck gate is applied.

The five controls clear ally/enemy/all objects and toggle ally/enemy damage
immunity relative to the current dropdown. Players and neutral boundaries survive
clear; faction projectiles/buildings are cleared too. Immunity covers future units
but does not freeze their natural lifetime. The session expires after 300 seconds.

Game-specific playground code lives in Editor. The small PlaygroundHost behaviour
is in a separate assembly with defineConstraints=[UNITY_EDITOR] and no platform
restriction. Wrapping a script in the ordinary Assembly-CSharp in UNITY_EDITOR removed
its player type but the real WebGL BuildReport still packed its MonoScript asset;
the post-build guard caught that leak. Moving a MonoBehaviour into Editor also
fails: Unity refuses to attach editor scripts. The define-constrained host assembly
allows Play Mode attachment and is entirely absent from player compilation. Its
Editor bridge references Assembly-CSharp and installs the host's lifecycle callbacks;
the host assembly itself must never reference a predefined assembly.
Do not add the generated
scene to build settings or the exclusive assets to Resources, StreamingAssets or
Addressables. PlaygroundBuildGuard checks both normal build entrypoints and
pre/post player builds. A feature define such as DEV_BUILD does not exclude content
from ordinary development player builds. The host assembly must remain constrained
to UNITY_EDITOR regardless of DEV_BUILD.

Validate with PlaygroundValidation.Validate in a batch Editor. This reloads the
scene and tests intentional build-scene and Resources dependency leaks. Run both
normal WebGL build methods to check packed assets; static compilation is insufficient.
ValidateAdminEntry checks the actual admin-button click, playground transition and
return flow in Play Mode using a local identity fixture, with HTTP requests suppressed.
ValidatePlayMode also verifies icon selection, actual camera/ground projection,
captured coordinates/faction, UI and field bounds, pending-command recovery, repeated
casts, cancellation and panel visibility using a no-HTTP command fixture.
The AdminScene button is an EditorOnly prefab instance with no playground script
reference; its listener is installed by the Editor bridge. Scene processing also
removes it during builds, so the entry UI cannot ship in ordinary players.
IProcessSceneWithReport also runs during Editor Play Mode with a null report.
Strip the entry only for a real player build; otherwise Play Mode loses its button.
The editor generator keeps layout in serialized scene objects and reusable prefab
instances; the runtime only populates repeated magic icons.

Do not merely disable ordinary match/network components in the generated scene.
Disabled MonoBehaviours still run Awake: PingSender dereferenced MatchInfo before
the independent session was ready and StompConnector created another transport.
Remove those components; preserve FieldSelector disabled only for its ground raycast.

## Balance Test

Open `Tools > ArcaneCasters > Balance Test` for the login-free local balance scene.
Start Rust `ac-balance serve <snapshot.json> <suite.json>` first; the default port
is 6211 on loopback only. The scene generator reuses this playground's prefab UI
and GameScene framing. `PlaygroundHost.balanceMode` chooses the balance controller.
Both generated scenes and the host stay excluded from all player builds.

Use the catalog's snapshot values and selected overrides on a dedicated
`PresentationWorld`. Do not use the logged-in parameter cache: a local candidate
can have different aura radii. Reuse native prefab rendering and frame dispatch.
Filter `Wall` from presentation creation and snapshots: it is a server boundary
collider with no client prefab. Passing it to an isolated world throws
`Missing runtime prefab: Wall` and stops initial playback. Retain the boundary in
the Rust simulation.

Run `BalanceLabValidation.Validate` with the loopback server available. It verifies
actual field casting, server pause, candidate restart, export and replay in Play
Mode. Captures go under `Logs`, since Unity deletes `Temp` when the batch Editor
exits. Keep generated font atlas changes out of the feature commit.
When reusing a button's TMP label in a short status row, reset its inherited
margins before enabling ellipsis. The 25-pixel balance target row showed only
`...` because its inherited top/bottom margins left too little line height.
