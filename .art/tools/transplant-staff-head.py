#!/usr/bin/env python3
"""Replace the staff head in the three player frames with the app icon's curled head.

Issue #116. The body must not move by a single pixel, because
`PlayerStaffPoseController` swaps the three frames on one canvas. Redrawing the
frames with `image_gen` moves and rescales the body, so this script does not
generate anything. It cuts the old forked head off each frame, extends the old
shaft along its own axis, and pastes one head taken from
`.art/concept/player-staff/icon-crystal-alpha.png`, rotated onto each frame's
staff axis at one shared scale. The three heads are the same pixels by
construction.

Every pixel it writes lies inside the frame's `remove` region, the shaft
extension or the pasted head; everything else is copied from the source frame.

    transplant-staff-head.py <icon-alpha.png> <frames-dir> <out-dir>

`<frames-dir>` must hold the frames from before #116 (`git show
3e24ec79:Assets/Art/Images/Customize/<name>`): the removal regions below are
measured on the forked head and do nothing sensible on the curled one.

Placement is tied to `Player.prefab`: `PlayerStaffAuraController` puts the
element aura at `idleLocalPosition` on the raised frame and at
`attackLocalPosition` on the attack frame, which are the old crystal centres.
The new crystal centre is placed on the old one unless the curl would leave the
canvas; the script prints how far it had to move.
"""

import sys
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image

# 0.27 would match the icon shaft (133px) to the frames' shafts (33-39px), but
# at 64px the curl then reads as a plain knob. 0.30 keeps the curl's opening
# visible at 64px; the shaft extension tapers the 3-5px difference away. 0.33
# reads better still but pushes the attack crystal 49px off the aura anchor.
SCALE = 0.30
# Icon rows: the lowest collar ring ends around y=920; keep a short piece of
# plain shaft below it and feather it into the old shaft.
ICON_CUT_Y = 965
ICON_FEATHER = 24
ICON_ERODE = 2
ICON_STAR_SEED = (760, 330)  # (x, y) inside the gold star, which is dropped
MARGIN = 3

FRAMES = {
    # axis_rows / axis_cols: where the plain shaft is measured.
    # ref: the plain cross-section the shaft extension is copied from; the old
    #      head is cleared up to a few pixels short of it (see remove_mask).
    # crystal: the old crystal centre, which is where the aura anchor points.
    "PlayerCharacterBase.png": {
        "axis_rows": [(724, 765), (1120, 1210)],
        "ref": ("row", 726),
        "crystal": (148.5, 505.0),
    },
    "PlayerCharacterStaffRaised.png": {
        "axis_rows": [(342, 415), (750, 915)],
        "ref": ("row", 344),
        "crystal": (154.0, 98.5),
    },
    "PlayerCharacterAttack.png": {
        "axis_cols": [(612, 698)],
        "ref": ("col", 697),
        "crystal": (909.5, 731.5),
    },
}

# Base: the right prong sits in front of the hair's left edge on rows 505-586,
# where the alpha channel cannot separate the two. The hair edge is measured on
# the rows just above and below (x=220 at row 504, x=216 at row 587) and drawn
# straight between them; the prong pixels right of that line are refilled with
# the hair to their right. The prong is the light wood (red > 135), the hair is
# dark (red 65-110).
BASE_MERGED_ROWS = (505, 586)
BASE_HAIR_EDGE = ((504, 220.0), (587, 216.0))
BASE_PRONG_RED = 135


def first_run(row, near):
    """The opaque run in `row` that contains column `near`."""
    if not row[near]:
        return None
    left = near
    while left > 0 and row[left - 1]:
        left -= 1
    right = near
    while right < len(row) - 1 and row[right + 1]:
        right += 1
    return left, right


def remove_mask(name, alpha):
    h, w = alpha.shape
    opaque = alpha > 0
    mask = np.zeros_like(opaque)
    if name == "PlayerCharacterBase.png":
        top, bottom = BASE_MERGED_ROWS
        for y in range(380, 722):
            if top <= y <= bottom:
                edge = np.interp(y, *zip(*BASE_HAIR_EDGE))
                mask[y, :int(np.floor(edge))] = opaque[y, :int(np.floor(edge))]
                continue
            # The staff is the leftmost run on these rows; the hair starts
            # further right.
            xs = np.nonzero(alpha[y, :330] > 128)[0]
            run = first_run(opaque[y], xs[0]) if len(xs) else None
            if run and run[0] < 186:
                mask[y, :run[1] + 1] = opaque[y, :run[1] + 1]
    elif name == "PlayerCharacterStaffRaised.png":
        mask[:340, :260] = opaque[:340, :260]
    else:
        mask[600:900, 700:] = opaque[600:900, 700:]
    return mask


def fit_axis(alpha, spec):
    """Fit the shaft centre line; return a point on it and the unit tip direction."""
    opaque = alpha > 128
    points = []
    if "axis_rows" in spec:
        for y0, y1 in spec["axis_rows"]:
            for y in range(y0, y1 + 1):
                row = opaque[y]
                xs = np.nonzero(row[:300])[0]
                run = first_run(row, xs[0]) if len(xs) else None
                if run:
                    points.append(((run[0] + run[1]) / 2, y))
        pts = np.array(points)
        slope, intercept = np.polyfit(pts[:, 1], pts[:, 0], 1)  # x = slope*y + b
        direction = np.array([-slope, -1.0])  # tip is up
        anchor = np.array([slope * pts[0, 1] + intercept, pts[0, 1]])
    else:
        for x0, x1 in spec["axis_cols"]:
            for x in range(x0, x1 + 1):
                col = opaque[:, x]
                run = first_run(col, 745)
                if run:
                    points.append((x, (run[0] + run[1]) / 2))
        pts = np.array(points)
        slope, intercept = np.polyfit(pts[:, 0], pts[:, 1], 1)  # y = slope*x + b
        direction = np.array([1.0, slope])  # tip is right
        anchor = np.array([pts[-1, 0], slope * pts[-1, 0] + intercept])
    direction /= np.linalg.norm(direction)
    if "axis_rows" in spec:
        residual = np.abs(slope * pts[:, 1] + intercept - pts[:, 0]).max()
    else:
        residual = np.abs(slope * pts[:, 0] + intercept - pts[:, 1]).max()
    return anchor, direction, residual


def flood(mask, seed):
    h, w = mask.shape
    seen = np.zeros_like(mask)
    x, y = seed
    queue = deque([(y, x)])
    seen[y, x] = True
    while queue:
        y, x = queue.popleft()
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                yy, xx = y + dy, x + dx
                if 0 <= yy < h and 0 <= xx < w and mask[yy, xx] and not seen[yy, xx]:
                    seen[yy, xx] = True
                    queue.append((yy, xx))
    return seen


def rgb_to_hsv(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx = rgb.max(-1)
    mn = rgb.min(-1)
    d = mx - mn
    h = np.zeros_like(mx)
    nz = d > 1e-6
    rm = nz & (mx == r)
    gm = nz & (mx == g) & ~rm
    bm = nz & ~rm & ~gm
    h[rm] = ((g - b)[rm] / d[rm]) % 6
    h[gm] = (b - r)[gm] / d[gm] + 2
    h[bm] = (r - g)[bm] / d[bm] + 4
    h /= 6
    s = np.where(mx > 1e-6, d / np.maximum(mx, 1e-6), 0)
    return np.stack([h, s, mx], -1)


def hsv_to_rgb(hsv):
    h, s, v = hsv[..., 0] * 6, hsv[..., 1], hsv[..., 2]
    i = np.floor(h).astype(int) % 6
    f = h - np.floor(h)
    p, q, t = v * (1 - s), v * (1 - s * f), v * (1 - s * (1 - f))
    table = [(v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q)]
    out = np.zeros(hsv.shape)
    for k, (r, g, b) in enumerate(table):
        sel = i == k
        out[..., 0][sel], out[..., 1][sel], out[..., 2][sel] = r[sel], g[sel], b[sel]
    return out


def hsv_stats(hsv, mask):
    h, s = hsv[..., 0][mask], hsv[..., 1][mask]
    ang = h * 2 * np.pi
    hue = (np.arctan2((np.sin(ang) * s).sum(), (np.cos(ang) * s).sum()) / (2 * np.pi)) % 1
    return hue, s.mean(), hsv[..., 2][mask].mean()


def shift_colour(rgb, mask, source, target):
    hsv = rgb_to_hsv(rgb)
    h0, s0, v0 = source
    h1, s1, v1 = target
    out = hsv.copy()
    out[..., 0] = np.where(mask, (hsv[..., 0] + h1 - h0) % 1, hsv[..., 0])
    out[..., 1] = np.where(mask, np.clip(hsv[..., 1] * s1 / s0, 0, 1), hsv[..., 1])
    out[..., 2] = np.where(mask, np.clip(hsv[..., 2] * v1 / v0, 0, 1), hsv[..., 2])
    return hsv_to_rgb(out)


def is_blue(rgb):
    return rgb[..., 2] > rgb[..., 0] + 0.2


def prepare_icon(path, wood_target, crystal_target):
    icon = np.asarray(Image.open(path).convert("RGBA")).astype(np.float64)
    alpha = icon[..., 3]
    star = flood(alpha > 0, ICON_STAR_SEED)
    alpha[star] = 0
    # The icon was keyed off a green background with a hard edge, so its
    # outermost opaque pixels carry green. After the colour shift that ring
    # turns into a pale halo round the crystal; drop it.
    for _ in range(ICON_ERODE):
        solid = alpha > 0
        inner = solid.copy()
        inner[1:, :] &= solid[:-1, :]
        inner[:-1, :] &= solid[1:, :]
        inner[:, 1:] &= solid[:, :-1]
        inner[:, :-1] &= solid[:, 1:]
        alpha[solid & ~inner] = 0

    opaque = alpha > 128
    points = []
    for y in range(935, 1024):
        xs = np.nonzero(opaque[y])[0]
        if len(xs):
            points.append(((xs[0] + xs[-1]) / 2, y))
    pts = np.array(points)
    slope, intercept = np.polyfit(pts[:, 1], pts[:, 0], 1)
    direction = np.array([-slope, -1.0])
    direction /= np.linalg.norm(direction)
    cut_point = np.array([slope * ICON_CUT_Y + intercept, ICON_CUT_Y])

    ys, xs = np.mgrid[0:alpha.shape[0], 0:alpha.shape[1]]
    along = (xs - cut_point[0]) * direction[0] + (ys - cut_point[1]) * direction[1]
    alpha *= np.clip(along / ICON_FEATHER, 0, 1)

    rgb = icon[..., :3] / 255
    solid = alpha > 200
    blue = is_blue(rgb)
    rgb = shift_colour(rgb, ~blue, hsv_stats(rgb_to_hsv(rgb), solid & ~blue), wood_target)
    rgb = shift_colour(rgb, blue, hsv_stats(rgb_to_hsv(rgb), solid & blue), crystal_target)

    crystal = (alpha > 200) & blue
    cys, cxs = np.nonzero(crystal)
    crystal_centre = np.array([(cxs.min() + cxs.max()) / 2, (cys.min() + cys.max()) / 2])
    rgba = np.dstack([np.clip(rgb * 255, 0, 255), alpha]).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA"), cut_point, direction, crystal_centre


def place_head(icon, icon_cut, icon_dir, icon_crystal, frame_dir, crystal_target, size, frame_anchor):
    """Scale, rotate and move the head so its shaft lies on the frame's staff axis."""
    scaled = icon.convert("RGBa").resize(
        (round(icon.width * SCALE), round(icon.height * SCALE)), Image.Resampling.LANCZOS)
    cut = icon_cut * SCALE
    crystal = icon_crystal * SCALE
    angle = np.arctan2(frame_dir[1], frame_dir[0]) - np.arctan2(icon_dir[1], icon_dir[0])
    cos, sin = np.cos(angle), np.sin(angle)
    rot = np.array([[cos, -sin], [sin, cos]])

    # The cut point goes on the frame axis; slide along the axis so the crystal
    # centre's projection lands on the old crystal's projection.
    crystal_rel = rot @ (crystal - cut)
    target_along = (np.array(crystal_target) - frame_anchor) @ frame_dir
    base = frame_anchor + (target_along - crystal_rel @ frame_dir) * frame_dir

    def render(origin):
        # Output pixel p maps to icon pixel rot^-1 (p - origin) + cut.
        inv = rot.T
        offset = cut - inv @ origin
        matrix = (inv[0, 0], inv[0, 1], offset[0], inv[1, 0], inv[1, 1], offset[1])
        img = scaled.transform(size, Image.Transform.AFFINE, matrix, Image.Resampling.BICUBIC)
        return img.convert("RGBA")

    head = render(base)
    bbox = head.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox()
    # Slide back along the axis while the curl leaves the canvas.
    shift = 0.0
    width, height = size
    while True:
        x0, y0, x1, y1 = bbox
        if x0 >= MARGIN and y0 >= MARGIN and x1 <= width - MARGIN and y1 <= height - MARGIN:
            break
        shift += 1
        head = render(base - shift * frame_dir)
        bbox = head.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox()
    origin = base - shift * frame_dir
    crystal_at = origin + crystal_rel
    return head, origin, crystal_at, shift, np.degrees(angle)


def sample_line(image, kind, ref, position):
    """Linear interpolation along one row or column, in premultiplied alpha."""
    line = (image[ref] if kind == "row" else image[:, ref]).astype(np.float64)
    premul = line.copy()
    premul[:, :3] *= line[:, 3:4] / 255
    position = np.clip(position, 0, len(line) - 1.001)
    left = np.floor(position).astype(int)
    frac = (position - left)[..., None]
    mixed = premul[left] * (1 - frac) + premul[left + 1] * frac
    alpha = mixed[..., 3:4]
    rgb = np.where(alpha > 0, mixed[..., :3] * 255 / np.maximum(alpha, 1e-6), 0)
    return np.clip(np.rint(np.concatenate([rgb, alpha], -1)), 0, 255).astype(np.uint8)


def run_length(line, centre):
    left, right = first_run(line, int(round(centre)))
    return right - left + 1


def extend_shaft(frame, spec, anchor, direction, stub_along, head_alpha):
    """Copy the plain cross-section `ref` forward along the axis up to the head's stub.

    The copy tapers from the old shaft's width at `ref` to the pasted stub's
    width, so the join has no step.
    """
    src = frame.copy()
    h, w = frame.shape[:2]
    kind, ref = spec["ref"]
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float64)
    along = (xs - anchor[0]) * direction[0] + (ys - anchor[1]) * direction[1]
    normal = np.array([-direction[1], direction[0]])
    across = (xs - anchor[0]) * normal[0] + (ys - anchor[1]) * normal[1]
    probe = anchor + (stub_along + 8) * direction
    if kind == "row":
        centre = anchor[0] + (ref - anchor[1]) * direction[0] / direction[1]
        ref_point = np.array([centre, ref])
        ref_width = run_length(src[ref, :, 3] > 128, centre)
        stub_width = run_length(head_alpha[int(round(probe[1])), :] > 128, probe[0])
        back = (ys - ref) / direction[1]  # distance back along the axis to the row
        position = xs - back * direction[0]
        before_ref = ys < ref
    else:
        centre = anchor[1] + (ref - anchor[0]) * direction[1] / direction[0]
        ref_point = np.array([ref, centre])
        ref_width = run_length(src[:, ref, 3] > 128, centre)
        stub_width = run_length(head_alpha[:, int(round(probe[0]))] > 128, probe[1])
        back = (xs - ref) / direction[0]
        position = ys - back * direction[1]
        before_ref = xs > ref
    along_ref = (ref_point - anchor) @ direction
    share = np.clip((along - along_ref) / (stub_along - along_ref), 0, 1)
    width = ref_width + (stub_width - ref_width) * share
    position = centre + (position - centre) * ref_width / width
    region = before_ref & (along <= stub_along + 10) & (np.abs(across) < 30)
    sampled = sample_line(src, kind, ref, position)
    # Write the region whole, transparent samples included: between the cut and
    # `ref` the old shaft is wider than the taper and would otherwise poke out.
    frame[region] = sampled[region]
    take = region & (sampled[..., 3] > 0)
    print(f"  shaft {ref_width}px at {kind} {ref} tapers to the stub's {stub_width}px")
    return take


def refill_base_hair(frame, original):
    """Replace the prong pixels right of the Base hair edge with the hair beside them."""
    top, bottom = BASE_MERGED_ROWS
    changed = 0
    for y in range(top, bottom + 1):
        edge = np.interp(y, *zip(*BASE_HAIR_EDGE))
        x = int(np.floor(edge))
        right = x
        while right < 260 and original[y, right, 0] > BASE_PRONG_RED:
            right += 1
        if right == x:
            continue
        source = original[y, right + 2]
        frame[y, x:right + 2] = source
        changed += right + 2 - x
    return changed


def main():
    icon_path, frames_dir, out_dir = map(Path, sys.argv[1:4])
    out_dir.mkdir(parents=True, exist_ok=True)

    base = np.asarray(Image.open(frames_dir / "PlayerCharacterBase.png").convert("RGBA")).astype(np.float64)
    old_head = remove_mask("PlayerCharacterBase.png", base[..., 3]) & (base[..., 3] > 200)
    rgb = base[..., :3] / 255
    blue = is_blue(rgb)
    hsv = rgb_to_hsv(rgb)
    wood_target = hsv_stats(hsv, old_head & ~blue)
    crystal_target = hsv_stats(hsv, old_head & blue)

    icon, icon_cut, icon_dir, icon_crystal = prepare_icon(icon_path, wood_target, crystal_target)

    for name, spec in FRAMES.items():
        original = np.asarray(Image.open(frames_dir / name).convert("RGBA"))
        frame = original.copy()
        size = (frame.shape[1], frame.shape[0])
        anchor, direction, residual = fit_axis(original[..., 3], spec)

        remove = remove_mask(name, original[..., 3])
        frame[remove] = 0
        if name == "PlayerCharacterBase.png":
            refill_base_hair(frame, original)

        head, origin, crystal_at, shift, angle = place_head(
            icon, icon_cut, icon_dir, icon_crystal, direction, spec["crystal"], size, anchor)
        stub_along = (origin - anchor) @ direction
        extended = extend_shaft(frame, spec, anchor, direction, stub_along,
                                np.asarray(head.getchannel("A")))

        out = Image.fromarray(frame, "RGBA")
        out.alpha_composite(head)
        out.save(out_dir / name, optimize=True)
        moved = np.array(crystal_at) - np.array(spec["crystal"])
        print(f"{name}: axis residual {residual:.1f}px, rotate {angle:.1f} deg, "
              f"slid back {shift:.0f}px, crystal {tuple(np.round(crystal_at, 1))} "
              f"(moved {tuple(np.round(moved, 1))} px), shaft extended {int(extended.sum())} px")


if __name__ == "__main__":
    main()
