# Game sound ownership

Before adding a gameplay sound, route it through `GameSfxPlayer` and assign its
event in `ObjectSfxCatalog`. Do not add an `AudioSource` or attack-sound behaviour
to runtime prefabs. The earlier mix of nested `HitSoundPlayer`, autoplay sources
and profile playback required muting every spawned source, hiding duplicate owners
and unused audio. Those owners were removed in #256. BGM and UI have separate
owners; preview-world source suppression remains deliberate.

`ServedObject.OnAttack` → `ServedObjectSfxController` → catalog entry's
`EffectiveAttack` → `GameSfxPlayer` is the attack path. One catalog entry selects
one attack slot. `attackProfile` overrides Attack only; other lifecycle events
still use `profile`. An absent override uses the lifecycle Attack; a disabled
override stays disabled. Intentional-silence rows suppress both references.

Attack classification uses server attack implementations and database size tags.
Do not infer size from sprite bounds or choose every attack by unit element:
FireSlime and FireTadpole attack in melee, whereas FireChildSpirit fires a shot.
Towers always select TowerAttack. Other approved water/fire projectile launches
select their element sound; remaining attackers select small or medium/large
size sounds. Unsupported projectile elements currently use the size fallback.
Non-attacking producers and support structures have no attack override.

Cosmetic effects bypass ObjectSpawner. OnDestroySpawner must explicitly play its
optional spawnSfxProfile after creating a live effect; its six Explode owners use
CosmeticImpact with the original light_explode.wav. BoulderStrikeImpact and
ShockOverloadSecondary resolve their catalog spawn slots explicitly. Never infer
audibility from an enabled catalog profile alone: #256 initially deleted the
cosmetic effect's autoplay source without connecting these direct Instantiate
paths, so ordinary catalog validation passed while the explosion was silent.
Validate actual dispatch through each creation path and count active clip-specific
voices, not AudioSource components retained by the pool. Previews and
SuppressPresentation destruction remain silent. Scene-unload/sync destruction
still follows the existing OnDestroySpawner presentation behavior.

Unit-produced projectile spawn profiles are silent: their attacker already owns
the release. Standalone ChainLightning, TideCall and WindBlade retain TransientShot
release playback; explosion objects retain TransientExplode. Inspect both attack
dispatch and projectile creation when adding a new sound, or one action can play
two release sounds. CloudDragon's notification does not distinguish its regular
water shot from secondary lightning, so its shared attack policy uses WaterLaunch.

The builder's `AttackGroups` is the default classification table. Update it and
the checked-in catalog together. Rebuilding preserves existing non-null attack
overrides and alias flags so Inspector tuning is not lost. Run
`Tools/Sound/Validate Object SFX Catalog` and `ObjectSfxTests` afterwards: they
check coverage, slot rules, removed owners, regeneration and serialized scripts.
Prefab AudioSources, including inherited ones, fail validation.

Controller destruction tests must enter Play Mode: an ordinary MonoBehaviour's
`OnDestroy` does not run for an Edit Mode-only instance. Checking the event after
Edit Mode `DestroyImmediate` falsely reports a subscription leak. Save batch test
results outside Unity's `Temp` directory; the Editor can remove them during exit.

When removing a nested sound prefab, remove its instance document, stripped
transform, parent's child reference and overrides that target deleted source
fileIDs. Deleting the script alone does not remove prefab ownership.

The offline tutorial has no server attack loop. It uses the central catalog and
player explicitly for the mock fire cast; its mock AquaArcher gets the same
controller as the live unit. Keep future mock attack events on this route.
