#!/usr/bin/env python3
"""Build a 9-slice dialog-panel sprite from a generated wooden frame.

Unlike the plank button, a panel has four independent straight sides and four
corners, so the generated art needs cleanup on both axes at once: the top and
bottom bands must be uniform along their whole horizontal run, the left and
right bands uniform along their whole vertical run, and the parchment centre
one flat colour. Only the four corner squares keep the generator's own pixels,
because that is the only place `make-game-art` wants character.

The four corners are detected automatically: for each side, walk inward from
the middle of that side (a safe zone far from any corner) until the sampled
colour settles within `--tol` of the parchment colour at the true centre. The
largest of the four settle distances becomes the corner box size, so a corner
box always contains its whole chamfered facet.

Prints the spriteBorder (left, bottom, right, top) to copy into the .meta.

Usage:
  build-frame-panel.py SRC DST --width 240 --height 240 --center 8
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


def settle_distance(arr, edge, interior, tol, safe_frac=0.15):
    """Distance from `edge` ('top'/'bottom'/'left'/'right') at which a strip
    through the middle of that side first matches `interior` within `tol`."""
    h, w, _ = arr.shape
    if edge in ("top", "bottom"):
        lo, hi = int(w * (0.5 - safe_frac)), int(w * (0.5 + safe_frac))
        rng = range(h) if edge == "top" else range(h - 1, -1, -1)
        for i, y in enumerate(rng):
            sample = np.median(arr[y, lo:hi, :3], axis=0)
            if np.abs(sample - interior).max() <= tol:
                return i
    else:
        lo, hi = int(h * (0.5 - safe_frac)), int(h * (0.5 + safe_frac))
        rng = range(w) if edge == "left" else range(w - 1, -1, -1)
        for i, x in enumerate(rng):
            sample = np.median(arr[lo:hi, x, :3], axis=0)
            if np.abs(sample - interior).max() <= tol:
                return i
    raise SystemExit(f"{edge} edge never settles into the interior colour")


def main():
    p = argparse.ArgumentParser()
    p.add_argument("src")
    p.add_argument("dst")
    p.add_argument("--width", type=int, required=True)
    p.add_argument("--height", type=int, required=True)
    p.add_argument("--center", type=int, default=8, help="width/height of the stretch band")
    p.add_argument("--center-x", type=int, default=None, help="override --center for the horizontal band")
    p.add_argument("--center-y", type=int, default=None, help="override --center for the vertical band")
    p.add_argument("--tol", type=float, default=14.0)
    p.add_argument("--corner-margin", type=float, default=1.15,
                    help="multiplier applied to the largest settle distance")
    args = p.parse_args()

    img = crop_to_alpha(clean_alpha(Image.open(args.src)))
    img = resize_premultiplied(img, (args.width, args.height))
    arr = np.array(img).astype(np.float32)
    h, w, _ = arr.shape

    interior = np.median(arr[h // 2 - h // 12:h // 2 + h // 12,
                              w // 2 - w // 12:w // 2 + w // 12, :3], axis=(0, 1))

    top = settle_distance(arr, "top", interior, args.tol)
    bottom = settle_distance(arr, "bottom", interior, args.tol)
    left = settle_distance(arr, "left", interior, args.tol)
    right = settle_distance(arr, "right", interior, args.tol)
    corner = round(max(top, bottom, left, right) * args.corner_margin)
    corner = min(corner, w // 2 - args.center // 2 - 1, h // 2 - args.center // 2 - 1)

    out = arr.copy()
    out[:, :, 3] = 255  # panel is a solid rectangle once rebuilt; corners restore their own alpha below

    # Row profile for the top/bottom straight bands, taken from the untouched
    # middle stretch of each band (avoiding both corners).
    top_profile = np.median(arr[:top, corner:w - corner, :], axis=1, keepdims=True)
    bottom_profile = np.median(arr[h - bottom:, corner:w - corner, :], axis=1, keepdims=True)
    left_profile = np.median(arr[corner:h - corner, :left, :], axis=0, keepdims=True)
    right_profile = np.median(arr[corner:h - corner, w - right:, :], axis=0, keepdims=True)

    out[:top, corner:w - corner, :] = top_profile
    out[h - bottom:, corner:w - corner, :] = bottom_profile
    out[corner:h - corner, :left, :] = left_profile
    out[corner:h - corner, w - right:, :] = right_profile

    # Centre: one flat parchment colour.
    out[top:h - bottom, left:w - right, :3] = interior
    out[top:h - bottom, left:w - right, 3] = 255

    # restore the four corner squares byte-for-byte from the original art
    out[:corner, :corner, :] = arr[:corner, :corner, :]
    out[:corner, w - corner:, :] = arr[:corner, w - corner:, :]
    out[h - corner:, :corner, :] = arr[h - corner:, :corner, :]
    out[h - corner:, w - corner:, :] = arr[h - corner:, w - corner:, :]

    # Insert the stretch band: cut the straight middle of each axis down to
    # `--center` pixels so the sprite's total size matches --width/--height
    # while the border/corner geometry stays fixed regardless of target size.
    top_part = out[:top, :, :]
    bottom_part = out[h - bottom:, :, :]
    mid_rows = out[top:h - bottom, :, :]
    center_y = args.center_y if args.center_y is not None else args.center
    band_h = np.repeat(mid_rows[mid_rows.shape[0] // 2:mid_rows.shape[0] // 2 + 1, :, :], center_y, axis=0)
    out = np.concatenate([top_part, band_h, bottom_part], axis=0)

    left_part = out[:, :left, :]
    right_part = out[:, out.shape[1] - right:, :]
    mid_cols = out[:, left:out.shape[1] - right, :]
    center_x = args.center_x if args.center_x is not None else args.center
    band_w = np.repeat(mid_cols[:, mid_cols.shape[1] // 2:mid_cols.shape[1] // 2 + 1, :], center_x, axis=1)
    out = np.concatenate([left_part, band_w, right_part], axis=1)

    result = Image.fromarray(np.clip(out + 0.5, 0, 255).astype(np.uint8), "RGBA")
    result.save(args.dst)
    print(f"corner box: {corner}px, detected settle top={top} bottom={bottom} left={left} right={right}")
    print(f"size {result.width}x{result.height}")
    print(f"spriteBorder: {{x: {left}, y: {bottom}, z: {right}, w: {top}}}")


if __name__ == "__main__":
    main()
