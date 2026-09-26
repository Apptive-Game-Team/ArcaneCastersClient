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

## A RectTransform's box is bigger than what renders on screen

Placing the Arcane Casters logo at the top of `LobbyScene.unity` (issue #114)
looked safe from the fact sheet handed to the implementing agent: it listed
`QueueLengthText`'s rect and the six buttons, all clear of the top edge.
`UserNameText` — also a direct child of the Canvas — was left off that list
because nobody had read its `RectTransform`. Its box is
`m_AnchorMin/Max: {x: 0.5, y: 1}`, `m_SizeDelta: {x: 500, y: 120}`: anchored to
the top edge, 120 units tall, centered across the middle 500 of the 800-wide
reference canvas. On the 800x450 canvas that is the entire top-center band
from y=105 to y=225 — exactly where a naive "put it at the top, centered"
placement lands, and exactly the one rect nobody had measured.

The box is that generous because of auto-sizing and word-wrap for long
names; the actual rendered line is far smaller and sits vertically centered
in it. But a hand-written scene edit can only check the declared rect, not
the rendered glyph, so the safe rule is to treat the full `m_SizeDelta` as
occupied.

Before adding any new UI element by hand, list every *direct sibling*
`RectTransform` under the same parent — not just the ones a prior note or
issue happened to call out — and compute each one's occupied rectangle
(`m_AnchorMin/Max`, `m_Pivot`, `m_AnchoredPosition`, `m_SizeDelta`) on the
`CanvasScaler` reference resolution before picking a position. A list of
siblings that omits one is worse than no list, because it reads as
permission to skip the check.
