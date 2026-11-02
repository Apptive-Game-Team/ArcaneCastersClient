# Unity playback recording

Use the project Unity MCP skill if available. Inspect the actual connection, project identity, Editor state and tool schemas. Do not assume the endpoint, scene name or Recorder installation from an old example.

## Capture the real Game View

Prefer an already installed Unity Recorder when suitable. Otherwise, a temporary in-memory coroutine can call `ScreenCapture.CaptureScreenshot(path)` at a chosen rate and log `Time.unscaledTime` for each filename. Feed timestamped frames to FFmpeg; this includes Screen Space Overlay UI.

The working approach from the Word intro was:

1. Snapshot initial dirty font/settings files and inspect Git status. Use a fresh ignored output folder.
2. Open the requested scene, enter Play mode, wait for localization readiness, then start the first cue. Reset all view/progress state if reusing a played controller.
3. Focus Game View if `WaitForEndOfFrame` would stall in Scene View. Restore any background/recording settings changed for capture.
4. Run a finite coroutine yielding `WaitForEndOfFrame`. At capture intervals call `ScreenCapture.CaptureScreenshot` with unique filenames and log actual timestamps. Leave an initial end-of-frame boundary before the first capture.
5. At agreed cue times call the existing `Advance()` and confirm the beat changed. Let the player's typing/reveal animation run. Stop after a readable final hold, before the completion click transitions scenes.
6. Save timestamps, end time, observed cue texts and completion/error status. Wait for pending screenshots to reach disk; check every listed frame exists. Encode only a completed capture.

A finite `IEnumerable<object>` built with LINQ `Select`/`TakeWhile` can supply an `IEnumerator` to `StartCoroutine` from MCP's method-body-only `execute_code`, without a production MonoBehaviour. Capture exceptions to an error marker and end the coroutine. Bound frames/time; do not leave an unbounded Editor callback. This is optional when a working recorder exists.

Adapt advancement and reading times to the actual scene. The Word intro used 8 holds of 5, 4.5, 4.5, 2.8, 3.8, 3.8, 3 and 5.6 seconds. Those are not generic counts/timings. Its last cues were continue→war; preserve the current user's requested order.

## Failures caught during recording

- `Camera.Render()` omits Screen Space Overlay UI. Capture Game View for captions/buttons.
- Calling `CaptureScreenshotAsTexture()` before end of frame produced a Render Target size mismatch and capture failure. Do not call a partial run complete.
- Blitting that texture into a temporary RenderTexture changed gamma and made the recording too bright. Direct `ScreenCapture.CaptureScreenshot` preserved colors. Resize with FFmpeg afterward and inspect actual samples; do not compensate artwork with a guessed gamma filter.
- Rendering dirtied both Jua and LilitaOne TMP assets. Compare all changed files afterward. Preserve initial dirty contents, back up later generated state, and restore only proven capture-generated changes.
- Wait for localized text before recording. A successful API call can capture empty text or a stale previous frame; inspect beginning, transitions and ending.

Restore initial Play/Edit mode and changed recording/background settings. Do not claim WebGL/browser recording when the source was Unity Game View.
