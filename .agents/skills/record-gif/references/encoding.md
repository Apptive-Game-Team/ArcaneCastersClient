# Encoding and timing

The helper needs Python 3 and FFmpeg on PATH (or `--ffmpeg <executable>`); no Pillow or ffprobe dependency.

```text
python <skill>/scripts/video_to_gif.py --input recording.mp4 --output preview.gif --max-mb 3 --width 960 --fps 8
python <skill>/scripts/video_to_gif.py --input recording.mp4 --output excerpt.gif --max-mb 2 --start 5 --duration 12
```

`--min-width` and `--min-fps` set quality floors (defaults 640 px and 4 fps). The helper tries up to six profiles; it never shortens the timeline automatically. `--start` and `--duration` select an explicitly requested excerpt. MB means decimal MB, not MiB. Use a new output path unless overwrite is intentional.

For Unity frames, write `frames.ffconcat` beside them with relative filenames and actual time between consecutive captures:

```text
ffconcat version 1.0
file 'frame-00000.png'
duration 0.131
file 'frame-00001.png'
duration 0.129
file 'frame-00002.png'
duration 0.250
file 'frame-00002.png'
```

Repeat the last file so its final hold is retained. Use the next timestamp minus the current timestamp, and the chosen end time minus the last timestamp. Do not assume capture intervals are exactly 1/fps: PNG encoding and Editor stalls otherwise change playback duration. Missing captures are errors.

Keep a source MP4 when a reusable video is wanted:

```text
ffmpeg -safe 0 -i frames.ffconcat -vf "fps=12,scale=1280:-2:flags=lanczos" -c:v libx264 -crf 23 -pix_fmt yuv420p -movflags +faststart recording.mp4
```

That command does not promise a size limit. If MP4 must also fit, estimate bitrate from duration, reserve muxing overhead, and measure bytes. GIF and MP4 budgets are independent. Prefer captured frames to a compressed MP4 as GIF input when it avoids additional noise.

The previous Word intro is evidence, not a universal preset: 32.25 seconds, 720×405, 4 fps, 96 colors produced 2,491,015 bytes. Other content needs fresh measurement. Do not delete final reveals or reading pauses to make a budget pass.
