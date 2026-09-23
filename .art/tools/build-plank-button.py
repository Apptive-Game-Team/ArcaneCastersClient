#!/usr/bin/env python3
"""Build a 9-slice button sprite from a generated wooden plank.

The generated plank has hand-cut faceted ends and a long middle that is almost,
but not exactly, uniform: the image generator leaves a faint gradient and noise
along its length. A 9-slice stretches the middle, so any drift there shows up as
a smear. This script keeps the two ends as drawn and rebuilds everything between
them from one clean column profile, then inserts a band of replicated rows at a
height where every column is a vertical strip, so the sprite also stretches
vertically without distorting the end facets.

Prints the spriteBorder (left, bottom, right, top) to copy into the .meta.

Usage:
  build-plank-button.py SRC DST --height 132 --row-frac 0.34 --center 9

Pick --center so the output width and height are multiples of 4. #118 built
Assets/Art/Images/UI/WoodPlankButton.png (104x140) from
.art/concept/wood-ui/plank-b2-raw.png with exactly the line above.
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


def settled_column(arr, profile, start, step, stop, tol):
    """First column, walking from `start` toward the middle, whose every
    opaque row is within `tol` of the middle profile."""
    for x in range(start, stop, step):
        diff = np.abs(arr[:, x, :3] - profile[:, :3]).max(axis=1)
        if diff.max() <= tol:
            return x
    raise SystemExit("plank end never settles into the middle profile")


def main():
    p = argparse.ArgumentParser()
    p.add_argument("src")
    p.add_argument("dst")
    p.add_argument("--height", type=int, default=132)
    p.add_argument("--row-frac", type=float, default=0.34,
                   help="height (0 top, 1 bottom) of the row replicated for vertical stretch")
    p.add_argument("--center", type=int, default=9, help="width and height of the stretch band")
    p.add_argument("--tol", type=float, default=18.0)
    p.add_argument("--blend", type=int, default=4)
    args = p.parse_args()

    img = crop_to_alpha(clean_alpha(Image.open(args.src)))
    scale = args.height / img.height
    img = resize_premultiplied(img, (round(img.width * scale), args.height))
    arr = np.array(img).astype(np.float32)
    h, w, _ = arr.shape

    # One clean colour per row, taken from the middle 40% of the plank.
    mid = arr[:, int(w * 0.3):int(w * 0.7), :]
    profile = np.median(mid, axis=1)

    left = settled_column(arr, profile, 0, 1, w // 2, args.tol)
    right = settled_column(arr, profile, w - 1, -1, w // 2, args.tol)
    left_border = left + 2
    right_border = (w - 1 - right) + 2

    # Horizontal: left end + stretch band of the profile + right end, with the
    # last few end columns blended into the profile so no seam shows.
    left_part = arr[:, :left_border, :].copy()
    right_part = arr[:, w - right_border:, :].copy()
    for i in range(args.blend):
        t = (i + 1) / (args.blend + 1)
        left_part[:, left_border - args.blend + i, :] = (
            (1 - t) * left_part[:, left_border - args.blend + i, :] + t * profile)
        right_part[:, args.blend - 1 - i, :] = (
            (1 - t) * right_part[:, args.blend - 1 - i, :] + t * profile)
    band = np.repeat(profile[:, None, :], args.center, axis=1)
    arr = np.concatenate([left_part, band, right_part], axis=1)

    # Vertical: replicate one row where every column is a vertical strip.
    y0 = round((h - 1) * args.row_frac)
    rows = np.repeat(arr[y0:y0 + 1, :, :], args.center, axis=0)
    arr = np.concatenate([arr[:y0, :, :], rows, arr[y0 + 1:, :, :]], axis=0)

    out = Image.fromarray(np.clip(arr + 0.5, 0, 255).astype(np.uint8), "RGBA")
    out.save(args.dst)
    top_border = y0
    bottom_border = h - 1 - y0
    print(f"size {out.width}x{out.height}")
    print(f"spriteBorder: {{x: {left_border}, y: {bottom_border}, z: {right_border}, w: {top_border}}}")


if __name__ == "__main__":
    main()
