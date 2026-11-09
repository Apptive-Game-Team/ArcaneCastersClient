---
name: record-gif
description: Record actual app or Unity playback and deliver an animated GIF under a requested file-size limit. Also use to convert a local video to GIF; not for creating motion-comic scenes or illustrations.
---

# Record GIF

Deliver a real playback recording as an animated GIF. Reuse a supplied video when available; otherwise record the requested app/window/scene. Use the user's size limit, or the recent limit in the conversation; default to 3 decimal MB (3,000,000 bytes) when none is given. Preserve duration and readable pauses unless the user requests an excerpt.

## Capture

Identify the target and capture only its content. Prefer its existing recorder or connected API. For Unity use the configured MCP and read [unity-recording.md](references/unity-recording.md); for native/browser recording follow the available computer-use instructions when needed. Do not present a slideshow made from still screenshots as actual playback.

Use a fresh ignored capture directory. Inspect initial project/Editor state and preserve user changes. Wait for scene/localization readiness; record all requested cues with time for dialogue. Automated advancement may call the scene's existing input/advance methods in memory. Do not change saved game logic just to make a preview play automatically.

Keep frames/timestamps or an MP4 as the source recording. Stop before entering the next scene unless requested. If recording capability is unavailable, report that missing step; an existing video can still be converted.

## Encode to a size budget

Confirm `ffmpeg` is available. Use the bundled helper:

```text
python <skill>/scripts/video_to_gif.py --input <video.mp4> --output <preview.gif> --max-mb 3
```

It also accepts a local `.ffconcat` manifest with per-frame durations. Read [encoding.md](references/encoding.md) for timing, options, and MP4 retention.

The helper uses palette generation/use and bounded resolution/frame-rate/color reductions. It preserves the input timeline, measures bytes after each attempt, fully decodes the result, and writes a JSON report. If the configured minimum quality still exceeds the limit, keep the source and explain the remaining tradeoff before cutting duration or dialogue. Do not report a failed size budget as success.

Preserve source recordings. Existing outputs need a new name or intentional `--overwrite` within the user's scope. GIF has no audio; keep an MP4 as well when sound or smoother playback matters.

## Verify and deliver

Inspect decoded samples near the beginning, transitions, and ending. Confirm expected cues, no blank/stale frames, text readability, original colors, duration, dimensions, loop behavior, and exact bytes. The helper verifies format/decoding/size, not whether the right scene was recorded.

Stop capture and return the Editor to its initial mode. Back up generated font caches before restoring known initial states; preserve pre-existing dirty changes and unrelated console history.

Return a local GIF preview and download link with actual size and duration; optionally link the MP4. Recording/conversion alone does not request a code change, commit, PR update, or upload. Follow explicit repository instructions if the task also changes source.
