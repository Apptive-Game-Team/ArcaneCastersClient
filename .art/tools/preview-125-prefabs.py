#!/usr/bin/env python3
"""Preview sheet for issue #125's panel/input/dropdown/toggle/scrollbar restyle.

Composes, at real prefab sizes on an 800x450-reference canvas (multiplier
column from unity-ui-prefabs SKILL.md): a ConfirmPage-sized wood panel with a
plank button, JoinPanal's input fields, the SortDropdown closed and open, a
toggle, a scrollbar, Panal.prefab's KoreanButton/EnglishButton, and a sound
slider. Run from the repository root.
"""
import importlib.util

spec = importlib.util.spec_from_file_location("preview_nine_slice", ".art/tools/preview-nine-slice.py")
_pns = importlib.util.module_from_spec(spec)
spec.loader.exec_module(_pns)
slice_draw, draw_label, with_shadow, button = _pns.slice_draw, _pns.draw_label, _pns.with_shadow, _pns.button

from PIL import Image, ImageDraw, ImageFont

PLANK = "Assets/Art/Images/UI/WoodPlankButton.png"
PANEL = "Assets/Art/Images/UI/WoodFramePanel.png"
SLOT = "Assets/Art/Images/UI/WoodInputSlot.png"
FONT = "Assets/Art/Fonts/Pretendard-Regular.otf"

PLANK_BORDER = (47, 86, 48, 45)
PANEL_BORDER = (43, 47, 43, 49)
SLOT_BORDER = (151, 65, 151, 66)

DARK = "#3A2616"
PLACEHOLDER = "#6B5136"
ZOOM = 2.0

plank_img = Image.open(PLANK).convert("RGBA")
panel_img = Image.open(PANEL).convert("RGBA")
slot_img = Image.open(SLOT).convert("RGBA")


def label(w, h, zoom, text, colour, align="center"):
    img = Image.new("RGBA", (round(w * zoom), round(h * zoom)), (0, 0, 0, 0))
    draw_label(img, text, colour, zoom, margin=(4, 2, 4, 2), max_size=16, min_size=8)
    return img


def paste(sheet, img, xy):
    sheet.alpha_composite(img, (round(xy[0]), round(xy[1])))


def section_title(sheet, xy, text):
    d = ImageDraw.Draw(sheet)
    f = ImageFont.truetype(FONT, 18)
    d.text(xy, text, font=f, fill="#EDEDED")


# --- 1. ConfirmPage-sized panel (500x400, panel mult 1) with a plank button ---
panel_tile = slice_draw(panel_img, PANEL_BORDER, 1, (500, 400), ZOOM)
btn_tile, _ = button(plank_img, PLANK_BORDER, 3, (200, 50), ZOOM, "확인", DARK,
                      ((0, -4), 0.5), margin=(18, 5, 18, 11))
paste(panel_tile, btn_tile, (panel_tile.width / 2 - btn_tile.width / 2,
                              panel_tile.height / 2 - btn_tile.height / 2))

# --- 2. JoinPanal input fields (300x40, slot mult 6) ---
def input_field(w, h, mult, text, colour, zoom):
    img = slice_draw(slot_img, SLOT_BORDER, mult, (w, h), zoom)
    draw_label(img, text, colour, zoom, margin=(151 / mult + 2, 66 / mult, 151 / mult + 2, 66 / mult),
               max_size=16, min_size=8)
    return img


name_field = input_field(300, 40, 6, "Enter Name...", PLACEHOLDER, ZOOM)
email_field = input_field(300, 40, 6, "player@example.com", DARK, ZOOM)
password_field = input_field(300, 40, 6, "********", DARK, ZOOM)

fields_tile = Image.new("RGBA", (name_field.width, name_field.height * 3 + round(12 * ZOOM)), (0, 0, 0, 0))
for i, f in enumerate((name_field, email_field, password_field)):
    paste(fields_tile, f, (0, i * (f.height + round(6 * ZOOM))))

# --- 3. Dropdown closed (caption 95x36, plank mult 6) and open (template 95x150, panel mult 1) ---
caption = slice_draw(plank_img, PLANK_BORDER, 6, (95, 36), ZOOM)
draw_label(caption, "이름", DARK, ZOOM, margin=(6, 4, 20, 4), max_size=14, min_size=8)
d = ImageDraw.Draw(caption)
ax = caption.width - round(16 * ZOOM)
ay = caption.height / 2
d.polygon([(ax - 5 * ZOOM, ay - 4 * ZOOM), (ax + 5 * ZOOM, ay - 4 * ZOOM), (ax, ay + 4 * ZOOM)], fill=DARK)

template = slice_draw(panel_img, PANEL_BORDER, 2, (95, 150), ZOOM)
row_h = round(20 * ZOOM)
items = ["기본덱", "이름", "공격력"]
for i, text in enumerate(items):
    row = Image.new("RGBA", (template.width - round(10 * ZOOM), row_h), (0, 0, 0, 0))
    if i == 0:
        d2 = ImageDraw.Draw(row)
        d2.rectangle([0, 0, row.width, row.height], fill=(201, 169, 113, 153))
    draw_label(row, text, DARK, ZOOM, margin=(6, 2, 6, 2), max_size=13, min_size=8)
    paste(template, row, (round(5 * ZOOM), round(4 * ZOOM) + i * row_h))

dropdown_tile = Image.new("RGBA", (max(caption.width, template.width), caption.height + round(6 * ZOOM) + template.height), (0, 0, 0, 0))
paste(dropdown_tile, caption, (0, 0))
paste(dropdown_tile, template, (0, caption.height + round(6 * ZOOM)))

# --- 4. Toggle (20x20 slot background mult 6, checkmark tinted dark) ---
toggle_bg = slice_draw(slot_img, SLOT_BORDER, 6, (20, 20), ZOOM)
d3 = ImageDraw.Draw(toggle_bg)
pad = round(5 * ZOOM)
d3.rectangle([pad, pad, toggle_bg.width - pad, toggle_bg.height - pad], fill=DARK)
toggle_tile = Image.new("RGBA", (toggle_bg.width + round(80 * ZOOM), toggle_bg.height), (0, 0, 0, 0))
paste(toggle_tile, toggle_bg, (0, 0))
lbl = label(80, 20, ZOOM, "코치 힌트", "#EDEDED")
paste(toggle_tile, lbl, (toggle_bg.width + round(4 * ZOOM), (toggle_bg.height - lbl.height) / 2))

# --- 5. Scrollbar (track 20x150 slot mult 30, handle 20x40 plank mult 30) ---
track = slice_draw(slot_img, SLOT_BORDER, 30, (20, 150), ZOOM)
handle = slice_draw(plank_img, PLANK_BORDER, 30, (20, 40), ZOOM)
scrollbar_tile = track.copy()
paste(scrollbar_tile, handle, (0, round(10 * ZOOM)))

# --- 6. Panal.prefab KoreanButton/EnglishButton (100x40, plank mult 3) ---
korean_btn, _ = button(plank_img, PLANK_BORDER, 3, (100, 40), ZOOM, "한국어", DARK,
                        ((0, -4), 0.5), margin=(18, 5, 18, 11))
english_btn, _ = button(plank_img, PLANK_BORDER, 3, (100, 40), ZOOM, "English", DARK,
                         ((0, -4), 0.5), margin=(18, 5, 18, 11))
lang_gap = round(10 * ZOOM)
lang_tile = Image.new("RGBA", (korean_btn.width + lang_gap + english_btn.width,
                                max(korean_btn.height, english_btn.height)), (0, 0, 0, 0))
paste(lang_tile, korean_btn, (0, 0))
paste(lang_tile, english_btn, (korean_btn.width + lang_gap, 0))

# --- 7. Sound slider (300x20: background 300x10 slot mult 15, fill wood-tone,
#        handle 20x20 plank mult 30) ---
FILL_COLOUR = "#C08C56"
slider_bg = slice_draw(slot_img, SLOT_BORDER, 15, (300, 10), ZOOM)
fill_w = round(300 * 0.6 * ZOOM)
slider_fill_tile = Image.new("RGBA", (fill_w, round(10 * ZOOM)), FILL_COLOUR)
slider_handle = slice_draw(plank_img, PLANK_BORDER, 30, (20, 20), ZOOM)

slider_tile = Image.new("RGBA", (round(300 * ZOOM), round(20 * ZOOM)), (0, 0, 0, 0))
paste(slider_tile, slider_bg, (0, round(5 * ZOOM)))
paste(slider_tile, slider_fill_tile, (0, round(5 * ZOOM)))
paste(slider_tile, slider_handle, (fill_w - round(10 * ZOOM), 0))

# --- compose sheet ---
gap = 30
col1_w = panel_tile.width
col2_w = max(fields_tile.width, dropdown_tile.width, toggle_tile.width, scrollbar_tile.width)
col3_w = max(lang_tile.width, slider_tile.width)
width = gap * 4 + col1_w + col2_w + col3_w
height = gap * 2 + max(
    panel_tile.height,
    fields_tile.height + dropdown_tile.height + toggle_tile.height + scrollbar_tile.height + gap * 3,
    lang_tile.height + slider_tile.height + gap,
)
sheet = Image.new("RGBA", (round(width), round(height)), (30, 33, 36, 255))

section_title(sheet, (gap, gap - 24), "ConfirmPage-sized panel + plank button")
paste(sheet, panel_tile, (gap, gap))

x2 = gap * 2 + col1_w
y = gap
section_title(sheet, (x2, y - 24), "JoinPanal input fields")
paste(sheet, fields_tile, (x2, y))
y += fields_tile.height + gap
section_title(sheet, (x2, y - 24), "Dropdown: closed + open")
paste(sheet, dropdown_tile, (x2, y))
y += dropdown_tile.height + gap
section_title(sheet, (x2, y - 24), "Toggle")
paste(sheet, toggle_tile, (x2, y))
y += toggle_tile.height + gap
section_title(sheet, (x2, y - 24), "Scrollbar")
paste(sheet, scrollbar_tile, (x2, y))

x3 = gap * 3 + col1_w + col2_w
y = gap
section_title(sheet, (x3, y - 24), "Panal.prefab language buttons")
paste(sheet, lang_tile, (x3, y))
y += lang_tile.height + gap
section_title(sheet, (x3, y - 24), "Sound slider")
paste(sheet, slider_tile, (x3, y))

sheet.convert("RGB").save(".art/concept/wood-ui-2/preview-prefabs.png")
print("saved .art/concept/wood-ui-2/preview-prefabs.png", sheet.size)
