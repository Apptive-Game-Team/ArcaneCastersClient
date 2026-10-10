# Hand-Edited Assets, Prefabs and Scenes

The Unity Editor cannot run in this environment, so `.asset`, `.prefab`, `.unity`
and `.meta` files are edited as YAML by hand. Read this before changing a
serialized type or deleting a script. Nothing here fails the build; every trap
below shows up only as a wrong value or a missing script at runtime.

## A serialized enum stores its integer, not its name

`Assets/Art/Images/UI/Card/CardImageMapper.asset` stores rows as
`elementType: 1`, not `elementType: Fire`. Change the declaration order of an
enum that any asset serializes and every existing row silently points at a
different member. Renaming the enum member does nothing; renumbering it changes
the data.

When you change such an enum:

1. grep the enum name and the serialized field name across `Assets/` to find
   every asset, prefab and scene holding a value.
2. Write down the old member for each stored integer, then write the new
   integer for that same member.
3. Rewrite the rows in the asset in the same pass as the enum.

This bit `CardType` → `ElementType`: `CardType.Fire` was `6` and
`ElementType.Fire` is `1`, so leaving the asset alone would have given every
element the wrong icon while still loading and rendering.

## Renaming a method is safer than changing its parameter type

`CardImageMapper.GetCardImage(string)` took a card name. After the redesign the
same call had to take an element name. Keeping the name would have left every
call site compiling and returning `null` at runtime. Renaming it to
`GetElementImage` turned each stale call site into a compile error, which is the
only review signal available when you cannot run the Editor. Prefer the rename
whenever the meaning of an argument changes but its C# type does not.

## Deleting a MonoBehaviour takes three edits, not one

Deleting `Assets/Scripts/Data/Magic/CombinedMagicResolver.cs` required:

1. the `.cs` and its `.cs.meta`,
2. the component entry in every prefab that carries it — both the
   `- component: {fileID: N}` line in the GameObject's `m_Component` list and
   the `--- !u!114 &N MonoBehaviour:` block itself
   (`Assets/Prefabs/MagicResolver.prefab`),
3. the `--- !u!114 &M stripped` block in every scene that instances that prefab,
   plus any `someField: {fileID: M}` line pointing at it
   (`Assets/Scenes/GameScene.unity`).

Find them by grepping the script's `.meta` guid across `Assets/`, then grepping
the local `fileID` each hit declares. Stop after step 1 and the prefab opens in
the Editor with a "missing script" component.

## Writing a new `.meta` by hand

Copy the shape from a sibling of the same importer. A script meta is three
lines: `fileFormatVersion: 2`, `guid: <32 lowercase hex>`, `timeCreated: <int>`.
The guid must be new — grep it across `Assets/`, `Packages/` and
`ProjectSettings/` before committing. Never reuse the guid of the file you are
replacing: scenes and prefabs resolve components by guid, so a reused guid makes
every old reference silently bind to the new type.

## Adding a UI button to a prefab is twelve documents, not one

`LogoutButton` was added to `Assets/Prefabs/UI/Lobby/Panal.prefab` by copying the
`DeleteAccountButton` GameObject. That copy is twelve YAML documents: the button
GameObject with its RectTransform, CanvasRenderer, `Image`, `Button`, the
`MonoBehaviour`, and `Shadow`, plus a `Text (TMP)` child GameObject with its
RectTransform, CanvasRenderer, `TextMeshProUGUI` and `LocalizeStringEvent`. A
thirteenth edit is easy to forget: the parent transform's `m_Children` list. The
GameObject exists without it, and it never renders.

Give every copied document a new `fileID`, then rewrite the references between
them in one pass — `m_GameObject`, `m_Component`, `m_Father`, `m_Children`,
`m_TargetGraphic`, and the `m_Target` of every `m_PersistentCalls` entry. Copying
the block and changing only the anchors leaves the new `Button` driving the old
button's component.

The check that catches all of it: every `{fileID: N}` in the file where `N` is
not `0` and the mapping has no `guid:` must resolve to a `--- !u!T &N` anchor in
that same file. Run it over the file before and after, and compare — the count of
unresolved references must stay zero and no anchor may be lost or duplicated.
Also confirm each component's `m_GameObject` points back at the GameObject that
lists it, and each child's `m_Father` points back at the transform that lists it.

Serialized fields are written in declaration order, and only `public` fields and
`[SerializeField]` ones appear. `protected` and `private` fields without the
attribute are absent, so the block for a `MonoBehaviour` deriving from
`DisableableButtonBase` holds that script's own fields and nothing from the base.

## Two different scripts can share a serialized field name

`ScrollRect` and `TMP_InputField` both serialize a field called
`m_ScrollSensitivity` — `ScrollRect`'s controls mouse-wheel scroll speed,
`TMP_InputField`'s controls how fast a multi-line text box scrolls its own
content. `Assets/Scenes/AdminScene.unity` has both: three `TMP_InputField`
documents (script guid `2da0c512f12947e489f739169773d7ca`) and one `ScrollRect`
document (script guid `1aa08ab6e0800fa44ae55d278d1423e3`), and all four held the
same value. A text search-and-replace on the field name alone would have
changed the wrong component's behavior without any error.

Before editing a serialized field by name across a project, grep for the field
name first, then filter each hit to the enclosing document's `m_Script` guid.
Only a match on both the field name and the owning script's guid is the real
target.

## An object inside a nested prefab has a computed fileID

A scene or prefab that instances another prefab does not store the instanced
objects, so there is no anchor to copy when you need to point at one (an
`m_OnClick` target, a `m_TargetGraphic`, an override on a nested object). Unity
derives the id: `(prefabInstanceFileID XOR sourceFileID) & 0x7FFFFFFFFFFFFFFF`.
For example, in `Panal.prefab` the `ProfileButton` instance is
`3866810188118442953` and the Button inside `Button Variant` is
`1254884346060698375`, so the Button is `2649125704893739214` in `Panal.prefab`.
Compute it this way instead of guessing; a wrong id is accepted silently and
the reference is simply null at runtime.

## Disabling a component that drives a RectTransform wakes up stale overrides

A `ScrollRect`, `LayoutGroup` or `ContentSizeFitter` rewrites the rects it
controls every frame, and the scene still saves whatever values the Editor last
saw for those rects, often as PrefabInstance overrides such as
`m_AnchorMax: 0,0` on a Viewport. They are harmless while the driver runs.
Disable the driver and they take effect: the settings panel's content collapsed
to a zero-size rect this way. Before disabling one, grep the scene for
overrides of every rect it drove and delete or correct them.

## A dynamic TMP font asset can be written by hand

`Assets/Art/Fonts/LilitaOne SDF.asset` and `Jua SDF.asset` were written without
the Editor by copying TextMesh Pro's own
`LiberationSans SDF - Fallback.asset`: `m_AtlasPopulationMode: 1`, empty
`m_GlyphTable` and `m_CharacterTable`, a 0x0 atlas `Texture2D` sub-asset, and
`m_FaceInfo` filled from the TTF's `hhea`/`OS/2` metrics scaled to
`m_PointSize`. TMP rasterises glyphs into the atlas at runtime, and a WebGL
build renders them. `hashCode` is
`GetSimpleHashCode(name)`: `h = ((h << 5) + h) ^ c` over the characters as a
32-bit int. Point `m_SourceFontFile` at the TTF, whose importer must keep
`includeFontData: 1`, or the build ships a font asset with nothing to rasterise.

Size the atlas for the text, and keep `m_IsMultiAtlasTexturesEnabled` off. The
first version sampled at 90 pt with padding 9 on one 512x512 atlas, which holds
about 30 glyphs. Once it was full, TMP took every later glyph from the Jua
fallback, so one stat row showed "300" in Lilita One and "0.5" in Jua. The
assets now sample at 64 pt with padding 6: Lilita One (Latin only) on one
1024x1024 atlas, and Jua (every Hangul in the ko-KR tables, about 470) on one
2048x2048 atlas. When you change the sampling size, scale every `m_FaceInfo`
metric by the same ratio and set the materials' `_GradientScale` to padding + 1.

Multi-atlas broke the Editor. TMP 3.0.7 adds each new atlas page as an unsaved
sub-asset and then reimports the font asset, which drops the page. Text still
pointing at it throws `MissingReferenceException: The variable m_AtlasTextures
of TMP_FontAsset doesn't exist anymore` from `TMP_MaterialManager.GetFallbackMaterial`.
Grow the single atlas instead of turning multi-atlas back on.

## UI sprites are rendered from CSS, not painted

`Assets/Art/Images/UI/Flat/*.png` come from the mockup's CSS rendered in
headless Chromium (`omitBackground`, device scale 2.5). Their 9-slice borders
in the `.meta` are the CSS corner radius plus outline plus shadow, times 2.5.
Change a sprite by changing the CSS and rendering again; a hand-painted PNG will
not match the rest, and a border smaller than the rounded corner stretches the
corner.

## A TMP text colour is stored twice

`TextMeshProUGUI` serializes its colour as `m_fontColor` (floats) and as
`m_fontColor32.rgba`, a packed ABGR integer, with a `serializedVersion: 2`
line between `m_fontColor32:` and `rgba:`. Change both. A regex that expects
`rgba` right after `m_fontColor32:` silently misses it, and the two values then
disagree. Ink `#1C1A2B` is `rgba: 4281014812` (0xFF2B1A1C).

## A legacy Text that does not fit its rect draws nothing

`UnityEngine.UI.Text` with `m_VerticalOverflow: 0` (Truncate) drops every line
that does not fit the rect's height, so a single line taller than the rect
leaves the text completely empty, not clipped. The login inputs had a 34-unit
field with the text rect inset 10 top and bottom (14 left) and size 13 (a line
of about 16): typed text and the placeholder were both invisible while the
field still took focus. Give input text rects at least 1.3x the font size in
height, or set `m_VerticalOverflow: 1`.

## A checked-in count of hand-placed instances can be stale — recount before trusting it

A task brief said `GameScene.unity`'s `Map` GameObject parents "~76" `tree_1`..
`tree_4`/`rock` `SpriteRenderer` children. The actual count, read from the
scene's own `m_Children` list under the `Map` `Transform` (fileID `1756437240`),
is 271 (`tree_1` 65, `tree_2` 69, `tree_3` 59, `tree_4` 77, `rock` 1); no
`grass_1`/`grass_2` instances exist in this scene yet. A prefix-matching approach (`BattleThemeApplier` walks `foreach (Transform
child in transform)` and matches each child's name against `tree_1`,
`tree_2`, … rather than holding 76 or 271 individual references) survives this
kind of drift; a fixed list or count written into a script or a doc does not. When a brief states a count of
hand-placed scene instances, recount from the `.unity` file's own children list
before designing around it — do not carry the number forward unchecked.

## Validating C# compiles here without the Editor or a generated `Assembly-CSharp.csproj`

`dotnet build Assembly-CSharp.csproj` (see the root `AGENTS.md`) assumes the
Editor has generated that project file at least once. In a fresh worktree in
this environment the Editor never runs, so the `.csproj` (gitignored) does not
exist and cannot be generated. `dotnet` itself is also not on `PATH` inside
WSL, but `/mnt/c/Program Files/dotnet/dotnet.exe` is, and WSL runs `.exe`
transparently — the same binfmt_misc path the `unity-cli` skill documents for
`unity.exe`. The same path-translation trap applies: a Linux-style absolute
path handed to the Windows `dotnet.exe` is misparsed as a compiler switch
(`error CS2007: Unrecognized option: '/home\...'`). Copy the sources to a
`/mnt/c/...` working directory (or run `wslpath -w` on every path argument)
before invoking it.

## `StagePanel` caps scenarios at the prefab's button count, silently

`Assets/Scripts/Adventures/Scenarios/StagePanal.cs`'s `StagePanel.PropagateSetup`
loops `for (var i = 0; i < buttons.Count; i++)` and only ever reads
`stage.Scenarios[i]` for `i < buttons.Count`. A stage with more scenarios than
its `stagePanelPrefab` has `ScenarioButton` slots does not throw and does not
show a fifth button — the extra scenarios are simply never displayed, and there
is no error to notice. `Assets/Prefabs/UI/Adventures/StagePanel/Stage.prefab`
carries exactly 4 slots (built for Stage 1's 4 scenarios); it happens to be
enough for every stage introduced with the adventure rework (each new stage has
3 scenarios), so all of Stage2–Stage4 reuse it unchanged with the trailing slot
left inactive. Before wiring a new `AdventureStageScriptableObject.stagePanelPrefab`,
count that stage's scenarios against the target prefab's `buttons` list
(`Stage.prefab`'s `StagePanel` component lists 4 `fileID`s) rather than assuming
the existing prefab scales.

## A stub stage asset can carry a placeholder wiring and a field the class no longer has

`Stage2.asset` existed before this content pass as a placeholder: its
`stagePanelPrefab` pointed at `EmptyStage.prefab` (a `StagePanel` with
`buttons: []`, guid `c5e33552230124a83a36a0d1ed9d9fea`) instead of the real
`Stage.prefab`, and it carried a `stageName:` `LocalizedString` block that
`AdventureStageScriptableObject` (`Assets/Scripts/Data/Adventures/Local/AdventureStageScriptableObject.cs`)
no longer declares as a field — Unity's serializer keeps unknown keys in a
`.asset` file instead of erroring, so the stale block sat there silently until
someone diffed the class against the asset. When filling in a stub
`AdventureStageScriptableObject` asset that predates the class's current shape,
diff every field the `.asset` declares against the script's current
`[SerializeField]` list and delete anything the script no longer has, in the
same pass that fills in the real content.

## `stone_fortress.png` is the adventure icon, not a story backdrop

`Assets/Art/Images/Adventure/stone_fortress.png` (guid
`b9322f20c555b4d75bcafccb4ea3cf2b`) is already wired as `FortressAdventure.asset`'s
`iconImage` and is 256×183 — a thumbnail. `forest_adventure_stage_1.png` (guid
`2d4e57a2719ef429aa4d24882eca8f28`), the `backgroundImage` both existing forest
stages use for the pre-match story overlay, is 867×256. Stretching the icon to
that overlay's size would visibly pixelate it, so `Stage3.asset` and
`Stage4.asset` reuse `forest_adventure_stage_1.png` for `backgroundImage`
instead of the fortress icon. A new fortress-specific story background, sized
to match the forest one, is still open work.

A hand-built throwaway `.csproj` referencing `UnityEngine*.dll` from any
locally available Unity player build's `Managed/` folder (even one from an
unrelated project — the module DLLs are stable enough for `MonoBehaviour`,
`ScriptableObject`, `SerializeField`, and other core-engine types) compiles
enough of `Assets/Scripts` to catch namespace, symbol and signature errors in
new or edited files. It will not compile cleanly end to end: a Managed folder
from a different project's build will be missing whatever packages that
project didn't use (this repository's `com.unity.localization` and
`com.unity.addressables` are common gaps — stub the handful of types actually
referenced, such as `LocalizedString.GetLocalizedString()`, rather than
chasing the whole package) and may carry a different `DOTween`/`Newtonsoft.Json`
version than this project pins, which shows up as errors in unrelated files
(`SpriteRenderer.DOFade`, a `JsonSubtypeConverter` overload). Confirm those
errors are confined to files you did not touch — grep the error log for your
changed file names — rather than treating a nonzero error count as a failed
check.

## The Windows checkout's `ScriptAssemblies` make a real compile check

`/mnt/c/Users/jys09/Projects/ArcaneCasters/Client/Library/ScriptAssemblies` holds
the DLLs the Editor last built for this project: `Assembly-CSharp.dll`,
`WordOnline.*.dll`, `Unity.TextMeshPro.dll`, `Unity.Localization.dll` and the
rest. Compiling changed files with the Roslyn `csc.dll` from
`/mnt/c/Program Files/dotnet/sdk/<version>/Roslyn/bincore/` against those DLLs,
the 2022.3.34f1 Editor's `Managed/UnityEngine/*.dll`, its
`NetStandard/ref/2.1.0/netstandard.dll` and
`NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll` checks the new code against
the project's real types instead of stubs. Two things make it work:

- Pass every argument through a response file (`@args.rsp`) with quoted Windows
  paths. `Program Files` split on the command line becomes a missing source file
  for every reference.
- Compile a changed asmdef assembly under its real name (`-out:WordOnline.Contracts.dll`).
  Under any other name, `Assembly-CSharp.dll` still asks for `WordOnline.Contracts`
  and every type it exposes from there fails with CS0012.

Unity's NUnit (`com.unity.ext.nunit`) throws `TypeLoadException` for
`System.Runtime.Remoting.Messaging.CallContext` on .NET 8, so it cannot run the
EditMode tests outside the Editor. Running test sources that only touch plain C#
needs a small stand-in for `Assert`, `[Test]` and `[TestCase]`; that checks the
logic, not NUnit's own behavior.
### A full compile, Localization included, from a Windows checkout's generated projects

When a Windows checkout of this project has been opened in the Editor (on the
profile quests work it was `C:\dev\ac-client`), its generated `*.csproj` files and
`Library\ScriptAssemblies` / `Library\PackageCache` give a real compile of a WSL
worktree, `com.unity.localization` and Addressables included. Copy the worktree's
`Assets/Scripts` and `Assets/Tests` under `C:\temp\<name>`, copy the generated
`Assembly-CSharp.csproj` and `WordOnline.*.csproj` next to them, replace every
`<Compile Include>` with the worktree's own file list (one list per `.asmdef`
folder, everything else under `Assets/Scripts` minus `Editor` folders for
Assembly-CSharp), and point the `Assembly-CSharp-firstpass` and `MCPForUnity.*`
project references at the DLLs in that checkout's `Library\ScriptAssemblies`.
`dotnet.exe build` on `Assembly-CSharp.csproj` then reports 0 errors only when the
new code really compiles; the remaining warnings are reference-version conflicts.
The checkout's packages are whatever it last imported, so compare its
`Packages/packages-lock.json` with yours before trusting a package API.

An earlier compile workspace (`C:\temp\claude-pq`) is a tempting shortcut: copy
its `*.csproj` and only swap the `<Compile Include>` lists. Its project files are
a snapshot of the asmdefs at the time, not of yours. When the chest work added
`WordOnline.Serialization` to `WordOnline.Contracts.asmdef`, the copied
`WordOnline.Contracts.csproj` still lacked that `ProjectReference`, and the build
failed with `CS0234: 'Serialization' does not exist in the namespace 'Global'`
in three contract files nobody had touched. Compare each copied project's
`ProjectReference` list with the `references` of the matching `.asmdef` before
reading the error list.

The EditMode test DLL built this way cannot be run outside Unity: Unity's
`nunit.framework.dll` needs .NET Framework remoting (`CallContext`). Tests that
touch only `WordOnline.Contracts` and `JsonCodec` run under `dotnet test` in a
`net8.0` project that compiles those sources directly with the NuGet `NUnit`,
`NUnit3TestAdapter` and `Newtonsoft.Json` packages; drop the two converters in
`JsonCodec.CreateSettings()` that need UnityEngine types.

## ProfileScene had no SystemMessage overlay

`SystemMessageUI.Instance` is a scene-local singleton set in `Awake`, and the
`SystemMessage` prefab is instanced per scene. Every scene had it except
`ProfileScene`, so a `SystemMessageUI.Instance.ShowMessage` there was a
`NullReferenceException`. Before calling it from a scene, grep the scene for the
prefab guid `c3bf6386364e64872904c6d1fa77d05d`.

## Everything under the adventure map's `Nodes` is destroyed on every rebuild

`AdventureMapController.Show` deletes every child of `nodesContainer` (`Nodes` in
`AdventureScene.unity`) before it draws the stage nodes again, except the objects
it names: `currentMarker` and `endOfRoadSlot`. A scene object placed under `Nodes`
without its own field in that skip list loads, renders in the Editor, and is gone
the first time the map draws. The end-of-adventure chest lives there for that
reason as `endOfRoadSlot`; anything else that has to sit on the road needs the
same treatment.

## A flat ground tile keeps its `SpriteRenderer` on the prefab root

`RiverWater` and `RiverBridge` lie flat on the ground (root rotated 90 degrees
about X). `ObjectSpawner` calls `PopupBookVisualPresenter.Attach`, which wraps
the sprite returned by `ServedObject.GetActualTransform()` in a pivot and, in
`LateUpdate`, rotates it to the camera. When that sprite is a child of the root,
the tile stands up and plays a spawn tilt. When the `SpriteRenderer` sits on the
root, `Attach` returns early and the tile stays flat. Do not add child renderers
to these prefabs; the art is the overlay described below.

The ground is the opaque `PopupBookGround` plane at `y = 0`, and the server sends
`y = 0`, so a flat sprite at the same height flickers against it. `GroundDecalLift`
keeps the tile at least 0.02 above the ground in `LateUpdate`. Raising it once in
`Start` was not enough: right after creating an object the server sends a position
update with `y = 0`, and `PositionUpdater` tweens the transform to it, which erases a
height set earlier. Anything laid flat on the ground has to restore its height after
`PositionUpdater` has run.

The prefabs no longer draw the river. Their root `SpriteRenderer` stays, with an
empty sprite, only so `ServedObject` and `PopupBookVisualPresenter.Attach` find it on
the root and leave the tile flat; both already handle a null sprite. The picture is one
overlay, drawn once per match by `RiverOverlay`
(`Resources/Prefabs/RiverOverlay.prefab`). Each `RiverWater` and `RiverBridge` carries a
`RiverOverlayMember`, which counts the live river objects per `PresentationWorld`
(null for the real match). The first member to wake creates the overlay, the last one
destroyed destroys it, so the order objects arrive in does not matter, a rematch in the
same scene reuses the one overlay, and nothing is left after the match. Inside a
`PresentationWorld` the overlay is a child of the world.

The numbers live in `GameScene.Dto.RiverOverlayLayout` (assembly
`WordOnline.GameContracts`, tested by `RiverOverlayLayoutTests`), all at 256 pixels per
unit and lifted 0.02 above the ground (the overlay is not driven by `PositionUpdater`,
so one lift at creation is enough):

| picture | pixels | covers | pivot | position |
| --- | --- | --- | --- | --- |
| `RiverWaterOverlay.png` | 1024 x 2560 | x 7 to 11, z 0 to 10 | bottom left | (7, 0.02, 0), sorting order -2 |
| `RiverBridgeOverlay.png` (twice) | 1024 x 1024 | 4 x 4 units | center | (9, 0.02, 2.5) and (9, 0.02, 7.5), sorting order -1 |

The water has soft banks that cross the logical lines x = 8 and x = 10 by up to 0.25
unit. The bridge at 2.5 opens z 1 to 4 (rows 1 to 3), the one at 7.5 opens z 6 to 9
(rows 6 to 8). The server contract is unchanged: 8 `RiverWater` and 12 `RiverBridge`
objects at cell centers (columns 8 and 9; water rows 0, 4, 5, 9; bridge rows 1, 2, 3, 6, 7, 8).

If the server moves the river, change the constants in `RiverOverlayLayout` and repaint
the two pictures for the new rectangle. An image generator does not hit pixel positions
by itself: generate the water and the bridge as separate images on a flat key
color, key the background out (`.art/tools/key-out-background.py`), resample to 256
pixels per unit, place them at the numbers above, and draw the logical grid over the
result (mock: `docs/pr-media/261/river-mock-64ppu-logical-guides.png`, ground texture
resampled to 64 pixels per unit, one background texture covering 20 units) before
accepting. The `.meta` files set sprite single, 256 pixels per unit, `maxTextureSize`
4096 (the 2560 pixel height must not shrink), no mipmaps.

## Changing a CanvasScaler reference resolution

Issue #269: `LobbyScene.unity` had a canvas at 800x450 while every other canvas is
1920x1080, so the lobby drew 2.4 times larger than the rest. Changing
`m_ReferenceResolution` alone makes the whole canvas 2.4 times smaller, so every
child must be multiplied by 2.4 too. Several things hide from a plain multiply:

1. A `PrefabInstance` stores its geometry as `m_Modifications`
   (`m_SizeDelta.x`, `m_fontSize`, `m_margin.x` ...). Scale those values, and add a
   modification for every value the prefab supplies that was never overridden
   (`Button Variant` text: `m_fontSizeMin` 9, `m_fontSizeMax` 15, margin 10/2/10/5).
   Otherwise the autosize range and margins stay at the old scale.
2. A `Sliced` image keeps its border in sprite pixels, so after a 2.4 times larger
   canvas the border looks 2.4 times thinner. Divide `m_PixelsPerUnitMultiplier` by the factor.
3. Scripts that build UI at runtime hard-code pixel sizes for the canvas they were
   written against (`FriendBootstrap` friend modal 760x560, moved into
   `Assets/Prefabs/UI/Lobby/Friend/FriendModal.prefab` by #309;
   `LobbyUIController` reward popup 430x230). Serialized script fields in canvas
   units hide the same way: `BattleHoverPresenter.bounceHeight` (12, now 28.8) is a
   plain float that no RectTransform search finds. Grep `sizeDelta` and `anchoredPosition`
   in `Assets/Scripts` before changing a canvas, and list what you could not rescale.
4. Do not shrink the battle HUD's hand bar by editing YAML. `GameScene`
   `BarController.MoveBar` moves the `Bars` rect between the literal y values 540
   and 240, which assume the old bar height, and `LowerBar` and `ManaBar` stretch
   across the screen width. #297 scaled `LowerBar`, `ManaBar` and `Timer` by
   `m_LocalScale` 0.75 and changed their `m_SizeDelta` and grid padding; the
   developer opened it in the Editor and the hand bar was broken, and the mana bar
   floated about 150 above the hand. The `GameScene.unity` change was reverted.
   Resizing the HUD needs the two `MoveBar` positions redesigned with it and a check
   in the Editor. `CardImage.prefab` (the hand card) is also used by
   `SpectatingScene` and `InteractiveTutorialScene` and has fixed-size children, so
   do not edit the prefab or the grid cell size for this either.
5. A GameObject the scene adds under a prefab instance is a normal document whose
   `m_Father` is a `stripped` RectTransform. A rescale script that walks
   `m_Children` from the canvas never reaches it, because a stripped transform has
   no `m_Children`; it lives only in the PrefabInstance's `m_AddedGameObjects`.
   #297 missed the hamburger `Icon` under the `Menu` Button Variant this way: the
   button grew to 87x94.5 and the icon stayed 16.25x16.25, so it showed as a tiny
   icon inside a large button. Walk ancestors upward from every RectTransform
   instead (a stripped transform's parent is its PrefabInstance's
   `m_TransformParent`), and keep only those that reach the rescaled canvas. A
   root PrefabInstance with its own `Canvas` is a separate canvas: `MatchingPage`
   in `LobbyScene` is 800x450 inside its prefab, so its `CoachPanel` and
   `CoachCloseButton` stay unscaled.
6. The prefab's own `m_PixelsPerUnitMultiplier` counts as an unoverridden value
   from item 1: `Button Variant` ships 4, so every Sliced instance on a 1920 canvas
   needs a `1.66667` modification on `1683031503725330102`. `RectOffset` fields
   (`m_Padding` of a layout group) hold integers; round them after multiplying
   instead of writing 9.6, which #297 did for `UserNamePill`.

## A modal prefab saved inactive in the scene

`FriendModal.prefab` (#309) is instanced once in `LobbyScene` with an
`m_IsActive: 0` override, and its controller `FriendModalUIController` sits on the
prefab root. Two things follow, and neither shows an error:

- `FindObjectOfType<T>()` skips inactive objects and returns null. `FriendBootstrap`
  uses `FindObjectOfType<FriendModalUIController>(true)`.
- The controller's `Awake` does not run until the first `SetActive(true)`, so it
  must not deactivate itself in `Awake` (the old controller did, which would close
  the modal the moment it opened). Listener wiring is idempotent and also called
  from `Open()`.

`FriendManager` is added with `AddComponent` at runtime, so its serialized
`friendModal` was always null and friend events never refreshed the open modal.
`FriendBootstrap` now hands the scene instance over with `BindFriendModal`.

`Button Primary.prefab` has no instance anywhere in the project, so ids computed
inside it are unverified. The friend prefabs use `Button Variant` instances with the
same nine modifications `Button Primary` applies (orange `m_Color`, outline
material, white text) instead. A generator for this layout is the fastest route:
the modal and three row prefabs are about 300 YAML documents.
