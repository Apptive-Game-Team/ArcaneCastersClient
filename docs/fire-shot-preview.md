# Fire Shot preview example

`fire_shot` is the internal name of the requested fireball example. The deck
management owned-card/deck-slot hover and selected magic book image use the same
`Assets/Prefabs/UI/FireShotPreview.prefab`. Other spells keep their existing UI.

## Data ownership

The client bundles `Assets/Resources/MagicPreviews/fire_shot.json`. Opening a
preview performs no network request, database query or live match simulation.
`FireShotPreviewTest` in the game repository exports real FireShotMagic,
FireShotPrefabInitializer, Shot and physics output against stationary test targets.
The recording contains 60 frames at 20 Hz and loops every three seconds.

Fixture speed 8, radius 0.5 and damage 100 originate from database V001. They
illustrate the mechanic, not current live balance. One target and its nearby
neighbour receive splash damage; the distant target does not. An explicit impact
marker is emitted only when the direct target actually receives damage.

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

## Validation (2026-10-02)

- Game suite: 663 tests passed, including direct/adjacent/distant hit assertions.
- Client C# compilation: passed (six existing warnings).
- Unity 2022.3.34f1 isolated Play Mode harness using the production component,
  prefab, sprites, JSON and serialization assembly: rendered flight/impact;
  asserted exactly one parsed impact; exercised looping, hide/release, reopen and
  square framing. This caught JsonUtility constructing missing impact objects;
  the component now uses the shared JsonCodec.
- Review images under `docs/pr-media/204` are actual preview RenderTexture
  captures in that isolated harness, **not screenshots of the complete scenes**.
- Full scene interaction/layout and a WebGL player build still require manual
  verification in the main project. The existing running Editor was left alone.

Manual check: hover a fire_shot card in both owned cards and equipped deck;
leave/re-enter quickly; select fire_shot then another spell in the book; test
screen-edge placement and change scenes while the preview is active.
