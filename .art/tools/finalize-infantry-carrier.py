"""Finalize approved frame sheets with one scale and one foot baseline per unit."""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / ".art/concept/infantry-mortar-production"
DEST = ROOT / "Assets/Resources/Game/sprites"

def finalize(source, names, maximum):
    sheet = Image.open(SOURCE / source).convert("RGBA")
    count = len(names)
    frames = []
    for index in range(count):
        frame = sheet.crop((round(index * sheet.width / count), 0,
                            round((index + 1) * sheet.width / count), sheet.height))
        alpha = np.asarray(frame.getchannel("A"))
        ys, xs = np.where(alpha > 128)
        baseline = int(ys.max()) + 1
        feet = np.where((alpha > 128) & (np.indices(alpha.shape)[0] >= baseline - 20))
        anchor = round((int(feet[1].min()) + int(feet[1].max())) / 2)
        bbox = frame.getbbox()
        frames.append((frame, anchor, baseline, bbox))
    half = max(max(anchor - box[0], box[2] - anchor) for _, anchor, _, box in frames)
    height = max(baseline - box[1] for _, _, baseline, box in frames)
    width = half * 2
    factor = min(maximum / width, maximum / height)
    size = (round(width * factor), round(height * factor))
    results = []
    for name, (frame, anchor, baseline, box) in zip(names, frames):
        # Shared geometry; center is the feet, not the overall weapon-dependent bounds.
        canvas = Image.new("RGBA", (width, height))
        canvas.alpha_composite(frame, (half - anchor, height - baseline))
        canvas = canvas.resize(size, Image.Resampling.LANCZOS)
        # Remove resampling colour under transparent pixels to keep PNGs deterministic.
        array = np.array(canvas)
        array[array[:, :, 3] == 0, :3] = 0
        canvas = Image.fromarray(array)
        canvas.save(DEST / (name + ".png"))
        a = np.asarray(canvas.getchannel("A"))
        rgb = np.asarray(canvas)[:, :, :3].astype(int)
        magenta = (rgb[:, :, 0] > 160) & (rgb[:, :, 2] > 160) & (rgb[:, :, 1] < 80) & (a > 128)
        assert not magenta.any(), name
        assert all(canvas.getpixel(p)[3] == 0 for p in [(0,0), (size[0]-1,0)])
        print(name, size, "bbox", canvas.getbbox(), "transparent", round(float((a == 0).mean()), 3))
        results.append(canvas)
    return results

frames = finalize("ArcaneInfantry-alpha.png", ["ArcaneInfantry", "ArcaneInfantryAttacking"], 128)
frames += finalize("CraterCarrier-alpha.png", ["CraterCarrier", "CraterCarrierDeployed", "CraterCarrierRecovering"], 192)
# Compare production sprites to frozen faction anchors at 64px on two backgrounds.
board = Image.new("RGB", (7 * 112, 2 * 112), "#e0e0e0")
anchors = [Image.open(ROOT / ".art/anchors/master-v2/ApprenticeMage.png").convert("RGBA"),
           Image.open(ROOT / ".art/anchors/master-v2/HellfireDemon.png").convert("RGBA")]
for row, color in enumerate(["#e0e0e0", "#36414c"]):
    ImageDraw.Draw(board).rectangle((0,row*112,board.width,(row+1)*112), fill=color)
    for col, frame in enumerate(frames + anchors):
        preview = frame.copy()
        preview.thumbnail((64,64), Image.Resampling.LANCZOS)
        board.paste(preview, (col*112+(112-preview.width)//2,row*112+24), preview)
board.save(SOURCE / "contact-sheet-64.png")
