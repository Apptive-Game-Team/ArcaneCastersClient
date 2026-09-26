#!/usr/bin/env python3
"""Move the floor of a built input-slot sprite to another colour.

`WoodInputSlot.png` was generated with a dark groove floor (#66473A), too dark
for the #3A2616 label every other control uses. Only pixels inside the floor
rectangle and near the floor colour move, and they move by the same offset, so
the floor keeps its faint shading while the rim and the corner facets, which
share some of the floor's browns, stay as generated. Recolouring the whole
image by colour distance instead speckled the corner facets.

The floor rectangle is the run of floor-coloured pixels through the sprite's
middle row and middle column.

Usage:
  recolour-slot-floor.py SRC DST --to E6CFA6 [--tol 40]
"""
import argparse

import numpy as np
from PIL import Image


def run(mask_line):
    """Start and end (exclusive) of the True run through the middle of a line."""
    mid = len(mask_line) // 2
    lo = hi = mid
    while lo > 0 and mask_line[lo - 1]:
        lo -= 1
    while hi < len(mask_line) and mask_line[hi]:
        hi += 1
    return lo, hi


def main():
    p = argparse.ArgumentParser()
    p.add_argument("src")
    p.add_argument("dst")
    p.add_argument("--to", required=True, help="new floor colour, hex RRGGBB")
    p.add_argument("--tol", type=float, default=40.0)
    args = p.parse_args()

    arr = np.array(Image.open(args.src).convert("RGBA")).astype(np.float32)
    h, w, _ = arr.shape
    floor = arr[h // 2, w // 2, :3].copy()
    near = np.abs(arr[:, :, :3] - floor).sum(axis=2) < args.tol
    x0, x1 = run(near[h // 2])
    y0, y1 = run(near[:, w // 2])

    target = np.array([int(args.to[i:i + 2], 16) for i in (0, 2, 4)], np.float32)
    box = np.zeros_like(near)
    box[y0:y1, x0:x1] = True
    moved = near & box
    arr[moved, :3] += target - floor
    Image.fromarray(np.clip(arr + 0.5, 0, 255).astype(np.uint8), "RGBA").save(args.dst)
    print(f"floor {floor.astype(int).tolist()} -> {target.astype(int).tolist()}, "
          f"box x {x0}..{x1} y {y0}..{y1}, {int(moved.sum())} pixels")


if __name__ == "__main__":
    main()
