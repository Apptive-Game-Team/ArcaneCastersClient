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
`UI-Base`. Since #118 those overrides set the wooden plank
sprite `Assets/Art/Images/UI/WoodPlankButton.png`, reset the tint to white
(otherwise the brown multiplies into the wood), set
`m_PixelsPerUnitMultiplier` to 3 and the `Shadow` to `y: -4`. Two lobby
buttons are hand-built with their own `Image` and carry the sprite directly:
`Lobby/CreditsPanel` `BackButton` and `Lobby/JoinPanal` `RegisterButton`.

### Since #125, `Brown-UI-Base` itself is the wooden panel — buttons stay independent through their own overrides

`Brown-UI-Base.prefab`'s own `m_Modifications` (targeting `UI-Base`'s `Image`
at `4587164778522837550` directly, one level down, no XOR needed) now set
`WoodFramePanel.png` (guid `01f67c633b374a9f8cab27b3e69c5ad7`), white tint,
`m_PixelsPerUnitMultiplier: 1`. This is safe to change in place, rather than
banned, only because every non-panel instance of `Brown-UI-Base` — `Button
Variant.prefab` and `UI/Debug/DebugItemButton Variant.prefab` — already
overrides `m_Sprite`, `m_Color.r/g/b` and `m_PixelsPerUnitMultiplier` on the
inherited `Image` itself (computed target `2990971800539104397`, the
two-levels-down XOR from `../../docs/hand-edited-assets.md`), so neither one
inherits anything from the new panel look. Before changing what
`Brown-UI-Base` itself draws, grep its guid across `Assets/` (prefabs and
scenes) and confirm every non-panel hit still carries its own `m_Sprite` +
full `m_Color` + `m_PixelsPerUnitMultiplier` override — a hit with only a
partial override (e.g. color but no sprite) would inherit the new panel
sprite silently.

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

## Wood panels, input fields, dropdowns, toggles and scrollbars (issue #125)

`WoodFramePanel.png` (guid `01f67c633b374a9f8cab27b3e69c5ad7`, border
L43 B47 R43 T49) and `WoodInputSlot.png` (guid
`5b9f556d0de74118bfddfcc737412099`, border L151 B65 R151 T66) round out the
wooden kit alongside `WoodPlankButton.png`. Every hand-built panel background
that used `Card.png` (guid `aa4924c3b99854f929c4321e387b3cf1`) as a large
container/dialog backdrop — `Lobby/Panal.prefab` `Panal`,
`Lobby/CreditsPanel.prefab` `CreditsPanel`, `Lobby/JoinPanal.prefab`
`JoinPanal`, `RewardUI.prefab` `Panal` — moved to the frame sprite, white
tint, multiplier 1 on their 800x450 canvas. Full-screen dim overlays
(`#0000007D`-ish, e.g. `SettingPage.prefab`'s modal backdrop) and card-shaped
prefabs stay untouched, per the standing rule.

### A 9-slice border sized for a big panel breaks on a small one

`WoodFramePanel.png`'s border (43-49 units) looks right on a 500x400 dialog at
multiplier 1, but a `TMP_Dropdown` `Template` (the open list, ~95x150) at the
same multiplier 1 renders as two facing corner blocks with no flat frame
between them — the borders don't overlap (43+43=86 < 95) but they dominate the
tiny rect so hard the shape reads as broken, not "framed". Draw the sprite at
its real target size with `.art/tools/preview-nine-slice.py`'s `slice_draw`
before trusting a multiplier that only looks right on a different-sized
instance; multiplier 2 was the smallest one that kept a visible parchment
interior for this 95-wide template. The wood kit does not have one correct
multiplier per canvas — it has one correct multiplier per (canvas, rect size)
pair, and the canvas-only rule from the plank-button section above is a
starting guess, not the final answer.

### `m_fontColor32`'s `rgba` is packed `ABGR`, not `ARGB`

`TextMeshProUGUI.m_fontColor32.rgba` looks like a standard packed color int,
but `#3A2616` opaque serializes as `4279641658` = `0xFF16263A` — alpha in the
top byte, then **blue**, then green, then red in the low byte. The formula is
`(a << 24) | (b << 16) | (g << 8) | r`, not the `(a << 24) | (r << 16) | (g <<
8) | b` an ARGB assumption would give. Compute this value whenever `m_fontColor`
changes on a `TextMeshProUGUI`/`TMP_Text` block — the two fields are
independently serialized and Unity does not derive one from the other on
load, and a plausible-looking wrong `rgba` produces a text tint that never
matches the flat color you actually asked for. Use `#3A2616` opaque
(`4279641658`) and `#6B5136` opaque (`4281749867`) as known-good values.

### Input-field placeholder color is `#6B5136` opaque

Every placeholder on `WoodInputSlot.png`'s `#E6CFA6` floor — legacy `Text` or
`TMP_Text`, in a prefab or a scene — uses `#6B5136` opaque
(`rgba: {r: 0.41960785, g: 0.31764707, b: 0.21176471, a: 1}`,
`m_fontColor32.rgba: 4281749867`), never the field's `#3A2616` value color at
reduced alpha. It clears 4.85:1 against `#E6CFA6`, comfortably over the 4.5:1
floor, while staying visibly lighter than the `#3A2616` real-text color so a
placeholder still reads as a placeholder. This is the one color value scene
work and prefab work must share exactly, since the same input fields are
sometimes built directly in a scene (`LoginScene.unity`, `RegisterScene.unity`)
and sometimes as a prefab (`JoinPanal.prefab`, `DeckOwnedCardControls.prefab`
`SearchInput`).

### `Brown-UI-Base`'s guid also turns up on things that are not `Brown-UI-Base` instances

Grepping `ab89aefebd40141228aabbe813d04aab` across `Assets/` also matches
every `Button Variant`/`DebugItemButton Variant` instance's `m_Modifications`,
because their overridden `Image` is a component *inside* `Brown-UI-Base` (see
"Overriding a component that lives two prefabs down" in
`../../docs/hand-edited-assets.md`) — the modification's `target` carries
`Brown-UI-Base`'s guid even though the instance's own `m_SourcePrefab` is
`Button Variant`. Filter on `m_SourcePrefab: {fileID: 100100000, guid:
ab89aefebd40141228aabbe813d04aab` to find the *direct* instances (real
panels, or non-text buttons like `Scenes/GameScene.unity`
`AdminPanalToggleButton`) instead.

### Dropdown items are literally `Toggle` components, but the dropdown section's rule wins

A `TMP_Dropdown`'s `Item` GameObject carries a `Toggle` component (script
guid `9085046f02f69544eb97fd06b6048fe2`, the same one a standalone checkbox
uses), so a guid grep for "every `Toggle` in the project" also finds dropdown
items. Style them from the dropdown they belong to, not from the generic
toggle rule: `Item Background` stays a plain warm highlight (it is a
hover/selection tint, not a checkbox box) while `Item Checkmark` and
`Item Label` take the same `#3A2616` treatment as everywhere else. A real
standalone toggle (e.g. `Lobby/Panal.prefab` `CoachHintToggle`) does take the
slot-sprite background.

### A 20-unit-wide scrollbar needs a much higher multiplier than the canvas rule gives

`WoodInputSlot.png`'s border (151 units horizontally) does not fit inside a
20-unit-wide scrollbar track or handle at the plank/slot multipliers used
elsewhere (6 on an 800x450 canvas) — the border alone would be ten times the
rect's width. Unity clamps a 9-slice's borders down when they would overlap,
so it will not crash or invert, but at multiplier 6 the clamp eats the entire
sprite and nothing textured survives. Multiplier 30 was the smallest value
that kept a visibly wood-grained sliver on a 20-unit track/handle; treat
"multiplier from the canvas" as the rule for panels and buttons, and
"multiplier from the rect, verified with `slice_draw`" as the rule for
anything scrollbar/handle-thin.

### Sound sliders

The three `Slider`s in `Lobby/Panal.prefab` (`GameSoundSlider`,
`BackgroundSoundSlider`, `UISoundSlider`) use the slot sprite at multiplier 15
for the 10-unit-tall `Background`, `Card.png` tinted `#C08C56` for `Fill`, and
the plank at multiplier 30 for the 20x20 `Handle`. Gameplay bars (HP, TTL,
mana) are `Slider`s too and stay as they are.

### A dark panel with light text stays dark

Some surfaces were dark on purpose and put white text on top, often from code:
`ManageDeckScene` `HaveCardPopup`, `DeckMagicPopup` and `DeckMagicDetail` are
navy `#1F2633` at alpha 0.87, and `HaveCardMagicPopup.CreateText` creates every
label with `Color.white`; `UI/Tutorial/TutorialSelectPanel.prefab` tints its
`Brown-UI-Base` the same navy under `#F4F1E8` text. Moving these to the
parchment panel made the text disappear, so they keep `Card.png` and their navy
tint (`TutorialSelectPanel` overrides `m_Sprite` back to `Card.png` and
`m_PixelsPerUnitMultiplier` back to 2, the old `UI-Base` value). Before
converting a panel, check the colour of every text over it, including text a
script creates, and change the text to `#3A2616` when it moves to parchment —
`AdventureStoryOverlayUI` `DialoguePanel` now uses `#3A2616` for the dialogue,
`#7A3A10` for the speaker name (6.6:1) and `#6B5136` for the hint.
`GameScene` `AdminPanalToggleButton` tints `Brown-UI-Base` without overriding
its sprite, so it also pins `Card.png` and multiplier 2 to keep its old look.
