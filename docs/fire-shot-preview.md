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
are no selection buttons. Captions are hidden; playback still advances through each situation.
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

Captions are hidden in both hosts; scenarios still cycle automatically.
Enemy target fixtures are displayed as MiniRock on the ground and ThunderBird
in the air. Bundled v2 recordings still contain passive ElectricSlime fixtures;
their initial height selects the visual only. Actual summoned units and ally
fixtures keep their own mappings and the server recordings remain unchanged.
HP decreases flash the target red per damage tick. Flash duration is capped at
0.12 seconds and shortened to 45% of the preceding damage interval so frequent
DOT ticks have an un-tinted gap instead of one continuous red tint.
Burn and SandStorm group damage flashes with a 1-second minimum interval;
every recorded damage tick still updates HP. Ordinary hits keep per-hit flashes.
Preview heights are 220 canvas units in the shared hover prefab and 420 in
the book explanation. The hover scales down only when needed to fit the canvas,
with placement clamped using its scaled bounds. Rendering uses a 512×420 target.
The book keeps this viewport size but uses 1.35× visual zoom with extra headroom
for flying units. Hover framing is unchanged. Sky-drop summons may enter from
above the viewport before landing; do not zoom out for their entire fall path.
Long labels may autosize within the hover width. Do not copy the button's text
margins: the preview caption has no button shadow and needs its full text area.

Validation before expansion: six Unity Play Mode tests passed, including the
one-second DOT flash regression. They cover mappings, complete cycles, stage
reuse, cleanup/reconfigure, legacy playback and malformed data handling.

## Additional batch (2026-10-04)

The same shared prefab now registers 21 magics / 30 sequential scenarios.

| Magic | Ordered situations |
| --- | --- |
| water_shot | impact/splash and Wet |
| lightning_shot | enemy Shock → allied lightning summon Overcharge/extra shots |
| wind_blade | three targets pierced with diminishing damage |
| rock_rolling | enemy collision and reflection |
| fire_drop, wind_drop | fall and direct damage |
| water_explosion | area damage, Burn and upward launch (current server behavior) |
| wind_explosion | area damage and knockback |
| rock_blast | small remnant consumed → medium remnant bursts and leaves small rubble |
| mini_rock_swarm | three summoned units attack a ground enemy |
| thunder_bird_swarm | ground dive attacks → death fall/field/ally energy absorption |
| water_slime_swarm | ground projectile attacks → water trail/Wet |

All three new swarms target ground enemies in production, even the flying
ThunderBird. Quantity is three. Only newly spawned swarm positions are staged
deterministically within the real +/-1 spawn range, preserving spawn height;
combat, movement, effects and death still use production components. The trail
scene places a passive victim on a real emitted WaterField to demonstrate Wet.
ElectricAbsorb uses the existing electric projectile art as a traveling marker.
Drop spells have no Burn/Knockback provider; their names are not effect contracts.

Server suite: 664 tests, zero failures. Exporter captures all 21 clips twice
identically. Static checks confirm all recorded object/projectile/effect names
have mappings and all 21 recording GUIDs resolve. Unity Play Mode tests and
screenshots for this new batch are pending: MCP at 127.0.0.1:8080 is unavailable
and no running Editor was detected. Previous screenshots do not validate this batch.

Still pending: other summons (e.g. AquaArcher, FireSpirit, RockMage, TreeGolem,
CloudDragon), buildings/towers/nests (e.g. RepairTotem, RockTurret, ManaWell),
and special spells (e.g. ChainLightning, SpiritBomb, VineWorld, MeteorShower,
TideCall). Cross-element combinations such as WaterField-to-ElectricField and
every possible conditional interaction are not covered by this batch.
