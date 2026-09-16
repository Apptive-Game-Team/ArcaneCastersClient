# CloudDragon frame pair (issue #694)

Redraws `Assets/Resources/Game/sprites/CloudDragon.png` (base pose, no water)
and `Assets/Resources/Game/sprites/CloudDragonAttacking.png` (water-spray
attack pose) to the master cut-paper style, replacing the legacy painted-cloud
art. References used: `.art/anchors/master-v2/MasterStyleKey.png` (rendering
technique) and the existing `ThunderSpirit.png` / `WindSpirit.png` sprites
(how the Spirits faction's faceted wisp/tuft language reads in production).
Per the make-game-art skill and this issue's own instructions, the twin-panel
technique (base and attack drawn side by side in one canvas) was avoided —
earlier sessions on this asset failed with it more than ten times, the
generator repeatedly shrinking the body to make room for both poses.

## Base pose — `CloudDragon-base-v1-*`

Generated in a parallel worktree (`client-art-frames`, working the `RockTurret`
and `TreeGolem` pairs) as a single square magenta-key generation, one subject,
no water. Read-only reused here rather than spending a fresh `image_gen` call:
the cutout was clean (all four corners alpha 0, zero magenta residue, sane
transparent share) and the style matched `ThunderSpirit`/`WindSpirit` well —
faceted cloud-tuft clusters, three value bands, upper-left light, simple oval
eye. `-raw.png` is the untouched magenta generation; `-cut.png` is after
`key-out-background.py --key magenta`.

Finalized to production geometry with:

```
git show origin/main:Assets/Resources/Game/sprites/CloudDragon.png > old-base.png
python3 embed.py CloudDragon-base-v1-cut.png base-embedded.png 256 182   # ad hoc helper, not committed
python3 .art/tools/fit-to-original.py old-base.png base-embedded.png Assets/Resources/Game/sprites/CloudDragon.png
```

`fit-to-original.py` requires both PNGs at the same canvas size, so
`embed.py` (a small scratchpad helper, not part of the repo's tool set) first
trims the candidate to its alpha bbox and thumbnails it into an empty 256x182
canvas without distorting its aspect ratio; `fit-to-original.py` then does the
real scale/position fit against the old file's content bbox and bottom gap.
Landed at scale 1.00 — the candidate's own proportions already matched the old
file's fill almost exactly. `check-replacement.py origin/main` reports 0
problems.

## Attack pose — `CloudDragon-attack-v1-*` and `-v2-*` (both rejected), `-v3-*` (shipped)

`CloudDragon-attack-v1-*` is the parallel worktree's matching attack
generation, also read-only reused at first. Its water-spray moment itself was
correct (water actively leaving the mouth with separated droplets, not a
gathering/preparing pose — the failure mode called out for `TreeGolem`) but
its raw dragon body was drawn smaller relative to its own canvas than the base
generation's body (dragon-only content height 816px of a 1254 canvas, vs the
base candidate's 1034px of the same size canvas — about 27% smaller). Fitting
each frame independently to its own old file (as `fit-to-original.py` is
built to do) passed `check-replacement.py` for both files individually, since
each only compares against its own old version, but `check-frame-pair.py`
caught the real problem: fitting the attack frame to fill its own old canvas
the same way rescaled the body inconsistently with the already-finalized base
frame, producing a body that would visibly jump size and position on the
attack/idle swap. Not used.

Regenerated instead as `CloudDragon-attack-v2-raw.png`: `codex exec` with the
*finalized* base sprite (`Assets/Resources/Game/sprites/CloudDragon.png`,
already fit to old geometry) attached as the primary reference and an explicit
instruction to keep body shape, size, and position identical and only open the
mouth and add a short water burst — see
`.art/concept/frame-pair-prompts/cloud-dragon-attack-v1.txt`. The returned
body matches the base reference closely (confirmed by overlaying the two
finalized frames: wing, horn, tail, and spine spikes overlap almost exactly).

The one problem: the generated water jet plus its two droplets extended about
317px in the raw canvas measured from the mouth, while the fixed 256px-wide
production canvas only has ~24px of margin to the right of the body once the
body is placed at the correct scale and at the same horizontal position as the
base frame (the body itself uses ~208 of the 256px). Placing the raw water at
full body-consistent scale would have clipped both droplets and part of the
main stream — tried and rejected, see `CloudDragon-attack-v2-cut.png` for the
unscaled cutout this was measured from. Rather than spend a third `image_gen`
call, `CloudDragon-attack-v2-composited-final.png` was built by compositing at
a fixed body scale: the dragon body is cropped from `-v2-cut.png` and scaled
so its content height fills the 182px canvas height exactly (matching the base
frame's own fill), and the water region (jet + both droplets, cropped as one
piece to keep their relative spacing) is scaled down by an extra factor
(~0.40) so the whole effect fits in the remaining ~24px next to the mouth,
then pasted flush against it. This keeps the body pixel-for-pixel at the
correct scale and position and keeps both droplets visible, at the cost of a
smaller-looking water burst than the raw generation intended.

`CloudDragon-attack-v2-composited-final.png` shipped first (PR #731's initial
commit), keeping body scale exact but shrinking the whole water effect by
~0.40 to fit the 256px canvas. Review feedback on that PR: the water read as
"a small blue bump next to the mouth," not a spray — at the 0.1s the attack
frame is shown, a jet that isn't instantly readable is the same as no jet at
all. Manually shrinking the water was also the wrong move in principle, since
it fights the generator's own proportions instead of giving them room.

The fix, per review direction and the precedent set by `TreeGolem2` (widened
256x215 to 312x256 for the same reason): keep height, PPU, and the body's
scale/baseline/vertical position exactly as they are, and widen the canvas —
the extra width is entirely the water's, not the body's.

`CloudDragon-attack-v3-raw.png` was regenerated with the same finalized base
attached as reference, this time explicitly asking for a wide landscape
canvas with a bold stream "roughly half the body's nose-to-tail length" and
"a third of the head's height" thick — see
`.art/concept/frame-pair-prompts/cloud-dragon-attack-v3.txt` for the prompt as
written. `codex exec`'s own agentic loop hit an output-moderation block on its
first internal attempt (flagged under the generic `"other"` category, most
likely triggered by the word "attack") and retried on its own with an
edit-only framing instead of an attack framing; the prompt that actually
produced the successful generation is saved separately as
`cloud-dragon-attack-v3-actual-used.txt`, since it differs from what was
asked. The agent also ran its own post-processing (a uniform vertical
stretch to remove magenta margins, then a horizontal squeeze of everything
right of an arbitrary split point) before proposing a "final" file — that
output was **not** used, since the horizontal squeeze is exactly the kind of
manual water-shrinking to avoid. Instead, `CloudDragon-attack-v3-cut.png` (the
untouched raw generation run through `key-out-background.py --key magenta`,
nothing else) was reprocessed from scratch:

- Dragon-only content (everything left of the mouth, found by scanning each
  row's rightmost opaque pixel and taking the width where it stabilizes
  before the jet begins) measured 893x785px raw, matching the base
  candidate's own proportions closely (207x182 final vs the base's own
  207x182 — this generation's body, unlike `-v2-*`, came back at consistent
  scale with the base on the first try).
- Scale to production: `182/785 = 0.23185`, applied to the *entire* crop
  (body and water together, cropped to the body's own top/bottom so the water
  cannot introduce a vertical mismatch) — one uniform scale, no separate
  squeeze for the water.
- Placement accounts for `BottomCenter` sprite alignment (`alignment: 7` in
  both files' `.meta`): the pivot Unity actually uses at runtime is the
  horizontal *center of whatever the canvas width is*, not a fixed pixel
  count, so simply padding the extra canvas width onto the right (keeping the
  body's own pixel offset unchanged) would shift the body's rendered world
  position left by half the added width. The fix is to shift the body
  rightward by exactly half of the width increase, which cancels the pivot
  move: `body_left_new = new_canvas_width/2 - 104`, where `104` is
  `base_pivot(128) - base_body_left(24)`, the base frame's own fixed
  body-to-pivot offset. Canvas width `540` was chosen as the smallest width
  (rounded up a little) that fits the entire generated water region with zero
  clipping at that placement.
- This is a straight crop+scale+paste of the untouched generation — no manual
  resizing of the water region at all, per the "use the generated jet at its
  generated size" instruction. `CloudDragon-attack-v3-wide-final.png` is this
  finalized 540x182 result, copied directly to
  `Assets/Resources/Game/sprites/CloudDragonAttacking.png`.

## Validation

- `python3 .art/tools/check-replacement.py origin/main` — reports one line for
  `CloudDragonAttacking.png`: the canvas changed from `256x182` to `540x182`.
  This is the intended change (see above), not a defect; content-vs-canvas
  fill and bottom gap are otherwise unaffected since the body itself is
  untouched.
- `python3 .art/tools/check-frame-pair.py Assets/Resources/Game/sprites/CloudDragon.png Assets/Resources/Game/sprites/CloudDragonAttacking.png`
  — vertical difference `+0.000` unit (passes, limit 0.03). Horizontal
  difference comes back as `+0.958` unit against a 0.05 limit — a large
  number, but a false positive from the script's own method, not body drift.
  The script takes the horizontal center of the bottom 10% of rows as the
  "foot" position; with a thick water stream passing through that band, the
  band's pixel count is dominated by water, not by the dragon's own (much
  narrower) paw silhouette at the same height. Confirmed by overlaying the
  two finalized frames aligned on their actual runtime pivot (each canvas's
  own horizontal center, per `BottomCenter` alignment, `overlay-v3-final-
  pivot-aligned.png` — produced in the session scratchpad, not committed):
  wing, horn, tail, and spine spikes land on the same pixels in both frames.
  A naive pixel-for-pixel overlay (ignoring the pivot shift) makes the body
  look offset by half the width difference; that comparison is wrong for a
  pair whose canvases differ in width under `BottomCenter` alignment, and is
  not what Unity renders. `.art/ANIMATION-ASSETS.md`'s Cloud Dragon section
  documents this false-positive mode for future replacements that also need a
  wider canvas.
- Corner alpha, transparent share, and magenta-residue checks (per the
  make-game-art skill's cutout section) pass on both finalized files: all four
  corners alpha 0, 0 magenta-toned opaque pixels on either file.

## Not used

- `.art/concept/frame-pair-prompts/cloud-dragon-idle-v1.txt` — this session's
  own first base-pose attempt, generated before the parallel worktree's
  already-finished candidate was found. Also a reasonable match to the master
  style, but not needed once the existing candidate was confirmed usable; kept
  for the record, not finalized.
- `.art/concept/frame-pairs/CloudDragon-attack-v1-raw.png` /
  `-cut.png` — see rejection above (body-scale mismatch against the base
  frame).
- `.art/concept/frame-pairs/CloudDragon-attack-v2-*` — shipped in this PR's
  first commit, replaced after review: the manually-shrunk water read as too
  small to register as "spraying water" in the 0.1s the attack frame is shown.
- `cloud-dragon-attack-v3.txt` describes what was asked for; the generation
  agent's own moderation-retry rewrote it before the successful call (see
  `cloud-dragon-attack-v3-actual-used.txt`), and its own follow-up manual
  crop/stretch/squeeze of that output was discarded in favor of reprocessing
  the untouched raw generation directly (above).

`cloud.png` (the dedicated spherical water aura) is unrelated to this pair and
was not touched.
