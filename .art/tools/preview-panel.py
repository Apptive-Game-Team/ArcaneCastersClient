#!/usr/bin/env python3
"""Draw WoodFramePanel / WoodInputSlot 9-slice previews the way Unity draws a
Sliced Image, reusing `slice_draw` from preview-nine-slice.py.

Panels get a WoodPlankButton on top (as a dialog would use one) and one text
line inside the parchment face. Input slots get one placeholder text line
sitting on the recessed floor.

Run from the repository root:
  preview-panel.py panel SPRITE --border L B R T --multiplier 3 \
      --sizes 500x400 700x500 --out .art/concept/wood-ui-2/preview-panel.png
  preview-panel.py slot SPRITE --border L B R T --multiplier 3 \
      --sizes 300x50 400x60 --colour "#3A2616" \
      --out .art/concept/wood-ui-2/preview-slot.png
"""
import argparse

from PIL import Image

from importlib import util as _util
import pathlib

_spec = _util.spec_from_file_location(
    "preview_nine_slice", pathlib.Path(__file__).with_name("preview-nine-slice.py"))
_pns = _util.module_from_spec(_spec)
_spec.loader.exec_module(_pns)

BUTTON_SPRITE = "Assets/Art/Images/UI/WoodPlankButton.png"
BUTTON_BORDER = (47, 86, 48, 45)
BUTTON_MULTIPLIER = 3


def panel_tile(sprite, border, multiplier, size, zoom, text, colour, margin):
    img = _pns.slice_draw(sprite, border, multiplier, size, zoom)
    button_sprite = Image.open(BUTTON_SPRITE).convert("RGBA")
    btn = _pns.slice_draw(button_sprite, BUTTON_BORDER, BUTTON_MULTIPLIER, (140, 44), zoom)
    img.alpha_composite(btn, (round((img.width - btn.width) / 2), round(img.height * 0.6)))
    _pns.draw_label(img, text, colour, zoom, margin, max_size=40)
    return img


def slot_tile(sprite, border, multiplier, size, zoom, text, colour, margin):
    img = _pns.slice_draw(sprite, border, multiplier, size, zoom)
    _pns.draw_label(img, text, colour, zoom, margin, max_size=32)
    return img


def sheet(tiles, out, bg=(58, 92, 52, 255)):
    gap = 24
    width = sum(t.width for t in tiles) + gap * (len(tiles) + 1)
    height = max(t.height for t in tiles) + gap * 2
    canvas = Image.new("RGBA", (width, height), bg)
    x = gap
    for t in tiles:
        canvas.alpha_composite(t, (x, gap))
        x += t.width + gap
    canvas.convert("RGB").save(out)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("kind", choices=("panel", "slot"))
    p.add_argument("sprite")
    p.add_argument("--border", type=int, nargs=4, required=True, metavar=("L", "B", "R", "T"))
    p.add_argument("--multiplier", type=float, required=True)
    p.add_argument("--sizes", nargs="+", required=True, help="WxH canvas units, e.g. 500x400")
    p.add_argument("--out", required=True)
    p.add_argument("--zoom", type=float, default=1.6)
    p.add_argument("--colour", default="#3A2616")
    p.add_argument("--margin", type=float, nargs=4, default=(24, 24, 24, 24), metavar=("L", "T", "R", "B"))
    p.add_argument("--text", default="설정 / Settings")
    args = p.parse_args()

    sprite = Image.open(args.sprite).convert("RGBA")
    sizes = [tuple(int(v) for v in s.split("x")) for s in args.sizes]
    tiles = []
    for size in sizes:
        if args.kind == "panel":
            t = panel_tile(sprite, args.border, args.multiplier, size, args.zoom,
                            args.text, args.colour, args.margin)
        else:
            t = slot_tile(sprite, args.border, args.multiplier, size, args.zoom,
                           args.text, args.colour, args.margin)
        tiles.append(t)
    sheet(tiles, args.out)


if __name__ == "__main__":
    main()
