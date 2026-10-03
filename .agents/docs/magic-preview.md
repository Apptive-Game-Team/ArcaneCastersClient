# Recorded magic preview checks

When changing hit flashes, route both HP decreases and `Status.Damaged` through
the same cadence gate. An HP-only test passed while status updates still flashed
SandStorm rapidly. Burn/SandStorm use a one-second minimum interval; HP always
updates per server tick. Ordinary hits must not inherit this DOT cooldown.

When extending registrations, verify each JSON/meta GUID is exactly 32 hex
characters, uniquely referenced by the shared preview prefab, and that every
created object, projectile and effect name has a corresponding style. Static
coverage cannot prove viewport framing: run MagicPreviewTests and inspect both
the book explanation and deck hover in Unity before calling the batch verified.

Never infer target masks or secondary effects from art/names. ThunderBird is
airborne but attacks ground only; current WaterExplosion provides Burn and an
upward impulse, and FireDrop is plain damage. Read actual server initializers.
See `docs/fire-shot-preview.md` for current coverage and pending families.
