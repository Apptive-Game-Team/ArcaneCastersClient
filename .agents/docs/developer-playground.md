# Editor developer playground

Prepare with Tools > ArcaneCasters > Prepare Playground Scene outside Play Mode.
The editor derives Assets/DevPlayground/Generated/Playground.unity from the current
GameScene, removes ordinary match/coach/card/network behaviours, and adds panels and
buttons instantiated from the existing UI prefabs. The generated scene is ignored;
regenerate it after GameScene changes rather than maintaining a copied battle scene.

Run LoginScene, log in as an administrator, reach LobbyScene, then use
Tools > ArcaneCasters > Enter Playground. Both servers must explicitly enable
PLAYGROUND_ENABLED=true. Select Left/Right, click the field to set the target,
then click a magic icon to cast immediately. The server catalog includes every
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
The editor generator keeps layout in serialized scene objects and reusable prefab
instances; the runtime only populates repeated magic icons.

Do not merely disable ordinary match/network components in the generated scene.
Disabled MonoBehaviours still run Awake: PingSender dereferenced MatchInfo before
the independent session was ready and StompConnector created another transport.
Remove those components; preserve FieldSelector disabled only for its ground raycast.
