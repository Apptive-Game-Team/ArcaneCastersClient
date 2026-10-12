# brick golem (PveBrickGolem)

Subject: the fortress adventure's last boss, `PveBrickGolem` (server object `pve_brick_golem`,
issue #329). It replaces `PveVineWitch` in scenario 13. The fortress walls held an old guardian
golem; the vines that overran the fortress corrupted it and woke it. It is the upper body of a
golem built from the fortress's own wall blocks, rising out of rubble, and it never moves. It
attacks with two arms: a slam that is telegraphed by a ground warning for 1.0 s, and a short
sweep.

`WORLD.md` has no entry for a fortress guardian. The golem is placed in the fortress story only;
whether it belongs to 돌 골렘 부족 is not decided, so the art borrows the rock-golem tribe's
technique (blocky stacked stone, seams, moss) and the fortress set's pale warm stone.

## References

- `.art/anchors/master-v2/MasterStyleKey.png` — technique.
- `.art/anchors/master-v2/RockGolem.png` — stone material, block seams, moss.
- `Assets/Art/Images/Background/Fortress/tree_3.png` (the ruined wall prop of the fortress battle
  theme) — stone palette only, stated as such in the prompt.
- Effects: `MasterStyleKey.png`, `ArcaneImpact.png`, `RockGolem.png`.

Palette sampled from `tree_3.png` and `tree_1.png` with a Pillow quantize: stone `#D8C9AA`
`#C7BCA7` `#AAA08D` `#918977` `#776F59`, seams `#535037`, moss `#6F6B4D` `#545333`, plus the
tribe moss `#374725` `#55572D` and eye glow `#C8E86A`.

## Generation

2 `image_gen` calls in total, both passed on the first attempt. Both through
`codex exec --approve-for-me -i <refs...> < prompt`, with an output path appended to the prompt.

| call | prompt | references | result |
|---|---|---|---|
| 1 | `prompts/golem-twin-v1.prompt` | MasterStyleKey, RockGolem, fortress `tree_3.png` | `golem-twin-v1-raw.png`, 1774x887 RGB, base pose left, attack pose right |
| 2 | `prompts/effects-twin-v1.prompt` | MasterStyleKey, ArcaneImpact, RockGolem | `effects-twin-v1-raw.png`, 1774x887 RGB, crack ring left, sweep smear right |

`image_gen` exposes no seed. Both images came back opaque on a near-magenta background
(corners `#F30CEF` to `#EB28E8`, not exactly `#FF00FF`).

## Finalization

```
.art/tools/key-out-background.py golem-twin-v1-raw.png twin-cut.png --key magenta
# transparent=39.5% boundary_pixels=11964 enclosed_pixels=8080
.art/tools/key-out-background.py effects-twin-v1-raw.png fx-cut.png --key magenta
# transparent=71.8% boundary_pixels=13397 enclosed_pixels=241711
```

`enclosed_pixels` is not key tint on the subject here: for the golem it is the background
between the arms and the body, for the effects it is the empty middle of the ring. The cutout
was checked over a flat blue, and no opaque pixel keeps the magenta key colour.

The golem pair is cut at x = 887 into two square panels, then cropped with **one shared box**,
the union of both panels' alpha bounding boxes, `(0, 19, 884, 887)`, and scaled by **one
shared factor**, 0.2896, to 256x251. Both frames therefore share canvas, scale and the body's
position: the eyes sit on the same pixels in both. The rubble under the attack pose ends 12 source
pixels (3 final pixels) above the base pose's, because the generator drew the rubble a little
differently; the body itself does not move.

The two effects are cropped to their own alpha box and fitted inside 256.

| file | size | pivot | PPU |
|---|---|---|---|
| `Assets/Resources/Game/sprites/PveBrickGolem.png` | 256x251 | Bottom Center | 70 |
| `Assets/Resources/Game/sprites/PveBrickGolem2.png` | 256x251 | Bottom Center | 70 |
| `Assets/Resources/Game/explode/brick_golem_crack_ring.png` | 255x256 | Center | 100 |
| `Assets/Resources/Game/shoot/brick_golem_sweep.png` | 256x144 | Center | 100 |

## Size

PPU 70 makes the golem 3.66 x 3.59 units. The server radius is 1.6 (body 3.2 units across), and
`EvilEnt` at PPU 100 is 2.24 x 2.56. `scale-candidates-ppu.png` puts PPU 100, 80 and 70 next to
`EvilEnt` and `RockGolem` over the fortress ground texture; 70 is the one that reads as a boss
and matches the radius. `composite-game-scale.png` is the check at game scale: the golem flipped
as the right-side player, a unit standing inside the slam warning ring, the slam just before
impact, and the sweep smear.

Facing: the eye pixels sit right of the body centre in the file. Like `EvilEnt.png`, the file
faces right and `ServedObject` flips it for `RightPlayer`, so it faces left in the match.

## Prefabs

- `Resources/Prefabs/PveBrickGolem.prefab` — copy of `PveEvilEnt.prefab` (an `AbstractRangeMob`
  variant with `HoppingMotionController` removed). Changed: name, body sprite, `swapSprite`
  = `PveBrickGolem2`, shadow scale `(3.3, 0.6)`, hp bar `y` 3.15 (just above the base pose's
  shoulders at 2.99), click collider `3.66 x 3.59` with offset `y` 1.79.
- `Resources/Projectiles/BrickGolemSlamWarning.prefab` — `GroundWarningProjectile`. The ring lies
  flat on the ground at the end point, grows from 0.55 to 1.0 of scale 1.2 and pulses three times.
  At scale 1.2 the ring is 3.06 units across, which matches `effect_radius` 1.5.
- `Resources/Projectiles/BrickGolemSlam.prefab` — `FallingFistProjectile`. `TitanFist.png`
  drawn with `flipY` drops onto the end point in 40% of the duration, then `rock_explode.png`
  bursts and the crack ring fades out.
- `Resources/Projectiles/BrickGolemSweep.prefab` — `AbstractProjectile` variant
  (`DefaultProjectile`). The smear moves from the golem to the end point, lifted 0.8 above
  the ground.

## Not run

- `./.art/make-sheets.sh` needs `magick`, which is not installed. All checks above used a Pillow
  virtual environment.
- No Unity Editor: the prefabs, `.meta` files, sorting and the `flipY` pivot behaviour of the
  falling fist were not seen in a running scene.
