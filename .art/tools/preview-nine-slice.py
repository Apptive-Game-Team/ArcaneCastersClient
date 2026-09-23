#!/usr/bin/env python3
"""Draw a 9-slice sprite the way a Unity UI Image of type Sliced draws it.

Unity is not available on every machine that edits this project, so this is how
a new button or panel sprite gets checked before it ships. The drawn border is
`border / pixelsPerUnitMultiplier` canvas units (sprite PPU 100, canvas reference
PPU 100). When a rect is narrower or shorter than its two borders, Unity scales
both borders on that axis down to fit; this does the same.

The `Shadow` component on `UI-Base.prefab` is drawn too: a black copy of the
mesh at the given offset and alpha, behind the sprite.

Run it directly for the standard button sheet (real button sizes and labels from
the lobby and login scenes), or import `button()` to compose other layouts:

  preview-nine-slice.py SPRITE --border L B R T --multiplier 3 --out sheet.png \
      [--shadow-y -4 --shadow-alpha 0.5] [--margin 18 5 18 11]

Run it from the repository root; the label font is read from `Assets/Art/Fonts`.
"""
import argparse

import numpy as np
from PIL import Image, ImageDraw, ImageFont

FONT = "Assets/Art/Fonts/Pretendard-Regular.otf"


def slice_draw(sprite, border, multiplier, size, zoom):
    """Return an RGBA image of `sprite` sliced into `size` canvas units at
    `zoom` pixels per canvas unit."""
    left, bottom, right, top = border
    sw, sh = sprite.size
    w, h = round(size[0] * zoom), round(size[1] * zoom)
    k = zoom / multiplier
    dl, dr, dt, db = left * k, right * k, top * k, bottom * k
    if dl + dr > w:
        f = w / (dl + dr)
        dl, dr = dl * f, dr * f
    if dt + db > h:
        f = h / (dt + db)
        dt, db = dt * f, db * f
    xs_src = [0, left, sw - right, sw]
    ys_src = [0, top, sh - bottom, sh]
    xs_dst = [0, round(dl), w - round(dr), w]
    ys_dst = [0, round(dt), h - round(db), h]
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    src = sprite.convert("RGBa")
    for i in range(3):
        for j in range(3):
            box = (xs_src[i], ys_src[j], xs_src[i + 1], ys_src[j + 1])
            dw = xs_dst[i + 1] - xs_dst[i]
            dh = ys_dst[j + 1] - ys_dst[j]
            if dw <= 0 or dh <= 0 or box[2] <= box[0] or box[3] <= box[1]:
                continue
            piece = src.crop(box).resize((dw, dh), Image.BICUBIC).convert("RGBA")
            out.alpha_composite(piece, (xs_dst[i], ys_dst[j]))
    return out


def with_shadow(img, offset, alpha, zoom):
    dx, dy = round(offset[0] * zoom), round(-offset[1] * zoom)
    a = np.array(img)
    shadow = np.zeros_like(a)
    shadow[:, :, 3] = (a[:, :, 3].astype(np.float32) * alpha).astype(np.uint8)
    pad = max(abs(dx), abs(dy))
    canvas = Image.new("RGBA", (img.width + 2 * pad, img.height + 2 * pad), (0, 0, 0, 0))
    canvas.alpha_composite(Image.fromarray(shadow, "RGBA"), (pad + dx, pad + dy))
    canvas.alpha_composite(img, (pad, pad))
    return canvas, pad


def draw_label(img, text, colour, zoom, margin=(10, 10, 10, 10), max_size=72, min_size=12, font=FONT):
    """Approximate TextMeshPro auto-size: the largest size that fits inside
    the rect minus the margin, clamped to [min_size, max_size]. `margin` is
    TextMeshPro's (left, top, right, bottom)."""
    d = ImageDraw.Draw(img)
    ml, mt, mr, mb = (m * zoom for m in margin)
    box_w = img.width - ml - mr
    box_h = img.height - mt - mb
    size = max_size * zoom
    while size > min_size * zoom:
        f = ImageFont.truetype(font, round(size))
        l, t, r, b = d.textbbox((0, 0), text, font=f)
        if r - l <= box_w and (f.size * 1.2) <= box_h:
            break
        size -= 0.5 * zoom
    f = ImageFont.truetype(font, round(size))
    l, t, r, b = d.textbbox((0, 0), text, font=f)
    x = ml + (box_w - (r - l)) / 2 - l
    y = mt + (box_h - (b - t)) / 2 - t
    d.text((x, y), text, font=f, fill=colour)
    return img


def button(sprite, border, multiplier, size, zoom, text, colour, shadow, margin=(10, 10, 10, 10)):
    img = slice_draw(sprite, border, multiplier, size, zoom)
    draw_label(img, text, colour, zoom, margin)
    if shadow:
        return with_shadow(img, shadow[0], shadow[1], zoom)
    return img, 0


def main():
    p = argparse.ArgumentParser()
    p.add_argument("sprite")
    p.add_argument("--border", type=int, nargs=4, required=True, metavar=("L", "B", "R", "T"))
    p.add_argument("--multiplier", type=float, required=True)
    p.add_argument("--out", required=True)
    p.add_argument("--zoom", type=float, default=2.4)
    p.add_argument("--colour", default="#3A2616")
    p.add_argument("--shadow-y", type=float, default=-10)
    p.add_argument("--shadow-alpha", type=float, default=0.5)
    p.add_argument("--margin", type=float, nargs=4, default=(10, 10, 10, 10), metavar=("L", "T", "R", "B"))
    args = p.parse_args()
    sprite = Image.open(args.sprite).convert("RGBA")
    rows = [
        [((200, 50), "Match Queue"), ((200, 50), "Practice Mode"), ((200, 50), "매칭 시작")],
        [((150, 50), "Magic Book"), ((150, 50), "덱 수정하기"), ((100, 50), "Menu"), ((100, 50), "메뉴")],
        [((320, 60), "Practice Mode"), ((250, 40), "로그인"), ((100, 30), "English")],
    ]
    z = args.zoom
    shadow = ((0, args.shadow_y), args.shadow_alpha) if args.shadow_alpha > 0 else None
    tiles = []
    for row in rows:
        tiles.append([button(sprite, args.border, args.multiplier, s, z, t, args.colour, shadow, args.margin)[0]
                      for s, t in row])
    gap = 24
    width = max(sum(t.width for t in r) + gap * (len(r) + 1) for r in tiles)
    height = sum(max(t.height for t in r) for r in tiles) + gap * (len(tiles) + 1)
    sheet = Image.new("RGBA", (width, height), (58, 92, 52, 255))
    y = gap
    for r in tiles:
        x = gap
        for t in r:
            sheet.alpha_composite(t, (x, y))
            x += t.width + gap
        y += max(t.height for t in r) + gap
    sheet.convert("RGB").save(args.out)


if __name__ == "__main__":
    main()
