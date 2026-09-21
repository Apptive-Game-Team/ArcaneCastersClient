# Scene Space and Animation

How the game scene is arranged, and the two mistakes that arrangement invites.
Read this before writing gameplay code that positions anything in the world or
animates a `ServedObject`.

## The camera is tilted, and sprites are billboarded to it

`Assets/Scenes/GameScene.unity` holds a **perspective** camera at `(9, 21, -16)`,
rotated 45° about X, field of view 25. The ground is the world XZ plane and world
`+Y` is height, matching the server's `Vector3`. `PositionUpdater` assigns
`UpdatedObjectDto.position` straight to `transform.position` — there is no axis
swap anywhere in the client.

Sprites are then **billboarded to that tilted camera**. A sprite's own up axis is
the camera's up axis, not world up.

### The two players face each other along X

The field spans world `X ∈ [0, 18]` and `Z ∈ [0, 10]` — those are the bounds
`SkillIndicatorShapeRenderer` clips its shapes to. `InteractiveTutorialScene.unity`
places `LeftPlayer` at `x = 1` and `RightPlayer` at `x = 17`, so **`LeftPlayer`
faces `+X` and `RightPlayer` faces `-X`**, and `Z` is the width of the field, not
a heading. `SceneContext.Me` is the string that says which side you are.

`MagicIndicatorResolver.GetForwardDirection()` is that convention written down;
use it rather than hard-coding `Vector3.right`. Code that forgets the right side
looks correct in every left-side test and points backwards in half of all matches.

### Offsets measured off a sprite use screen-up, never `Vector3.up`

A height read off a sprite — a shoulder, a muzzle, an anchor point — is a
distance along the camera's up vector. Applying it along `Vector3.up` raises the
point by only its cosine on screen, about 71% at this tilt, and spends the rest
moving the point toward the camera in depth. The error also changes with the
direction the object faces, so it reads as an offset that drifts.

Use the existing helpers rather than reaching for `Vector3.up`:

- `ProjectileUtil.GetScreenUp()` — screen-up in world space.
- `ServedObject.GetAnchorUpDirection()` — the same thing, for anchors.
- `ServedObject.GetEdgeWorldPositionTowards(from, edgeBias)` — a point on a
  sprite facing something else. Pass `0f` for the sprite centre. Prefer this over
  a hand-tuned height whenever the target's size varies, so the same code aims at
  the body of a slime and of a golem.

`Vector3.up` is still correct for anything measured in world terms, such as a
server-sent height or a physics jump. The rule is about distances read off a
billboarded sprite.

### A served object's position is its feet, not its body

Sprite pivots in this project sit at the bottom edge — `PlayerCharacterBase.png`
carries `spritePivot: {x: 0.3751, y: 0.0072}` — and `PositionUpdater` assigns the
server position straight to `transform.position`. So a `ServedObject`'s
`transform.position` is the point where it stands, and its 2.2-unit-tall sprite
rises from there. `ObjectContainer.FindById(...).transform.position`, which is
what `ProjectileUtil.GetPosition` returns for a `ReferenceProjectileTarget`, is
that same ground point.

Anything drawn between two objects has to lift both ends before it aims, or it
is drawn along the floor and buried under the ground art and the units' feet.
`SpiritBombBeamProjectile` shipped without that step and the beam was hard to
see at all; `StretchProjectile.Aim` does it and says so in a comment.

Read the height off the sprite rather than hard-coding one, so the same code hits
a slime and a golem in the body:

- `ServedObject.GetEdgeWorldPositionTowards(from, 0f)` — the sprite's centre.
- The same call with a point one unit along `ProjectileUtil.GetScreenUp()` above
  that centre, and a bias between 0 and 1, walks from the centre to the top edge.
  The method reads the direction in screen space and lays the distance along the
  renderer's own axes, which is why the result lands on the sprite.

### Distance between two points uses the camera plane, not the world

Anything drawn as a sprite spanning two world points — a beam, a stretching arm,
a tether — lies in the camera plane. `Vector3.Distance` is the wrong length for
it: a delta along world `+Z` is foreshortened while a delta along world `+X` is
not, so reach appears to change with facing, and the perspective camera adds a
depth error on top.

Use `ProjectileUtil.GetCameraPlaneLength(start, end)`. It round-trips the end
point through screen space at the start point's depth, which is exact for both
projection and perspective, and is consistent with `ProjectileUtil.GetRotation`
by construction because both consume the same two `WorldToScreenPoint` results.

### A beam drawn from a runtime sprite has to ask for FullRect

The sprite way to draw something spanning two points is the one
`StretchProjectile` and `SpiritBombBeamProjectile` use: a base piece, a middle
`SpriteRenderer` with `drawMode = SpriteDrawMode.Tiled`, and a tip piece parented
to a root placed at the start point and rotated by `ProjectileUtil.GetRotation`.
`size.x` on the middle piece is the length from `GetCameraPlaneLength` and
`size.y` is the thickness, so the pattern repeats instead of smearing as the
length changes. The middle piece must stay at `localScale` 1, because `size` is
applied before scale.

Two traps appear when the `Sprite` is built at runtime rather than taken from the
import, which is what a projectile with no prefab has to do here.
`ProjectileSpawner.SpawnSpiritBombBeam` assembles its object in code, and the
`Sprite` an imported texture already carries is fixed at `pixelsPerUnit` 100,
which the second trap below rules out. The pixels themselves are still imported
assets — `Game/shoot/spirit_bomb_beam_segment` and `..._cap` — and only
`Sprite.Create` runs at runtime.

- `Sprite.Create` defaults to `SpriteMeshType.Tight`, which trims the transparent
  border away, and a trimmed mesh makes `SpriteRenderer.size` do nothing. Pass
  `SpriteMeshType.FullRect` explicitly. Nothing throws; the piece simply keeps its
  natural size and the beam never reaches its target. An imported sprite can hide
  this — `evil_ent_arm_segment.png` is imported with `spriteMeshType: 0` and still
  tiles, because it is an opaque rectangle whose tight mesh is already the full
  rect.
- `pixelsPerUnit` fixes the tile's world size, and the tiles are laid in both axes.
  If the thickness does not equal the sprite's natural height, the row is clipped
  or repeated. Derive `pixelsPerUnit` from the thickness — `textureHeight /
  thickness` — so exactly one row is laid; the tile's length then scales with the
  thickness too, which is what a beam wants. That makes the sprite depend on the
  thickness, so hold the `Texture2D` in a `static` field and create one `Sprite`
  per instance. `OnDestroy` destroys that sprite and nothing else: the texture is
  a `Resources` asset shared by every beam, and destroying it takes the art away
  from the next cast.

A `SpriteRenderer` created in code already carries the default sprite material, so
there is no reason to build one from `Shader.Find("Sprites/Default")`; the
built-in default cannot be stripped from a WebGL build, and a `Shader.Find` result
can be.

### A ring sprite and a ground circle project to the same ellipse

"Billboarded" above means a sprite never turns with its object — facing is
`SpriteRenderer.flipX`, not rotation. It does not mean the sprite plane is tilted
to meet the camera. A `ServedObject`'s transform carries no rotation at all:
`ObjectSpawner` instantiates with `prefab.transform.rotation`, no runtime prefab
under `Assets/Resources/Prefabs` holds a rotation (`grep -rn 0.38268343` finds
none), and `PositionUpdater` only moves and flips. So a sprite, and any child
sprite added to one, stands in the world XY plane at 45° to the camera plane.
Two kinds of thing do face the camera: what billboards itself every frame, in
`ServedObjectGaugeBar.NormalizeTransform` and `PopupBookVisualPresenter`, and the
scene decoration baked into `GameScene.unity`, where `tree_1` and `grass_1` carry
a 45° X rotation. Do not copy a tree's transform into a runtime prefab.

That makes an area-of-effect ring cheap to draw. At this camera's 45° tilt sine
and cosine are equal, so a circle of radius r standing in the world XY plane and
a circle of radius r lying flat on the ground XZ plane both project to an ellipse
of horizontal radius r and vertical radius r·cos45 — the same ellipse, up to a
few percent of perspective between the standing ring's top and the ground
circle's far edge. An aura ring can therefore be a plain child sprite with no
rotation and no mesh: `RepairTotem.prefab` draws its repair aura that way and
`AuraRadiusScaler` sizes it from the `radius` parameter the server reads.

This equality holds only at 45°. If the camera's X rotation ever changes, a
standing ring stops matching the ground circle it stands for, and the visual has
to move to `SkillIndicatorShapeRenderer`, which builds a real mesh in the XZ
plane and already clips it to the field bounds.

### Server radii reach the client twice, and only one of them is drawn in a build

`GameObject.drawCircle` sends a component's radius to the client in the spawn
payload, but `ServedObjectGizmoRenderer` is wrapped in `#if UNITY_EDITOR`, so
those circles exist only in the Editor. A radius that has to be visible to
players comes instead from the parameter table `ParametersDataSource` caches,
which holds the same numbers the server reads. Use `AuraRadiusScaler` or read the
table the way it does; do not assume the gizmo is on screen because it is on the
wire.

## There is no Animator in this project

There are no `.controller` or `.anim` assets and no `Animator` reference in
`Assets/Scripts`. Do not plan animation states, parameters or clips. Animation is
three mechanisms:

- **Sprite swap.** `AttackSpriteSwapController` swaps `SpriteRenderer.sprite` for
  a duration on the `OnAttack` event. `OnAttackSpriteSwapper` is the older
  equivalent still used by `TreeGolem` and `RockGolem`; prefer the former.
  `SpriteFrameAnimator` loops a `Sprite[]` on a plain timer.
- **DOTween.** `DOTweenAction` holds the shared motions; the
  `ServedObjectComponent/Motion` controllers start an idle tween in `Awake`.
- **Spawned effect prefabs**, for hits, deaths and spawns.

### Replacing a hit visual may also require suppressing a projectile

Do not assume that changing `HitEffectController` replaces every visual attached
to an attack. The server can emit a `HitEvent` and a projectile DTO for the same
impact. Storm Stag tier 3/4 charges do exactly this: the hit event selects the
dedicated impact prefab, while the accompanying `ElectricShot` must be suppressed
for that charged source in `ProjectileSpawner`. If only the hit effect is changed,
the old projectile remains visible on top of the new impact.

When replacing an attack visual, inspect the full frame payload path and verify
both `events` and `objects.projectile`. Preserve projectiles with the same type
from unrelated sources; identify the source through a
`ReferenceProjectileTarget` and its active effects before suppressing anything.

`Player.prefab` has no `HitEffectController`. Any hit visual that must also work
against a player must be selected before the controller lookup and spawned from
the target `ServedObject` position (prefer
`GetEdgeWorldPositionTowards`). If the handler returns when the controller is
missing, the effect will work against creatures but silently disappear against
players; this is how the Storm Stag impact regression presented.

### A tween value is not always inside 0 to 1

`Ease.OutBack` peaks at **1.10** and `Ease.InBack` dips to **-0.10**; the Elastic
family is worse. That is the point of those curves, and it is harmless while the
value drives a scale or a colour. It is not harmless when the value is
multiplied by a distance: a 10% overshoot threw the stretching arm's hand a tenth
of its reach past its target, and because the arm runs downhill from the shoulder
to the enemy's body, past the target also meant into the floor.

Clamp with `Mathf.Clamp01` before using a tween value as a fraction of a
distance, and express any deliberate reach past the target in world units so it
does not scale with how far away the target happens to be.

Server `Status` arrives as a raw string and `ServedObject.HandleStatus` routes
only `Destroyed`, `Attack` and `Damaged`. Everything else, including `Idle` and
`Move`, falls through to `OnOtherStatus`, which only `PlayerActionController`
subscribes to. A creature cannot react to `Idle` or `Move` through status.

## Verifying without the Editor

The Editor cannot open this checkout from WSL, so gameplay geometry usually
cannot be run before review. When that is the case, verify the maths against the
real camera values above — read them out of `GameScene.unity` rather than
assuming — and say in the pull request which cases were computed and which still
need an Editor pass. For anything aimed across the field, check all four
headings: `+X`, `-X`, `+Z`, `-Z`. The `+Z` and `-Z` cases are where a world-space
length or offset fails, and where a bug hides if only `+X` was tried.
