# Localization Tables

Everything under `Assets/Localization` is Unity Localization data serialized as
YAML. The Unity Editor cannot run in this environment, so these files are edited
by hand. Read this before touching them.

## One magic, three different spellings of its name

`Assets/Scripts/Data/Magic/LocalCombinedMagicData.cs` takes the magic name that
the game server sends (snake_case, for example `evil_ent`) and derives three
separate names from it with `Data.Util.StringUtils`:

| Field | Transform | Example | Used for |
| --- | --- | --- | --- |
| `localizationKey` | `ToCamelCase` | `evilEnt` | key in the `Magic` string table (display name) |
| `textLocalizationKey` | `ToSnakeCase` | `evil_ent` | key in the `MagicBook` string table (description) |
| `resourceName` | `ToPascalCase` | `EvilEnt` | prefab and sprite name under `Assets/Resources` |

The two localization keys are the trap: the same unit is `evilEnt` in one table
and `evil_ent` in the other. Adding only one casing looks correct in the file you
edited and still leaves the other screen broken.

Both keys are read in two places, so check both:

- `Assets/Scripts/MagicBookScene/MagicInfo.cs` — magic book name and description.
- `Assets/Scripts/DeckScene/DeckInfoMagicPopup.cs` — deck popup name and description.

Each key is looked up in exactly one table with no fallback chain, so a missing
row shows the raw key on screen. Neither table fails the build; the only way to
notice is to look at the screen or to check the tables.

Not every magic needs a `MagicBook` row — the `Magic` table is larger than the
`MagicBook` table on purpose.

## Which table holds what

| Table | Keys | Read by |
| --- | --- | --- |
| `Magic` | camelCase magic name | 마법 이름. 도감, 덱 화면, 손패 카드 이름 전부 |
| `MagicBook` | snake_case magic name | 마법 설명. 도감과 덱 팝업 |
| `Element` | `None`, `Fire`, `Water`, `Nature`, `Lightning`, `Rock`, `Wind` | 원소 이름. 도감 필터 |

카드 한 장이 마법 하나가 되면서 `Card` 표는 없어졌다. 카드 이름은 이제 `Magic`
표에서 읽으므로, 서버가 새 마법을 내보내면 `Magic` 표에 키를 넣지 않는 한 손패와
덱 화면에 raw key 가 그대로 보인다. 마법을 추가할 때 `Magic` 표는 선택이 아니다.

## Adding an entry

A string table is three files: `<Table> Shared Data.asset` maps key to `m_Id`,
and `<Table>_ko-KR.asset` and `<Table>_en.asset` map that same `m_Id` to the
localized string. Add the entry to all three, with the same `m_Id`, or the row
exists with no text.

`m_Id` is normally allocated by a `DistributedUIDGenerator` in the Editor. When
hand-editing, pick a value that is unused in that table and shaped like its
neighbours — continue the table's existing run rather than inventing a new
numbering scheme. Entries are stored in ascending `m_Id` order; appending the
new largest id keeps the file sorted and the diff to one hunk.

After editing, verify that the shared-data key count equals each locale file's
entry count, and that the new id occurs exactly once in each of the three files
and nowhere else in that table.

## YAML string encoding

- Korean is stored as `\uXXXX` escapes inside a double-quoted scalar, uppercase
  hex. English is stored as an unquoted plain scalar.
- Long values wrap. The rule that reproduces the existing files: write the value
  after `    m_Localized: `, and at each space, if the current column is already
  at or past 80, break the line instead and indent the continuation by 6 spaces.
  This counts the escaped output, so a Korean line breaks after far fewer words
  than an English one.
- Files are LF only (`.gitattributes` is `* text=auto eol=lf`).
- `Onboarding/Onboarding_ko-KR.asset` and `Onboarding_en.asset` do not follow
  the two rules above: both store the raw text, Hangul included, in a
  double-quoted scalar on one line. Keep that shape when editing them; rewriting
  them to the escaped, wrapped form turns a one-line text change into a diff of
  every row. Check the rule against the file before applying it — wrapping
  reproduces every Editor-written row of `tutorial_ko-KR.asset` and
  `tutorial_en.asset`, and the rows it did not reproduce were hand-written on
  one line.

Do not copy the neighbouring rows blindly. The tables already contain damage
that an Editor pass would fix but review will not:

- `Magic Shared Data.asset` has a dead key `'emberSpiritSwarm` with a leading
  apostrophe alongside the real `emberSpiritSwarm`. It can never be looked up.
- The Korean value for `treeGolem` ends with a stray `\n`.
- `chicken_commando` stores raw Hangul instead of `\uXXXX` escapes, so the
  encoding is not uniform.
- `Magic Shared Data.asset` has a second dead key of the same shape,
  `'waterExplosion` with a leading apostrophe, alongside the real
  `waterExplosion`. Like `'emberSpiritSwarm` it can never be looked up.
- `Localization-Assets-Shared.asset` gives `Magic Shared Data.asset` the address
  `Assets/Magic Shared Data.asset`, missing the `Localization/` segment that
  every neighbouring entry has.

## Adding a whole table

A new table is four assets and three Addressables entries. Miss the Addressables
entries and the table loads in the Editor and is absent from a WebGL build,
which is the hardest version of this failure to notice.

1. `<Table> Shared Data.asset` (script guid `5b11a58205ec3474ca216360e9fa74a8`),
   `<Table>_en.asset` and `<Table>_ko-KR.asset` (script guid
   `e9620f8c34305754d8cc9a7e49e852d9`), and the collection `<Table>.asset`
   (script guid `5be51871efa6c3e4eae1703925c8f5ac`). The collection lists ko-KR
   before en. Each needs a `.meta` with a fresh guid.
2. `m_TableCollectionNameGuidString` in the shared data must equal the shared
   data asset's own guid, and both locale files point at it through
   `m_SharedData`.
3. Register three of them in
   `Assets/AddressableAssetsData/AssetGroups/`: the shared data in
   `Localization-Assets-Shared.asset` addressed by full path, and each locale
   table in its `Localization-String-Tables-<locale>.asset` addressed by file
   name with the `Locale-<code>` and `Preload` labels.

`Assets/Localization/Element*.asset` was written this way and has not been
opened in the Editor.

## What still needs the Editor

Hand-edited tables are not loaded until someone opens the project. An Editor
pass should confirm the Localization Tables window shows the new row in both
locales, that the magic book and deck popup render it, and that saving the
tables produces no diff — a diff there means the hand-written YAML did not match
what Unity emits.
