---
name: unity-ui-prefabs
description: Use when creating or modifying Unity UI scenes, pages, panels, buttons, list items, or HUD/overlay UI in this client. Enforces inspecting existing scenes first and using Assets/Prefabs/UI prefabs or creating reusable UI prefabs instead of hand-building one-off UI.
---

# Unity UI Prefabs

Use this skill for UI work in `word-online/dev/client`.

## Rule

When making UI, start from existing UI prefabs and scene patterns. Do not hand-build buttons, panels, cards, or common UI containers in code or raw scene YAML unless the user explicitly asks for that approach.

Use the default project background for new UI scenes/pages. In this client, that means the scene-authored Canvas child named `Background` used by Login, Register, and Lobby scenes: an `Image` using `Assets/Art/Images/Background/background.png` with preserve aspect enabled. Do not replace it with custom camera colors, ad hoc full-screen images, or new background panels unless the user explicitly asks for a distinct background.

## Workflow

1. Inspect nearby scenes that already solve a similar UI problem.
   - Search `Assets/Scenes/*.unity` for existing `m_SourcePrefab` usage.
   - Check how the scene overrides prefab positions, text, scripts, and serialized references.
2. Inspect reusable UI prefabs under `Assets/Prefabs/UI`.
3. Choose the closest existing prefab or variant:
   - Generic panel/container: `Assets/Prefabs/UI/UI-Base.prefab`
   - Brown panel/container: `Assets/Prefabs/UI/Brown-UI-Base.prefab`
   - Standard button: `Assets/Prefabs/UI/Button Variant.prefab`
   - Debug item button: `Assets/Prefabs/UI/Debug/DebugItemButton Variant.prefab`
   - Adventure button: `Assets/Prefabs/UI/Adventures/AdventureButton Variant.prefab`
   - Stage/scenario tiles: `Assets/Prefabs/UI/Adventures/StagePanel/*.prefab`
   - Reward popup: `Assets/Prefabs/UI/RewardUI.prefab`
4. Use the same default background approach as nearby scenes.
   - For the current login/register/lobby pattern, add a `Background` Image as the first Canvas child.
   - Use sprite guid `4fe2d02ca2c9b49fc938a577493218e1` (`Assets/Art/Images/Background/background.png`), size `{x: 800.01, y: 605.9091}`, center anchors, and `m_PreserveAspect: 1`.
5. Build scene UI as prefab instances with overrides, not independent recreated objects.
6. If no prefab matches and the element is reusable, create a new prefab under `Assets/Prefabs/UI/<Domain>/` or `Assets/Prefabs/UI/`.
7. For repeated list rows/cards/items, create a prefab and populate it from code with a factory/controller. Do not duplicate one-off rows in the scene.
8. Keep behavior in scripts and layout/visual structure in scene-authored prefabs.
9. Ignore unrelated dirty files unless the user explicitly asks to include them.

## Scene Wiring

- Scene buttons should be scene-authored prefab instances.
- Wire button `OnClick` to an existing `ButtonBase` subclass or scene controller method.
- Do not create UI buttons from `Awake`, `Start`, or other runtime code unless the UI is truly dynamic and repeated.
- If a page deserves its own scene, add the scene to `ProjectSettings/EditorBuildSettings.asset` and navigate with `SceneManager.LoadScene`.

## Prefab Creation

Create a new prefab when:

- the same UI structure appears in more than one place
- a list/table/grid needs repeated item views
- the UI component has its own controller script
- a future scene is likely to reuse the same panel/button/card layout

Prefer duplicating the nearest existing prefab and making minimal overrides. Keep root names, prefab filenames, and controller names aligned.

## Validation

- Confirm each new scene UI element that should be reusable is backed by `m_SourcePrefab`.
- Confirm new prefabs and `.meta` files exist.
- Run a scoped `git diff --check` on changed UI scene, prefab, script, and build settings files.
- If Unity is available, run an Editor import/compile check or open the scene in Unity.

## Current Patterns

Existing scenes heavily instantiate:

- `Button Variant.prefab` for normal buttons
- `UI-Base.prefab` / `Brown-UI-Base.prefab` for framed UI surfaces
- specialized variants under `Assets/Prefabs/UI/Adventures` and `Assets/Prefabs/UI/Debug`

Follow those patterns before inventing new UI structure.

## Instancing a UI prefab by hand

The Editor cannot open this checkout from WSL, so a new panel is usually written
as prefab and scene YAML. Read `../../docs/hand-edited-assets.md` first; this
section is only what the UI prefabs add on top of it.

### The target `fileID` of an inherited object is the source file's stripped anchor

`Button Variant.prefab` is a variant of `Brown-UI-Base.prefab`, which is a
variant of `UI-Base.prefab`. Its root GameObject, RectTransform and `Image` are
all inherited, so their ids inside `Button Variant.prefab` are the `stripped`
anchors that file declares — not the ids those objects carry in `UI-Base.prefab`.
An instance elsewhere must target the stripped anchors:

| Object | `fileID` in `Button Variant.prefab` |
| --- | --- |
| root GameObject | `1042010048011351009` |
| root RectTransform | `1972775562532451163` |
| root `Image` | `1683031503725330102` |
| `Button` | `1254884346060698375` |
| `Text` GameObject | `1414512995440436844` |
| `TextMeshProUGUI` | `7149443897086103952` |

`Brown-UI-Base.prefab` is the same trap one level up: its RectTransform is
`2718469730974271840` and its GameObject is `3507375722896574938`, and neither
string appears anywhere in that file. Unity computes the id; you cannot read it
off the base.

Do not guess. Copy the numbers from an existing instance —
`Assets/Prefabs/UI/Global/ConfirmPage.prefab` instances `Button Variant.prefab`
and `Assets/Scenes/GameScene.unity` instances `Brown-UI-Base.prefab` — or from
the table above. A wrong id does not fail the import: the `m_Modifications`
entry silently applies to nothing, so the button keeps the base's size,
position and name and looks like the override was never written.

### An icon-only button turns the text child off

`Button Variant.prefab` ships with a `Text` child reading `매칭 시작`. For a
button that carries only a picture, add one modification setting `m_IsActive` to
`0` on GameObject `1414512995440436844` and put an `Icon` GameObject
(RectTransform, CanvasRenderer, `Image`) in `m_AddedGameObjects` under the
button's RectTransform. Blanking `m_text` instead leaves an empty
`TextMeshProUGUI` that still lays out and still raycasts.

An icon-only button needs nothing from `Assets/Localization`, which is the
cheapest way to keep a new panel out of the localization id collisions that
`../../docs/localization.md` describes.

### Generate the YAML, then check every reference

Six button instances are around fifty YAML documents. Write them from a small
script rather than by hand, then run the check
`../../docs/hand-edited-assets.md` prescribes over the result: every `{fileID:
N}` with no `guid:` and `N` other than `0` must resolve to an anchor in the same
file, no anchor may repeat, and every `m_Children` entry must be matched by the
child's `m_Father`. Run the same check over a file you did not touch, such as
`ConfirmPage.prefab`, to confirm it reports nothing there before trusting it on
yours.

`git diff --check` will report trailing whitespace on `m_Name: `,
`m_EditorClassIdentifier: `, `userData: ` and their kin. Unity writes those
lines that way — `Assets/Resources/Prefabs/Player.prefab` alone holds 21 of them
— so match the existing files rather than stripping the space.

## A UI drag must be excluded from the battlefield for the whole press

`FieldSelector.Update` acts on `Input.GetMouseButtonUp(0)` and decides the
release is not its own by calling `PointerInputUtility.IsPointerOverUiOrSelectable()`,
which raycasts at the release position. That check only looks at where the
pointer ended. A control the player presses on and drags off — the emote picker
is the first one here — therefore casts a magic whenever the finger leaves the
panel before it lifts.

A UI that takes a press for its whole life must say so:
`PointerInputUtility.BeginPointerCapture()` on pointer down and
`EndPointerCapture()` on pointer up, and every reader of a field release checks
`PointerInputUtility.IsPointerCapturedByUi` before
`IsPointerOverUiOrSelectable()`. Release it in `OnDisable` too, or a control
that is switched off mid-press leaves the field dead.

The capture stays true until the end of the frame it was released in, on
purpose. `EventSystem` dispatches `OnPointerUp` from its own `Update`, and
nothing orders that against `FieldSelector.Update`. Clearing the flag the
instant the pointer lifts leaks the release on exactly the frames where the
`EventSystem` happens to run first, which is the half of the time that looks
like an intermittent bug.

## Touch has no hover, so decide by raycasting the release

`IPointerEnterHandler` never fires for a finger sliding across options. Read
what is under the pointer with `PointerInputUtility.FindUnderPointer<T>(position)`,
passing the position off the `PointerEventData` the event carries.
`IsPointerOverUi()` and its neighbours read `Input.mousePosition` instead, which
is the pressed pointer only by coincidence.

## The existing speech bubble lives inside a handler file

`Assets/Art/Images/UI/SpeechBubble.png` and its copy at
`Assets/Resources/UI/SpeechBubble.png` are drawn by `PveSpeechBubbleUI`, an
`internal` class at the bottom of
`Assets/Scripts/GameScene/Handler/PveScriptEventHandler.cs`. Nothing in the file
name says so, and searching for a `SpeechBubble` script finds nothing.

That class is the pattern for anything that has to float above a `ServedObject`:
a screen space overlay `Canvas` whose `RectTransform` follows
`ServedObject.GetSpeechBubbleAnchorWorldPosition()` through
`Camera.main.WorldToScreenPoint`. Read
`../../docs/scene-space.md` for why a world space sprite is the wrong answer —
the camera is tilted 45°, so a bubble standing in the world plane is foreshortened.

## Where a button's look lives

`Button Variant.prefab` is a variant of `Brown-UI-Base.prefab`, which is a
variant of `UI-Base.prefab`. The `Image` and the `Shadow` exist only in
`UI-Base`; `Brown-UI-Base` adds the brown tint `#D99F71` as an `m_Color`
override. `Brown-UI-Base` is also placed directly as a panel
(`Global/ConfirmPage` `Panel`, `Lobby/MatchingPage`, `SystemMessageUI`,
`Tutorial/TutorialSelectPanel`, `DebugItemButton Variant`,
`Resources/UI/Adventures/AdventureStoryOverlayUI`), so change how every
standard button looks through overrides in `Button Variant.prefab`, never in
`Brown-UI-Base` or `UI-Base`. Since #118 those overrides set the wooden plank
sprite `Assets/Art/Images/UI/WoodPlankButton.png`, reset the tint to white
(otherwise the brown multiplies into the wood), set
`m_PixelsPerUnitMultiplier` to 3 and the `Shadow` to `y: -4`. Two lobby
buttons are hand-built with their own `Image` and carry the sprite directly:
`Lobby/CreditsPanel` `BackButton` and `Lobby/JoinPanal` `RegisterButton`.

A Sliced `Image` draws its border at `border / m_PixelsPerUnitMultiplier`
canvas units, whatever the canvas reference resolution is. Lobby, Login and
Register use an 800x450 canvas; Adventure, Adventures and MagicBook put their
`BackButton` on a 2000x1125 canvas, so the same prefab draws its ends 2.5 times
thinner there relative to the screen. Draw every size with
`.art/tools/preview-nine-slice.py` before changing the multiplier.

## More hand-built buttons now carry the plank sprite directly (issue #125)

Beyond the two `Lobby/CreditsPanel`/`Lobby/JoinPanal` buttons above, these also
carry `WoodPlankButton.png` (guid `a838f9134d664488ba83f8a6b7a5bdb9`) directly
on their own `Image` rather than through `Button Variant`: `Deck.prefab` and
`CreateDeckButton.prefab` (the `ManageDeckScene` deck list, `deckPrefab` in
`DeckManagementController.cs`), `UI/Tutorial/PrimaryButton.prefab` and
`SecondaryButton.prefab`, `Scenes/ResultScene.unity` `GotoLobbyButton`, and
`Scenes/SpectatingScene.unity` `Time` and `Button` ("Back"). `UI/Debug/DebugItemButton Variant.prefab`
is an instance of `Brown-UI-Base` (not `Button Variant`), so it carries the
same look as `m_Modifications` overrides on the inherited `Image`/`Shadow`
(same target fileIDs `Button Variant.prefab` uses, since both instance
`Brown-UI-Base` directly) rather than on an `Image` of its own.

Secondary-style buttons that need to stay visually distinct from the plain
plank use a tint, not a different sprite: `SecondaryButton.prefab` tints the
same `WoodPlankButton.png` `#9A8B7C` (darker, desaturated) instead of white.
Check label contrast against the *tinted* sprite color, not the raw wood
color — `#3A2616` (the label color everywhere else) clears only 4.3:1 on
`#9A8B7C`, under the 4.5:1 minimum, so that one button uses black
(`text-dark`) instead.

### The plank multiplier must scale with the canvas, not stay 3 everywhere

`Button Variant.prefab`'s own override applies `m_PixelsPerUnitMultiplier: 3`
uniformly to every instance regardless of that instance's canvas — #118 never
special-cased this. That is only correct on an 800x450-reference canvas.
`Scenes/SpectatingScene.unity` runs a 1920x1080 canvas, and its two hand-built
buttons needed `1.25` (`3 * 800 / 1920`) instead, or the plank ends render
2.4 times thinner relative to the screen than everywhere else. When
hand-styling a button outside Login/Register/Lobby/ManageDeck/Tutorial (all
800x450), trace the button's actual `Canvas`/`CanvasScaler` ancestor before
picking the multiplier — do not assume 3.

### Not every square or icon-glyph button is in scope

A `Brown-UI-Base` (or `UI-Base`) instance with no text child at all — e.g.
`Scenes/GameScene.unity` `AdminPanalToggleButton`, a 100x100 admin/debug
toggle with a custom tint and no `Text (TMP)` under it — is not a "text-label
rectangular button" even though it isn't a Button Variant instance either;
skip it. Likewise a button whose label is a single glyph standing in for an
icon (`CoachCloseButton`'s `"X"`, 44x44) reads as an icon button, not a text
button — skip it even though it technically has a `TextMeshProUGUI` child.
`Field` and `ManaBar` GameObjects that carry a `Button` component in
`GameScene.unity`/`InteractiveTutorialScene.unity` are gameplay hit zones (an
invisible drag-drop rect with `m_Sprite: {fileID: 0}` and `m_Color.a: 0`, and
a full-width mana gauge bar with no text child) — the `Button` component on
them is not a rendered button at all.
