# Shared attack sounds (#256)

One authoritative attack event selects one sound. Tower classification takes
priority, approved water/fire projectile launchers select their element clip,
and remaining attackers use database size tags.

| Shared attack profile | Approved source | Runtime types |
| --- | --- | --- |
| TowerAttack | towerattack.wav | GroundCannon, GroundTower, RockTurret, ElectricTower, FireworkTower, DragonTower, Towerback, TitanRemnant |
| WaterLaunch | aqua.wav | AquaArcher, WaterSlime, BubbleSpirit, CloudDragon, BubbleGenerator |
| FireLaunch | flame1.wav | FireChildSpirit, FireSpirit |
| SmallAttack | smallattack1.wav | ChickenCommando, EmberSpirit, FireSlime, FireTadpole, PveFireTadpole, LeafSlime, SeedSpirit, VineSpirit, ElectricSlime, LightningTadpole, PveLightningTadpole, ThunderBird, ThunderSpirit, ZapMouse, RockMage, RockSlime, MiniRock, WindSlime, WindSpirit, BombSprite |
| SwingAttack | swing1.wav | SeaSerpent, TreeGolem, EvilEnt, PveEvilEnt, PveVineWitch, StormRider, StormStag, RockGolem, WallGolem, MagmaSpirit |

FireLordSpirit and DimensionToad are producers rather than attackers. Support
objects remain without an attack override. SeaSerpent uses a direct beam rather
than a travelling projectile, so it follows the large attacker rule. TitanRemnant
is a stationary attack structure and uses the tower rule. CloudDragon uses its
regular water-shot sound; its secondary attack shares the same server notification.
Nature/lightning/rock/wind projectile attackers use size fallback until those
element sounds are approved.

Sources were approved by the user from `D:/SeongPill/Sound/Resources/AI/SFX`.
They were generated with the user's local Stable Audio workflow; generation
prompts/settings were not supplied. Originals remain unchanged. Runtime copies
under `Assets/Resources/Sound/Game/Attack` were processed with the project audio
skill's `postprocess_candidate.py`: stereo-to-mono mix, silence trimming with a
3 ms lead pad, 15 ms tail fade, -3 dBFS peak, PCM16 at 44.1 kHz, no hard duration
cap. Unity imports mono with preload enabled, and each attack slot starts at
volume 0.7 / pitch 1 before applying the player's game SFX volume.

| Clip | Runtime duration | Runtime peak |
| --- | ---: | ---: |
| aqua.wav | 0.597 s | -3 dBFS |
| flame1.wav | 0.813 s | -3 dBFS |
| smallattack1.wav | 0.187 s | -3 dBFS |
| swing1.wav | 0.614 s | -3 dBFS |
| towerattack.wav | 0.556 s | -3 dBFS |

Measurement and normalization do not establish perceived loudness; the five
shared profiles allow playtesting adjustments without touching every unit.

`ObjectSfxCatalog` also lists all runtime prefabs and supported server aliases.
Five shared attack profiles override only Attack on the existing lifecycle
profiles. UnitShot is silent on projectile spawn; independent spell releases and
explosion spawn sounds keep their existing profiles. No live blanket muter or
prefab-local legacy attack owner remains.
