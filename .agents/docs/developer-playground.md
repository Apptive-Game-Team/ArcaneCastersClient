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
