# Magic previews using actual in-game presentation

Runtime recordings are downloaded from the selected lobby's server-preview API.
The legacy 85 recordings are now Editor-only regression fixtures. See
`.agents/docs/server-magic-previews.md` for startup, caching and deployment ordering.

The deck hover and magic book explanation use the same MagicPreview prefab.
The book retains its icon; replay appears below the description. Situations play
sequentially without selectors or captions.

## Runtime architecture

Live DeltaFrameHandler and offline MagicPreview both call
PresentationFramePlayer.Apply(ObjectsInfo, events, world).
Dispatch order is create → events → projectiles → update. ObjectSpawner,
ProjectileSpawner, ServedObject listeners, GameEventHandler, native projectile
components, gauges, spawn/hit/death effects and popup-book presentation are shared.

Live calls retain ObjectContainer.Instance / Camera.main. Preview calls supply
PresentationWorld: separate origin, camera, registry, recorded parameters and
transient hierarchy. No login/server/full GameScene is required. Prefabs initialize
inactive before configuration/activation. Selection, match-name lookup and audio
are suppressed. Clearing removes owned coroutines/linked tweens/effects/death clones;
cleanup destruction cannot trigger additional death spawns.

This is actual prefab presentation driven by recorded server DTOs, not video frames
or a second sprite/height/beam renderer. Frames/combat animations run at 1x Unity
time. Long hitches restart the case instead of firing overdue attacks together.
Native cosmetic behaviors retain their authored clock settings.

## Recordings and extension

85 magics / 129 ordered cases are exported by MagicScenarioPreviewTest and
RemainingMagicPreviewTest. Real server spells, attacks, effects, physics and
lifecycle run in fixtures. Parameters are illustrative, not live database balance;
coverage is not exhaustive for every elemental permutation. One-cast quantities use
the explicit V001 snapshot (MiniRock 2, EmberSpirit 5, SeedSpirit 4,
ThunderBird/WaterSlime 3, VineSpirit/ZapMouse 2), not a universal three.

Automatic death-created ground fields and 35 generic combat_death showcases are
omitted in the recording fixture only. Remnants, energy absorption, movement trails,
Crater landing fields and self-destruct attacks remain. Area-attacking units face
several separated enemies; same-actor hit batches (or one chain ID) verify actual
multi-victim damage. The native renderer and production gameplay are unchanged.

Version 2 stores magic, frameDuration, ordered scenarios and real DTO frames.
Each scenario adds fixtureTargetIds (passive enemies only) and parameters (the
owner.key values read by the fixture). Ground fixtures use MiniRock, air fixtures
ThunderBird. Real initial ElectricSlime absorbers retain their prefab.
Version 1 remains compatible through its original passive-slime convention.

Export a scenario, bundle its TextAsset/meta, register magicId/recordingAsset and
ensure native runtime prefabs exist. Missing object/projectile dependencies fail
preflight, warn and hide the preview, without unrelated fallback art.
No per-magic style mapping is needed.

From game, run .\gradlew.bat test -PpreviewExport=true.
From client, run pwsh -File .agents/tools/expand-magic-previews.ps1 -CopyRecordings.
Review/apply any printed clip/meta patch, then run Unity tests. Existing registrations
produce no patch. No gameplay deployment or database migration is needed.

## UI and limitations

The existing forest ground material is rendered on an isolated XZ quad.
A 512×420 RenderTexture feeds the existing 220-unit hover / 420-unit book viewport.
Book zoom is 1.35× using native perspective framing; UI dimensions are unchanged.

Native HP/TTL bars reflect DTO gauges. Match HUD/cards/timer and ManaWell's HUD
counter are outside the object-frame dispatcher. The actual ManaWell prefab/aura
plays, not the old fabricated cyan gauge.

Four legacy Electric/Fire/Rock/WindSummon nest bodies lack specific runtime art and
share the explicit SeedNest alias in both paths. ElectricAbsorb uses ElectricShot.
MeteorShower's stale sprite reference is fixed. Inspired currently lacks a runtime
effect prefab: its state/motion replay without a separate buff decal. Knockback is
recorded motion. StormStag tiers use actual native components, not invented markers.

## Verification

PlayMode tests cover catalog preflight / first-frame initialization, explicit
fixture identity, perspective zoom, native beam/arm/projectile animation, DOT flashes,
one-cast quantities, multi-victim damage in both viewports, concurrent worlds,
cleanup, legacy/malformed input, every recorded object type
without match context, and same-DTO live/preview state plus actual hit dispatch.

Real-time captures are saved to Temp/SharedPreviewCaptures and Temp/AreaPreviewCaptures. Committed examples in
docs/pr-media/204 are isolated Unity preview-camera captures, not screenshots of
authenticated full-scene pointer flow. Server suite: 728 tests passed after recording
fixture IDs and parameter reads. Current Unity results are in the work plan/PR.
Authenticated deck/book interaction and a WebGL player build remain manual checks.
