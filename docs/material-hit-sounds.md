# Target material hit sounds (#348)

Live `HitEvent` dispatches the target controller's `EffectiveHit` through
`GameSfxPlayer`. It does not require the attacker or a visual hit component to
exist. Dispatch precedes visual early returns, including charged StormStag hits.
Neither HP decreases nor projectile destruction own this contact sound.

`hitProfile` overrides only Hit, parallel to `attackProfile`. Absent overrides
fall back to lifecycle Hit; disabled overrides stay disabled; intentional silence
suppresses all slots. Builder regeneration preserves non-null overrides and slot
tuning. New material profiles initialize their approved clip at volume 0.55 and
pitch 1; the existing attacks start at 0.7. The controller coalesces hits on the
same target for 0.2 seconds, with the existing four-voice HitHeal category cap.

## Classification

`ObjectSfxCatalogBuilder.HitGroups` is the complete default mapping. There are
82 target rows, including the four legacy summoning aliases:

| Material | Rows | Examples and exceptions |
| --- | ---: | --- |
| WoodHit | 25 | Ents, wood/plant bodies, vines/nests, wooden totems; FireRune's disc, ManaWell, ShockTrap; four summoning aliases use SeedNest's body |
| RockHit | 26 | Rock creatures, stone buildings/runes, WindTotem; CraterCarrier/MagmaSpirit armor, DimensionToad shell, RockMage, Towerback |
| CreatureHit | 31 | Remaining creatures including Player, water/air/fire/lightning bodies; LeafSlime, SeedSpirit and PveVineWitch remain creatures |

PveWaterSlimeNest is ice and stays unassigned. Fields, projectiles, explosions,
rubble and river scenery do not receive target-contact profiles. Classification
follows inspected sprite material rather than element. Future Player appearance
changes do not dynamically change its material.

Previews remain silent at both the handler and controller boundaries. Server
HitEvents are the authority: HP-only ongoing damage is silent, while any damage
for which the server actually emits a HitEvent follows the same contact policy.

## Approved sources and runtime processing

The user supplied and selected `creature_onhit_1.wav`, `wood_onhit_1.wav` and
`rock_onhit_1.wav` on 2026-10-19, following the Stable Audio prompt discussion.
Actual generation parameters and exact provider version were not supplied. No
additional generation or listening-based quality claim is made by this import.

Original Downloads files are unchanged. Runtime copies are under
`Assets/Resources/Sound/Game/Hit/` with the same filenames. All were stereo PCM16,
44.1 kHz, 1 second. `postprocess_candidate.py --peak -3` downmixed them to mono,
trimmed relative -60 dB silence with a 3 ms lead pad, applied a 15 ms tail fade,
and normalized to -3 dBFS PCM16 at 44.1 kHz. No hard duration cap was applied.
Runtime lengths are approximately 0.348 / 0.362 / 0.514 seconds, respectively.
Imports force mono, preload, and use no background loading or normalization.

| Material | Original SHA-256 | Runtime SHA-256 |
| --- | --- | --- |
| creature | `20148d08efcdd287b37135ff92e85e6a4e261334df92a809ebacf255f7994f0e` | `5795441ca67bab946a37be1f245ffdb810fa17dc203ae10fbcb47c11c9ebbe5a` |
| wood | `2209a0956543cca2eed10f2cf0cb925911584fa0e80c4e224eb63839e4be4d25` | `3fa76eb25727877091a607db58f11e9a5f230afa60fe1202db7a8e9fefc53fa7` |
| rock | `8bd20ec38fc45606f49a7a929280f7727dbd93f02a10d83f4f3c436ebadbca82` | `bc16197cc99bfc94d124efdf0a623a211afb3399990337609206cddd4ca4c569` |

## Verification

`MaterialHitSfxTests` checks material exceptions, eligible target coverage,
override semantics, regeneration, imports, live clip-specific dispatch without
an actor or visual component, HP-only silence, cooldown, preview silence and
charged StormStag dispatch. Run it with `ObjectSfxTests`, `ExplosionSfxTests` and
`ObjectSfxCatalogValidator` after changing target sound ownership.

Unity 2022.3.34f1 compilation and catalog generation/validation passed on
2026-10-19. All 81 focused tests passed (33 material hits, 31 existing object SFX,
12 explosion SFX, 5 event contract tests), including real Play Mode transitions.
Static comparison preserves all existing lifecycle/attack/silence/alias mappings.
In-game listening and WebGL playback remain unverified.
