# Earth Call production art

`EarthCall.png` previously reused the complete `RockGolem.png` character art.
This batch replaces it with a dedicated rubble-awakening spell effect while
keeping the existing Unity filename and `.meta` GUID.

## References

- `.art/anchors/master-v2/MasterStyleKey.png` — shared rendering technique
- `.art/anchors/master-v2/RockGolem.png` — warm stone and moss material
- `.art/anchors/master-v2/ArcaneImpact.png` — chunky magic-effect silhouette

Legacy live sprites were inspected only to identify the old placeholder and
were not used as rendering references.

## Candidates

- `EarthCall-v2-raw.png`: rejected. The 1.01 content ratio was too square for
  the old sprite's 0.89 geometry and would have reduced its in-game height.
- `EarthCall-v3-raw.png`: selected. Its keyed cutout has a 0.867 content ratio.
  The production fit is `207x238` inside the inherited `256x244` canvas, versus
  the old `212x238` content box.

The magenta-key pass reports enclosed background pixels because the circular
vibration band and separated rising stones intentionally enclose negative
spaces. Visual inspection of the cutout over grey and blue backgrounds confirms
that those pixels are gaps, not holes in the stone. The production image has
zero opaque magenta residue.

## Validation

- RGBA `256x244`, existing Bottom Center import settings retained
- content bbox `207x238`, bottom gap `0px`
- all four corner alpha values `0`
- transparent share `60.8%`
- opaque magenta residue `0` pixels
- `.art/tools/check-replacement.py 37a023b1`: 1 image checked, 0 problems
- `64px` silhouette and two-background cutout review:
  `.art/sheets/earth-call-review.png`

ImageMagick is unavailable on this workstation, so the `magick` skill's Pillow
fallback produced the resize, alpha checks, and focused contact sheet.
