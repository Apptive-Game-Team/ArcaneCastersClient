#!/usr/bin/env python3
"""Cut a flat chroma-key background out of a generated sprite.

`image_gen` on this machine does not return an alpha channel — it paints an
opaque background, and asked for transparency it draws a checkerboard into the
pixels. Asking it for one flat key colour instead gives something that can be
removed exactly, which a drawn checkerboard cannot.

Every key-coloured pixel is removed, wherever it sits — the gaps between the
staff, the arm and the body are enclosed by the subject and have to go too, so
a border flood fill leaves green blobs inside the silhouette. That only works
because the key colour appears nowhere on the subject; green is safe for this
character, whose brown hair, blue cloak, grey robe, skin and gold are all red-
or blue-dominant. The run reports how much it removed away from the border, so
a key tint landing on the subject shows up as a number instead of a hole.

The character's own colours are left alone. Only the boundary pixels get a
partial alpha and have the key colour divided back out, which is where the
fringe would otherwise be.
"""

import argparse
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image

# Below this the pixel is treated as pure background, above it as pure subject;
# in between it is a boundary pixel that keeps a partial alpha. The generator's
# key fill is not bit-exact — its outermost row reads about 0.07 coverage — so
# this sits above that, or the image keeps a one-pixel opaque frame that every
# later measurement then reads as content.
BACKGROUND_ALPHA = 0.10
SUBJECT_ALPHA = 0.94


def key_alpha(rgb, key_channel):
    """Per-pixel coverage, from how much the key channel exceeds the other two.

    With ``C = a * F + (1 - a) * K`` and a subject whose key channel never
    dominates its other two, everything by which the key channel overshoots the
    larger of those two came from the background.
    """
    others = [channel for channel in range(3) if channel != key_channel]
    strongest_other = np.maximum(rgb[:, :, others[0]], rgb[:, :, others[1]])
    excess = rgb[:, :, key_channel].astype(np.int16) - strongest_other.astype(np.int16)
    headroom = np.maximum(255 - strongest_other.astype(np.int16), 1)
    coverage = 1.0 - np.clip(excess / headroom, 0.0, 1.0)
    return coverage.astype(np.float32)


def magenta_alpha(rgb):
    """Per-pixel coverage for a magenta key, where red and blue are both the key.

    The single-channel form cannot express magenta, because either of red or blue
    alone is ordinary subject colour. What magenta has and no palette colour here
    has is red AND blue together above green, so the weaker of red and blue is what
    must overshoot green for a pixel to be background.

    This is what the magic book icons needed. A green key would have eaten
    boulder_strike's moss and spirit_bomb's leaves, and a blue key would have bitten
    into boulder_strike's pale crescents; nothing in those palettes is magenta.
    """
    weakest_key = np.minimum(rgb[:, :, 0], rgb[:, :, 2])
    green = rgb[:, :, 1]
    excess = weakest_key.astype(np.int16) - green.astype(np.int16)
    headroom = np.maximum(255 - green.astype(np.int16), 1)
    coverage = 1.0 - np.clip(excess / headroom, 0.0, 1.0)
    return coverage.astype(np.float32)


def reachable_from_border(is_key):
    """Key-coloured pixels connected to the border, four-way. Reported, not enforced."""
    height, width = is_key.shape
    reached = np.zeros_like(is_key)
    queue = deque()
    for x in range(width):
        for y in (0, height - 1):
            if is_key[y, x] and not reached[y, x]:
                reached[y, x] = True
                queue.append((y, x))
    for y in range(height):
        for x in (0, width - 1):
            if is_key[y, x] and not reached[y, x]:
                reached[y, x] = True
                queue.append((y, x))
    while queue:
        y, x = queue.popleft()
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < height and 0 <= nx < width and is_key[ny, nx] and not reached[ny, nx]:
                reached[ny, nx] = True
                queue.append((ny, nx))
    return reached


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("destination", type=Path)
    parser.add_argument("--key", choices=("green", "magenta", "blue"), default="green")
    args = parser.parse_args()

    image = Image.open(args.source).convert("RGB")
    rgb = np.asarray(image)
    if args.key == "magenta":
        coverage = magenta_alpha(rgb)
    else:
        coverage = key_alpha(rgb, {"green": 1, "blue": 2}[args.key])

    background = coverage < SUBJECT_ALPHA
    alpha = np.ones(coverage.shape, dtype=np.float32)
    alpha[background] = coverage[background]
    alpha[coverage < BACKGROUND_ALPHA] = 0.0

    # Divide the key colour back out of the boundary pixels, where the drawn
    # edge is a blend of the subject and the background.
    key_colour = np.zeros(3, dtype=np.float32)
    if args.key == "magenta":
        key_colour[0] = key_colour[2] = 255.0
    else:
        key_colour[{"green": 1, "blue": 2}[args.key]] = 255.0
    boundary = background & (alpha > 0.0)
    out = rgb.astype(np.float32)
    if boundary.any():
        share = alpha[boundary][:, None]
        blended = out[boundary]
        out[boundary] = np.clip((blended - (1.0 - share) * key_colour) / share, 0, 255)

    rgba = np.dstack([out.astype(np.uint8), (alpha * 255).astype(np.uint8)])
    Image.fromarray(rgba, "RGBA").save(args.destination, optimize=True)

    enclosed = int((background & ~reachable_from_border(background)).sum())
    transparent = float((alpha == 0).mean())
    partial = int(((alpha > 0) & (alpha < 1)).sum())
    print(f"{args.destination.name} {image.size} transparent={transparent:.1%} "
          f"boundary_pixels={partial} enclosed_pixels={enclosed}")


if __name__ == "__main__":
    main()
