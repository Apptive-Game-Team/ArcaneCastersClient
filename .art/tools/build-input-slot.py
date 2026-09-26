#!/usr/bin/env python3
"""Build a 9-slice input-field sprite from a generated wooden groove.

Copied from `build-plank-button.py` (see that file for the general approach:
keep the two generated ends, rebuild the middle from one column profile, and
insert a replicated-row band for vertical stretch) and adapted for one defect
that only shows up on this shape: the input slot's hand-cut ends taper to
alpha 0 within the very top and bottom rows before the shape widens to the
full groove height, so a column near an end has some fully-transparent rows.
`build-plank-button.py`'s `settled_column` compares every row's RGB against
the middle profile including those transparent rows, whose unpremultiplied
RGB is near-black — nothing like the profile — so it reports a fake mismatch
all the way to the middle of the plank instead of the true few dozen end
pixels. Rows with alpha below `--alpha-floor` are excluded from that
comparison here; everything else is identical to `build-plank-button.py`.

Prints the spriteBorder (left, bottom, right, top) to copy into the .meta.

Usage:
  build-input-slot.py SRC DST --height 132 --row-frac 0.5 --center 9
"""
import argparse

import numpy as np
from PIL import Image


def clean_alpha(img):
    a = np.array(img.convert("RGBA")).astype(np.float32)
    alpha = a[:, :, 3]
    alpha[alpha < 16] = 0
    alpha[alpha >= 240] = 255
    a[:, :, 3] = alpha
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def crop_to_alpha(img):
    alpha = np.array(img)[:, :, 3]
    ys, xs = np.where(alpha > 128)
    return img.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))


def resize_premultiplied(img, size):
    return img.convert("RGBa").resize(size, Image.LANCZOS).convert("RGBA")


def settled_column(arr, alpha, profile, start, step, stop, tol, alpha_floor, max_border=None):
    """First column, walking from `start` toward the middle, whose every
    *opaque* row is within `tol` of the middle profile. Rows below
    `alpha_floor` are end taper, not shape, and are ignored.

    A long faint gradient left over from generation (not real facet geometry)
    can push the true settle point past any reasonable border width. When
    `max_border` is given, the search stops there and reports that column
    instead of running to the middle — the remaining drift gets flattened by
    the centre-fill anyway, same as the plank tool's four-pixel blend hides a
    much smaller version of the same thing.
    """
    limit = start + step * max_border if max_border is not None else stop
    if step > 0:
        limit = min(limit, stop)
    else:
        limit = max(limit, stop)
    for x in range(start, limit, step):
        visible = alpha[:, x] >= alpha_floor
        if not visible.any():
            continue
        diff = np.abs(arr[visible, x, :3] - profile[visible, :3]).max(axis=1)
        if diff.max() <= tol:
            return x
    if max_border is not None:
        print(f"warning: end never settled within {max_border}px, capping there")
        return start + step * (max_border - 1)
    raise SystemExit("slot end never settles into the middle profile")


def main():
    p = argparse.ArgumentParser()
    p.add_argument("src")
    p.add_argument("dst")
    p.add_argument("--height", type=int, default=132)
    p.add_argument("--row-frac", type=float, default=0.5,
                   help="height (0 top, 1 bottom) of the row replicated for vertical stretch")
    p.add_argument("--center", type=int, default=9, help="width and height of the stretch band")
    p.add_argument("--center-x", type=int, default=None, help="override --center for the horizontal band")
    p.add_argument("--center-y", type=int, default=None, help="override --center for the vertical band")
    p.add_argument("--tol", type=float, default=18.0)
    p.add_argument("--alpha-floor", type=float, default=200.0,
                   help="rows with alpha below this are end taper, excluded from settle detection")
    p.add_argument("--blend", type=int, default=4)
    p.add_argument("--max-border", type=int, default=None,
                   help="cap the settle search this far from each end (see settled_column)")
    args = p.parse_args()

    img = crop_to_alpha(clean_alpha(Image.open(args.src)))
    scale = args.height / img.height
    img = resize_premultiplied(img, (round(img.width * scale), args.height))
    arr = np.array(img).astype(np.float32)
    alpha = arr[:, :, 3]
    h, w, _ = arr.shape

    # One clean colour per row, taken from the middle 40% of the slot.
    mid = arr[:, int(w * 0.3):int(w * 0.7), :]
    profile = np.median(mid, axis=1)

    left = settled_column(arr, alpha, profile, 0, 1, w // 2, args.tol, args.alpha_floor, args.max_border)
    right = settled_column(arr, alpha, profile, w - 1, -1, w // 2, args.tol, args.alpha_floor, args.max_border)
    left_border = left + 2
    right_border = (w - 1 - right) + 2

    left_part = arr[:, :left_border, :].copy()
    right_part = arr[:, w - right_border:, :].copy()
    for i in range(args.blend):
        t = (i + 1) / (args.blend + 1)
        left_part[:, left_border - args.blend + i, :] = (
            (1 - t) * left_part[:, left_border - args.blend + i, :] + t * profile)
        right_part[:, args.blend - 1 - i, :] = (
            (1 - t) * right_part[:, args.blend - 1 - i, :] + t * profile)
    center_x = args.center_x if args.center_x is not None else args.center
    band = np.repeat(profile[:, None, :], center_x, axis=1)
    arr = np.concatenate([left_part, band, right_part], axis=1)

    # Vertical: replicate one row where every column is a vertical strip.
    center_y = args.center_y if args.center_y is not None else args.center
    y0 = round((h - 1) * args.row_frac)
    rows = np.repeat(arr[y0:y0 + 1, :, :], center_y, axis=0)
    arr = np.concatenate([arr[:y0, :, :], rows, arr[y0 + 1:, :, :]], axis=0)

    out = Image.fromarray(np.clip(arr + 0.5, 0, 255).astype(np.uint8), "RGBA")
    out.save(args.dst)
    top_border = y0
    bottom_border = h - 1 - y0
    print(f"size {out.width}x{out.height}")
    print(f"spriteBorder: {{x: {left_border}, y: {bottom_border}, z: {right_border}, w: {top_border}}}")


if __name__ == "__main__":
    main()
