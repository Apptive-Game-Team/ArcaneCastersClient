# Magic preview replay (Fire Shot example)

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

The client bundles `Assets/Resources/MagicPreviews/fire_shot.json`. Opening a
preview performs no network request, database query or live match simulation.
`FireShotPreviewTest` in the game repository exports real FireShotMagic,
FireShotPrefabInitializer, Shot and physics output against stationary test targets.
The recording contains 60 frames at 20 Hz and loops every three seconds.

Fixture speed 8, radius 0.5 and damage 100 originate from database V001. They
illustrate the mechanic, not current live balance. One target and its nearby
neighbour receive splash damage; the distant target does not. An explicit impact
marker is emitted only when the direct target actually receives damage.

The prefab's `clips` array matches a magic's `serverName` to its bundled
recording, impact art, and a list of visual mappings keyed by the recorded
object `type` (for example `Player`, `ElectricSlime`, `FireShot`). To add another
spell, export a version-1 recording from a server fixture, bundle it as a
`TextAsset`, and add one clip plus a sprite/height/sorting mapping for every
created object type. No new replay class or hover/book wiring is needed for
basic create/move/damage/destroy recordings. Unknown types warn and are skipped
rather than showing an unrelated fallback creature. A clip is offered only when
its magic ID and recording asset are configured. The camera uses the forest
battlefield's existing `ground.mat` on a separate XZ quad; the shared material
is not changed.

Sprites are visual proxies using existing game art, not instantiated gameplay
prefabs. Movement, creation, damage and destruction come from recorded DTOs;
animation, particles, audio and ongoing burn effects are not reproduced. Do not
present this example as a pixel-exact recording of a live match.

## Regenerate

From the sibling game repository, run:

```powershell
.\gradlew.bat test --tests com.wordonline.server.game.preview.FireShotPreviewTest -PpreviewExport=true --rerun-tasks
Copy-Item build/previews/fire_shot.json ../client/Assets/Resources/MagicPreviews/fire_shot.json
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
