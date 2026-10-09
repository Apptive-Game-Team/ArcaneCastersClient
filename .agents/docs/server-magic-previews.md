# Downloaded magic previews

`GameDataRefresh` starts `MagicPreviewDataSource` in the background when the first
lobby initializes. Parameters, magic data, login and matching proceed independently.
The selected lobby's authenticated catalog pins server/revision. Three simultaneous
downloads check each replay's UTF-8 size and SHA-256. Ready manifests refresh open
book/hover UI. Three attempts use 5/10-second delays and a fresh catalog each time.

Runtime `MagicPreview` reads this store, then uses the existing JSON codec, frame
player and isolated presentation world. It has no bundled recording fallback.
Unknown prefabs/malformed recordings disable only that preview. Server switches
and logout abort requests/retries, drop memory and reject late responses. A catalog
that cannot be verified cannot unlock old files. One download failure preserves
successful recordings; 409 invalidates the manifest and triggers verification.

`ReplayFileCache` stores verified files in Application.persistentDataPath, namespaced
by lobby URL hash. PlayerPrefs holds no replay payloads. The WebGL plugin requests
IDBFS sync after downloads; quota/storage failures keep the memory cache usable.
Browser restart/quota behavior requires a deployed WebGL check. Editor filesystem
tests are not evidence of IDBFS durability.

The 85 regression recordings and GUIDs now live in `Assets/Tests/Editor/MagicPreviews`,
outside Resources and player references. PlayMode tests inject them into an Editor-only
clips field. The runtime prefab has no registration and that field/type is absent
from player code. The refresh tool updates only these test fixtures. Never add
recordings back to Resources or the prefab to register a new server scenario.

Deploy game/lobby first, then client. Run ReplayFileCacheTests, MagicPreviewTests and
a WebGL player check for current JSON, server switching, startup time and persistence.

For a CLI WebGL build, pass `-buildTarget WebGL` before executing BuildScript.
Specifying only BuildPlayer's target while the Editor still targets desktop can
fail at KeyInputSetting with CS0103 for UnityEngine.WebGLInput. Switching the active
target at launch includes the WebGL module references. Restore build-only define
changes before committing unrelated ProjectSettings edits.
