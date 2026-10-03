# Sequential magic preview replay

`fire_shot` is the internal name of the requested fireball example. The deck
management owned-card/deck-slot hover and magic book explanation area use the same
`Assets/Prefabs/UI/MagicPreview.prefab`. Other spells keep their existing UI
until a clip is registered.
The book keeps the selected spell's icon and places the replay after its text in
the scrollable explanation. The Fire Shot hover opens immediately and is
positioned under the root canvas: its original deck scroll parent is only
420×239 units, smaller than the preview popup, and the shared hover transition
otherwise waits one second before showing anything.

## Data ownership and extension

The client bundles registered recordings under `Assets/Resources/MagicPreviews/`. Opening a
preview performs no network request, database query or live match simulation.
`MagicScenarioPreviewTest` in the game repository exports nine representative
magics and fourteen ordered situations using production components at 20 Hz.
The client advances after each situation's duration and loops the whole sequence.

The original `FireShotPreviewTest` remains a version-1 compatibility example
(60 frames, three seconds, V001 illustrative parameters). The bundled Fire Shot
now uses version 2 with a Burn scenario followed by a Wet-interaction scenario.
All representative parameters are fixed test values, not current live balance.

The prefab's `clips` array matches a magic's `serverName` to its bundled
recording and impact art. Shared visual mappings are keyed by the recorded
object `type` (for example `Player`, `ElectricSlime`, `FireShot`). To add another
spell, export a version-2 recording from a server fixture, bundle it as a
`TextAsset`, and add one clip plus a sprite/height/sorting mapping for every
created object, projection and effect type. Existing mappings can be shared.
No new replay class or hover/book wiring is needed. Unknown types warn and are skipped
rather than showing an unrelated fallback creature. A clip is offered only when
its magic ID and recording asset are configured. The camera uses the forest
battlefield's existing `ground.mat` on a separate XZ quad; the shared material
is not changed.

Sprites are visual proxies using existing game art, not instantiated gameplay
prefabs. Movement, creation, damage and destruction come from recorded DTOs;
attack frames, projection sprites, HP bars and effect markers are reproduced;
full gameplay particles, audio and pixel-exact motion are not. Do not
present this example as a pixel-exact recording of a live match.

## Regenerate

From the sibling game repository, run:

```powershell
.\gradlew.bat test --tests com.wordonline.server.game.preview.MagicScenarioPreviewTest -PpreviewExport=true
Get-ChildItem build/previews/scenarios -Filter '*.json' | Copy-Item -Destination ../client/Assets/Resources/MagicPreviews
```

Commit the exporter and bundled JSON together when the fixture or gameplay
behaviour changes. No database migration or server deployment is required to
display the bundled example. Remove the serialized `previewPrefab` references in
both scenes to disable it.

## Validation (2026-10-02 to 2026-10-03)

- Game suite: 663 tests passed, including direct/adjacent/distant hit assertions.
- Client C# compilation: passed (six existing warnings).
- Unity 2022.3.34f1 isolated Play Mode harness using the production component,
  prefab, sprites, JSON and serialization assembly: rendered flight/impact;
  asserted exactly one parsed impact; exercised looping, hide/release, reopen and
  square framing. This caught JsonUtility constructing missing impact objects;
  the component now uses the shared JsonCodec.
- Review images under `docs/pr-media/204` are actual preview RenderTexture
  captures in that isolated harness, **not screenshots of the complete scenes**.
- Unity MCP deck scene Play Mode displayed the Fire Shot replay after explicitly
  setting the RawImage UV rect. This used synthetic card data because entering
  the scene without login returned API 401. Actual owned-card pointer flow,
  magic-book screen and a WebGL player build still require manual verification.
- Unity MCP Play Mode then verified the generic clip mapping and forest ground
  in both deck hover and book explanation. The book preview is inside its
  description scroll, so it is fully visible after scrolling down. The
  Editor-resolved `MagicPreview.prefab` has one clip and the `ground` material;
  `Supports(fire_shot)` is true and an unregistered magic is false. No preview
  compile errors or warnings appeared. Both scenes used synthetic spell data,
  not an authenticated owned-card interaction.

Manual check: hover a fire_shot card in both owned cards and equipped deck;
leave/re-enter quickly; select fire_shot then another spell in the book; test
screen-edge placement and change scenes while the preview is active.

## Sequential representatives (2026-10-03)

Both hosts automatically play ordered situations, then wrap to the first. There
are no selection buttons. A caption shows the situation and sequence index.
Reopening/reconfiguring starts at zero. Transitions remove all dynamic objects,
HP bars, effects and projections but reuse camera, RenderTexture and grass.

| Feature | Magic | Ordered situations |
| --- | --- | --- |
| Ground summon | rock_golem | attack/knockback → death/remnant |
| Air summon | wind_spirit | charge/self-destruct and splash |
| Both targets | towerback | ground melee → air projectile/splash |
| Support summon | healing_totem | injured ally heals |
| Projectile | fire_shot | impact/Burn/DOT → Wet interaction |
| Area burst | lightning_explosion | nearby victims/Shock, distant target unharmed |
| Persistent area | sand_storm | ongoing damage |
| Rage | frenzy_totem | baseline → neutral/faster attacks/restoration |
| Conversion | earth_call | small remnant/MiniRock attack → medium remnant/RockGolem attack |

Nine magics, fourteen scenarios. All timings and HP values are illustrative
fixtures, not the live stats shown in the surrounding UI. Other spells and
conditional combinations are not yet covered. FireDrop does not make a field,
so SandStorm is the persistent-area example. Frenzy is not a simple ally buff:
it changes ownership to neutral temporarily.

Version 2 stores an ordered `scenarios` array with `id`, `labelKo`, `labelEn`,
`duration` and `frames`. Version 1 normalizes to one scenario and uses the same
playback path. Shared object/projectile/effect mappings live in the prefab;
do not instantiate server-managed gameplay prefabs or reimplement effect rules.
Server gauges/master/effect updates drive HP changes, team colours and markers.
Unknown mappings warn and fail the representative coverage test.

Captions use the game's existing outlined LilitaOne/Jua fallback styling.
Long labels may autosize within the hover width. Do not copy the button's text
margins: the preview caption has no button shadow and needs its full text area.

Validation: game suite 664 passed; exporter captures twice identically. Two
Unity Play Mode tests cover all representative mappings, complete cycles,
stage reuse, cleanup/reconfigure, legacy playback and malformed data handling.
