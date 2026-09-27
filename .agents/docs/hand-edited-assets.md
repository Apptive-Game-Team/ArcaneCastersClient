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

Size the atlas for the text, and turn on `m_IsMultiAtlasTexturesEnabled`. The
first version sampled at 90 pt with padding 9 on one 512x512 atlas, which holds
about 30 glyphs. Once it was full, TMP took every later glyph from the Jua
fallback, so one stat row showed "300" in Lilita One and "0.5" in Jua. The
assets now sample at 64 pt with padding 6 on 1024x1024 atlases with multi-atlas
on. When you change the sampling size, scale every `m_FaceInfo` metric by the
same ratio and set the materials' `_GradientScale` to padding + 1.

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
