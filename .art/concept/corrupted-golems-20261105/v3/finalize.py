"""타락 강화 생성물을 기존 Unity 캔버스와 접지 영역에 맞춘다."""
import io
import json
import subprocess
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
SPRITES = ROOT / 'Assets/Resources/Game/sprites'
NAMES = ['MiniRockSwarm', 'RockGolem', 'RockGolem2', 'RockMage', 'WallGolem']
PREVIOUS = 'bfca3fa9'


def old_image(name):
    data = subprocess.check_output(['git', 'show', f'{PREVIOUS}:Assets/Resources/Game/sprites/{name}.png'], cwd=ROOT)
    return Image.open(io.BytesIO(data)).convert('RGBA')


def normalize():
    (HERE / 'sources').mkdir(exist_ok=True)
    report = []
    for name in NAMES:
        source_path = HERE / 'sources' / f'{name}.png'
        if not source_path.exists():
            raw = Image.open(HERE / 'generated' / f'{name}.png').convert('RGBA')
            # 반투명 경계 밖 생성 찌꺼기를 제외한 본체를 선택한다.
            box = raw.getchannel('A').point(lambda a: 255 if a > 128 else 0).getbbox()
            source = raw.crop(box)
            source.thumbnail((512, 512), Image.Resampling.LANCZOS)
            source.save(source_path, optimize=True)
        source = Image.open(source_path).convert('RGBA')
        old = old_image(name)
        target = old.getchannel('A').getbbox()
        width, height = target[2] - target[0], target[3] - target[1]
        ratio_change = (source.width / source.height) / (width / height)
        assert abs(ratio_change - 1) < 0.08, (name, ratio_change)
        fitted = source.resize((width, height), Image.Resampling.LANCZOS)
        # 축소 필터의 미세한 반투명 꼬리를 제거한다.
        fitted.putalpha(fitted.getchannel('A').point(lambda a: 0 if a < 8 else a))
        result = Image.new('RGBA', old.size)
        result.paste(fitted, target[:2])
        result.save(SPRITES / f'{name}.png', optimize=True)
        bbox = result.getchannel('A').getbbox()
        bottom_before = old.height - target[3]
        bottom_after = result.height - bbox[3]
        assert result.size == old.size
        assert abs(bottom_before - bottom_after) <= 1, name
        assert all(abs(a - b) <= 1 for a, b in zip(target, bbox)), name
        assert all(result.getpixel(p)[3] == 0 for p in [(0, 0), (old.width - 1, 0), (0, old.height - 1), (old.width - 1, old.height - 1)])
        meta = f'Assets/Resources/Game/sprites/{name}.png.meta'
        assert (ROOT / meta).read_bytes() == subprocess.check_output(['git', 'show', f'{PREVIOUS}:{meta}'], cwd=ROOT)
        report.append({'name': name, 'canvas': result.size, 'old_bbox': target, 'new_bbox': bbox,
                       'bottom_before': bottom_before, 'bottom_after': bottom_after,
                       'source_aspect_change': round(ratio_change - 1, 4), 'meta_identical': True})
    (HERE / 'validation.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(report, indent=2))


def compare():
    media = ROOT / 'docs/pr-media/423'
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 17)
    for label, size, gap, path in [('Corruption v1 / partial v3', 210, 260, 'golems-partial-corruption-v3.png'),
                                    ('Corruption v1 / partial v3 - 64px', 64, 90, 'golems-partial-corruption-v3-64px.png')]:
        sheet = Image.new('RGB', (1080, 80 + gap * 2), '#22425f')
        d = ImageDraw.Draw(sheet)
        d.text((16, 10), label + ' - before (top), after (bottom)', font=font, fill='white')
        for col, name in enumerate(NAMES):
            x = col * 216
            d.text((x + 6, 44), name, font=font, fill='white')
            for row, im in enumerate([old_image(name), Image.open(SPRITES / f'{name}.png').convert('RGBA')]):
                im.thumbnail((size, size))
                sheet.paste(im, (x + (216 - im.width) // 2, 76 + row * gap + size - im.height), im)
        sheet.quantize(colors=192).save(media / path, optimize=True)


if __name__ == '__main__':
    normalize()
    compare()
