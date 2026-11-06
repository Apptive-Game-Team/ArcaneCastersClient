"""생성된 붉은 눈만 원본 눈 영역에 맞추고 원본 몸체와 alpha를 보존한다."""
import io
import json
import subprocess
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
BATCH = Path(__file__).resolve().parent
SPRITES = ROOT / 'Assets/Resources/Game/sprites'
BASE = '38840beb'
EYES = {
    'MiniRockSwarm': [(136, 45, 149, 75), (168, 44, 180, 73)],
    'RockGolem': [(144, 54, 151, 70), (159, 53, 166, 69)],
    'RockGolem2': [(122, 48, 130, 64), (137, 46, 144, 62)],
    'RockMage': [(123, 80, 136, 106), (154, 79, 164, 104)],
    'WallGolem': [(125, 20, 132, 34), (137, 19, 144, 33)],
}


def original(name):
    data = subprocess.check_output(['git', 'show', f'{BASE}:Assets/Resources/Game/sprites/{name}.png'], cwd=ROOT)
    return Image.open(io.BytesIO(data)).convert('RGBA')


def components(points):
    result = []
    while points:
        queue = [points.pop()]
        for x, y in queue:
            for point in [(x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)]:
                if point in points:
                    points.remove(point)
                    queue.append(point)
        result.append(queue)
    return result


def bounds(points):
    xs, ys = zip(*points)
    return min(xs), min(ys), max(xs) + 1, max(ys) + 1


def generated_eyes(name, old):
    selected = [BATCH / 'eyes' / f'{name}-{index}.png' for index in range(2)]
    if all(path.exists() for path in selected):
        return [Image.open(path).convert('RGB') for path in selected]
    source = Image.open(BATCH / 'generated' / f'{name}.png').convert('RGBA')
    sx, sy = source.width / old.width, source.height / old.height
    boxes = EYES[name]
    region = (int((boxes[0][0] - 12) * sx), int((min(b[1] for b in boxes) - 10) * sy),
              int((boxes[1][2] + 12) * sx), int((max(b[3] for b in boxes) + 10) * sy))
    pixels = source.load()
    red = set()
    for y in range(max(0, region[1]), min(source.height, region[3])):
        for x in range(max(0, region[0]), min(source.width, region[2])):
            r, g, b, a = pixels[x, y]
            if a > 200 and r > 70 and r > 1.8 * g and r > 1.8 * b:
                red.add((x, y))
    groups = sorted(components(red), key=len, reverse=True)[:2]
    assert len(groups) == 2 and min(map(len, groups)) > 15, name
    (BATCH / 'eyes').mkdir(exist_ok=True)
    patches = []
    for path, box in zip(selected, sorted([bounds(g) for g in groups])):
        patch = source.crop(box).convert('RGB')
        patch.save(path, optimize=True)
        patches.append(patch)
    return patches


def eye_mask(old, box):
    mask = Image.new('L', old.size)
    p, m = old.load(), mask.load()
    x0, y0, x1, y1 = box
    for y in range(y0, y1):
        dark = [x for x in range(x0, x1) if max(p[x, y][:3]) < 135 and p[x, y][3] > 200]
        if dark:
            for x in range(min(dark), max(dark) + 1):
                m[x, y] = 255
    assert mask.getbbox(), box
    return mask


def finalize():
    report = []
    for name, boxes in EYES.items():
        old = original(name)
        new = old.copy()
        patches = generated_eyes(name, old)
        allowed = Image.new('L', old.size)
        for box, source_patch in zip(boxes, patches):
            mask = eye_mask(old, box)
            target = mask.getbbox()
            patch = source_patch.resize((target[2] - target[0], target[3] - target[1]), Image.Resampling.LANCZOS)
            layer = old.copy()
            layer.paste(patch, target[:2])
            new.paste(layer, (0, 0), mask)
            allowed = ImageChops.lighter(allowed, mask)
        new.putalpha(old.getchannel('A'))
        diff = ImageChops.difference(old, new)
        assert not ImageChops.multiply(diff.convert('RGB'), ImageChops.invert(allowed).convert('RGB')).getbbox(), name
        assert old.getchannel('A').tobytes() == new.getchannel('A').tobytes(), name
        assert old.size == new.size and old.getbbox() == new.getbbox(), name
        changed = sum(a != b for a, b in zip(old.get_flattened_data(), new.get_flattened_data()))
        assert changed > 10, name
        new.save(SPRITES / f'{name}.png', optimize=True)
        report.append({'name': name, 'canvas': old.size, 'alpha_bbox': old.getchannel('A').getbbox(),
                       'changed_pixels': changed, 'eye_regions': boxes, 'alpha_identical': True, 'outside_eyes_identical': True})
    (BATCH / 'validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report, indent=2))


def sheets():
    media = ROOT / 'docs/pr-media/423'
    media.mkdir(parents=True, exist_ok=True)
    sheet = Image.new('RGB', (1080, 640), '#22425f')
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 17)
    draw.text((16, 12), 'Before / After - original pixels, only eyes changed', font=font, fill='white')
    for col, name in enumerate(EYES):
        x = col * 216
        draw.text((x + 8, 44), name, font=font, fill='white')
        for row, im in enumerate([original(name), Image.open(SPRITES / f'{name}.png').convert('RGBA')]):
            thumb = im.copy()
            thumb.thumbnail((200, 250))
            sheet.paste(thumb, (x + (216 - thumb.width) // 2, 78 + row * 270 + 250 - thumb.height), thumb)
    sheet.quantize(colors=192).save(media / 'golems-before-after.png', optimize=True)
    small = Image.new('RGB', (1080, 230), '#22425f')
    d = ImageDraw.Draw(small)
    d.text((16, 8), '64px readability - before (top), after (bottom)', font=font, fill='white')
    for col, name in enumerate(EYES):
        x = col * 216
        d.text((x + 8, 42), name, font=font, fill='white')
        for row, im in enumerate([original(name), Image.open(SPRITES / f'{name}.png').convert('RGBA')]):
            im.thumbnail((64, 64))
            small.paste(im, (x + (216 - im.width) // 2, 76 + row * 76), im)
    small.save(media / 'golems-64px.png', optimize=True)
    paths = sorted(SPRITES.glob('*.png'))
    all_sprites = Image.new('RGB', (8 * 144, ((len(paths) + 7) // 8) * 152), '#303030')
    d = ImageDraw.Draw(all_sprites)
    for index, path in enumerate(paths):
        im = Image.open(path).convert('RGBA')
        im.thumbnail((128, 128))
        x, y = index % 8 * 144, index // 8 * 152
        all_sprites.paste(im, (x + (144 - im.width) // 2, y + 128 - im.height), im)
        d.text((x + 2, y + 131), path.stem[:20], fill='white')
    (BATCH / 'review').mkdir(exist_ok=True)
    all_sprites.save(BATCH / 'review/sprites.png', optimize=True)


if __name__ == '__main__':
    finalize()
    sheets()
