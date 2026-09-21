#!/usr/bin/env python3
"""새로 그린 아트의 색을 기준 이미지의 색 통계에 맞춘다.

    python3 .art/tools/match-colour-to.py <reference.png> <new.png> <out.png>

형태는 건드리지 않고 색조/채도/명도의 평균만 기준 이미지 쪽으로 옮긴다.
스타일만 바꾸고 색감은 기존 아트를 유지하고 싶을 때 쓴다 — 이미지를 다시
생성하면 형태까지 같이 바뀌어 버리므로, 색만 맞추는 편이 통제하기 쉽다.

색조는 채도로 가중한 원형 평균을 쓴다. 단순 평균을 쓰면 회색 픽셀이 색조를
끌어당겨, 돌처럼 무채색이 많은 스프라이트에서 엉뚱한 값이 나온다.

alpha 는 그대로 보존한다. 불투명 이미지는 자동으로 감지한다.
"""

import sys

import numpy as np
from PIL import Image
import matplotlib.colors as mc


def _stats(hsv, mask):
    h, s = hsv[:, :, 0][mask], hsv[:, :, 1][mask]
    ang = h * 2 * np.pi
    hue = (np.arctan2((np.sin(ang) * s).sum(), (np.cos(ang) * s).sum()) / (2 * np.pi)) % 1.0
    return hue, s.mean(), hsv[:, :, 2][mask].mean()


def _load(path):
    im = Image.open(path)
    rgba = im.mode in ("RGBA", "LA") or "transparency" in im.info
    a = np.asarray(im.convert("RGBA" if rgba else "RGB")).astype(np.float32)
    rgb = (a[:, :, :3] if rgba else a) / 255.0
    alpha = a[:, :, 3] if rgba else None
    mask = (alpha > 128) if rgba else np.ones(rgb.shape[:2], bool)
    return rgb, alpha, mask


def match(ref_path, src_path, out_path):
    rgb, alpha, mask = _load(src_path)
    rrgb, _, rmask = _load(ref_path)
    hsv = mc.rgb_to_hsv(rgb)
    h0, s0, v0 = _stats(hsv, mask)
    h1, s1, v1 = _stats(mc.rgb_to_hsv(rrgb), rmask)
    hsv[:, :, 0] = (hsv[:, :, 0] + (h1 - h0)) % 1.0
    hsv[:, :, 1] = np.clip(hsv[:, :, 1] * (s1 / max(s0, 1e-6)), 0, 1)
    hsv[:, :, 2] = np.clip(hsv[:, :, 2] * (v1 / max(v0, 1e-6)), 0, 1)
    out = np.clip(mc.hsv_to_rgb(hsv), 0, 1) * 255
    if alpha is None:
        Image.fromarray(out.astype(np.uint8)).save(out_path)
    else:
        Image.fromarray(np.dstack([out, alpha]).astype(np.uint8), "RGBA").save(out_path)
    print(f"{out_path}: H/S/V {h0:.3f}/{s0:.3f}/{v0:.3f} -> {h1:.3f}/{s1:.3f}/{v1:.3f}")


if __name__ == "__main__":
    if len(sys.argv) != 4:
        sys.exit(__doc__)
    match(*sys.argv[1:])
