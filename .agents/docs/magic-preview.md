# Shared in-game magic presentation

Runtime recordings now come from the server. Read `server-magic-previews.md` before
changing the source/caches. Legacy offline fixtures are Editor-only test assets.

Both DeltaFrameHandler and MagicPreview call PresentationFramePlayer with real
ObjectsInfo/GameEvent DTOs in create → events → projectiles → update order.
Do not add a parallel sprite renderer, synthetic beam, gauge, effect marker or
manual tween clock. Native prefab components own visual behavior. The preview
adapts the recording envelope and explicit fixture targets only.

Instantiate under PresentationWorld's inactive initialization parent. Configure
world/master/ID/gizmos, suppress name/input/audio, activate, then bind listeners.
Disabling PlayerNameSetter alone leaves its placeholder text: hide the label too.
Do not render editor debug gizmo lines, but retain geometry for real aura listeners.

Never replace ObjectContainer.Instance or Camera.main. Resolve references/camera
measurements through the instance world. Every effect, afterimage, death clone and
fragment must be owned by that world. Clear marks destroy spawners before
deactivation; deferred destruction must not produce cleanup-only effects. Status
effects also need inactive creation. Keep linked tweens; never global KillAll or
ManualUpdate. Native cosmetic behaviors retain their authored clock settings.

Only fixtureTargetIds designate passive enemies. Some initial ElectricSlime
objects are real death-energy absorbers: replacing all initial slimes breaks that
mechanic. Scenario-local parameters capture server reads for AuraRadiusScaler;
never query/mutate the logged-in parameter cache in previews. Values are illustrative.

HP decreases and Status.Damaged share the flash gate. Burn/SandStorm flashes have
a one-second minimum while every tick updates HP; ordinary hits remain separate.
Projection position endpoints use flat x/y/z. Use native DTO converters, beam/arm
components and reference tracking; inspect mid-animation captures.

Coverage: 85 recordings / 129 cases. Regenerate with
.agents/tools/expand-magic-previews.ps1 -CopyRecordings after server export.
The script updates Editor fixtures and reports missing metadata; it never registers
recordings in the runtime prefab or Resources.
Run MagicPreviewTests to verify both viewports and all native object initializations.
Advancing elapsed instantly does not advance coroutines/tweens; animated tests must
wait on Unity time. Initial-frame/cycle tests alone cannot prove animated parity.
Reflection tests span Assembly-CSharp, GameContracts and Serialization assemblies.

MeteorShower's deleted sprite GUID is repaired in its runtime prefab: verify resolved
sprites, not valid YAML alone. Four legacy summon nests share a SeedNest runtime
alias; ElectricAbsorb shares ElectricShot. Inspired has no runtime effect prefab:
its authoritative state/motion replay, without an invented RallyingTotem marker.
Knockback is position motion, not a decal. StormStag tier listeners remain native.
See docs/fire-shot-preview.md for architecture, coverage and limitations.

Before changing preview scenarios, use the server PreviewSummonQuantities snapshot
for actual one-cast counts. Neither a generic quantity=3 nor "all non-swarms=1" is
correct (VineSpirit/ZapMouse are two; EmberSpirit is five). Count initial bodies
separately from allies, evolution inputs and delayed children. Do not infer the
parameter owner from prefab aliases. DeathField.spawn is omitted only in the server
fixture; preserve water/leaf trails, Crater landing fields, remnants and energy.
AOE fixtures require multiple same-attack victims, not just two final HP decreases.
Inspect animated impact captures at both hover/book sizes: trigger-only fixture
colliders don't extend body-edge CombatRange and close sprites can obscure hits.
