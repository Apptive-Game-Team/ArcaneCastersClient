# Issue #377 SFX Asset Manifest

## Status

- Inventory snapshot: 2026-07-23
- Auditory approval: forest ambience v4 and Fire release batch 02 candidate 4
- Authority: `.plan/issues/2026-07-21-issue-377-sfx-style-guide.md`
- Runtime activation rule: a clip stays provisional until it passes isolated and in-game listening.

`Referenced by` records serialized Unity references. Clips loaded by a Resources
string are recorded under `Runtime owner` even when no GUID reference exists.

| Asset | Runtime owner / serialized references | Current event | Technical format | Style status | Eligible target | SHA-256 |
|---|---|---|---|---|---|---|
| `Art/Sounds/BGM/25 Rpg Game Tracks/in-game-bgm.wav` | `BgmPlayer.prefab` | in-game forest ambience loop | PCM 16-bit, 48 kHz, mono | **Source choice approved by user listening, 2026-07-23**; isolated review passed, Unity-context review pending | In-game ambience | `08df0f14343689d6ab63bd52a0a045645caf7595eb92c91521009588983e7842` |
| `Art/Sounds/BGM/Diorama/lobby_marimba_round_v1.wav` | `BGMClipContainer.bgmClip` + BgmPlayer `AudioSource` in Lobby, Login, Register, ManageDeck, Result, Admin scenes | lobby marimba round loop | PCM 16-bit, 44.1 kHz, mono | **Approved by user listening, 2026-07-27**; isolated review passed, Unity/WebGL review pending | Lobby-family BGM | `1f0044d367a894ceaff65e964e039300e5ab029fa6197234da7d69b06a257391` |
| `Art/Sounds/BGM/Diorama/magicbook_wood_box_v1.wav` | `BGMClipContainer.bgmClip` + BgmPlayer `AudioSource` in MagicBook, Adventures, Adventure scenes | magic book wooden music box loop | PCM 16-bit, 44.1 kHz, mono | **Approved by user listening, 2026-07-27**; isolated review passed, Unity/WebGL review pending | MagicBook-family BGM | `16a1c4b487442ea658a2d415263f2234d4e69c2445b21298ddd4a60fc75dded3` |
| `Art/Sounds/BGM/Adventure/boss_battle_v1.wav` | `BGMClipContainer.bgmEntries` key `boss` in `BgmContainer.prefab` | PVE boss battle loop, switched in when the server sends `pveState` channel `bgm` value `boss` | PCM 16-bit, 44.1 kHz, stereo, 60.0 s | **Chosen by the user, 2026-10-12**; generated locally with ACE-Step 1.5 (MIT, outputs usable commercially), seed 129080701; Unity/WebGL loop review pending | PVE boss BGM | `6b456026f3e2460b78acc1194a04c98a2a455eaf28557ffd98251828ef3647ae` |
| `Art/Sounds/BGM/Adventure/forest_battle_v1.wav` | `ForestBattleTheme.battleBgm`, applied by `BattleThemeApplier` on adventure matches only | forest adventure battle loop | PCM 16-bit, 48 kHz, stereo, 60.0 s | **Chosen by the user, 2026-10-12**; generated locally with ACE-Step 1.5 (MIT), seed 202, prompt: marimba, harp, pizzicato, frame drum, wooden flute, 124 BPM D minor; Unity/WebGL loop review pending | Adventure theme BGM | `422f20137b37b14902ee62d01fa4b442d004b19cc64ed1df67d859f4529285d3` |
| `Art/Sounds/BGM/Adventure/forest_boss_v1.wav` | `ForestBattleTheme.bossBgm`, applied by `BattleThemeApplier` on adventure matches only | forest adventure boss loop (`boss` key on a forest adventure match) | PCM 16-bit, 48 kHz, stereo, 60.0 s | **Chosen by the user, 2026-10-12**; generated locally with ACE-Step 1.5 (MIT), seed 202, prompt: low strings, taiko, wooden flute, celesta, wordless choir, 140 BPM D minor; Unity/WebGL loop review pending | Adventure theme BGM | `68f88511feb2694ef0fbae7fe2d8830d5465ef97862fa9550d9ac704ac196e3e` |
| `Art/Sounds/BGM/Adventure/fortress_battle_v1.wav` | `FortressBattleTheme.battleBgm`, applied by `BattleThemeApplier` on adventure matches only | fortress adventure battle loop | PCM 16-bit, 48 kHz, stereo, 60.0 s | **Chosen by the user, 2026-10-12**; generated locally with ACE-Step 1.5 (MIT), seed 202, prompt: snare, timpani, brass fanfare, staccato strings, marimba, 128 BPM E minor; Unity/WebGL loop review pending | Adventure theme BGM | `c62fc7da3d682b7669a93e555b1668bda42a55369c7f1a71f1aebe7104dc1f1f` |
| `Art/Sounds/BGM/12 Pixel Tracks/Pixel 8.wav` | none (was lobby-family `bgmClip`) | retired 2026-07-27 | — | Replaced by `lobby_marimba_round_v1.wav` | none | — |
| `Art/Sounds/BGM/12 Pixel Tracks/magic-book-bgm.wav` | none (was MagicBook-family `bgmClip`) | retired 2026-07-27 | — | Replaced by `magicbook_wood_box_v1.wav` | none | — |
| `Art/Sounds/BGM/25 Rpg Game Tracks/lobby-bgm.wav`, `lobby-bgm2.wav` | none (were stale BgmPlayer `AudioSource` clips) | retired 2026-07-27 | 22.6 MB each | Unreferenced; deletion awaiting user decision | none | — |
| `.sfx-work/issue-377/review-candidates/FireReleaseBatch02/fire_flare_candidate_04.mp3` | approved source only; provisional `SoundAssets.FireAttack` runtime mapping removed | Fire attacker release candidate | stereo MP3, 44.1 kHz, 128 kbps | **Source choice approved by user listening, 2026-07-23**; isolated review passed, canonical WAV conversion/reapproval pending | future `FireCreature` attack | `9b0f75d18fc0083e98c7b6b87f444818e881a1394764163c4845686649712fc7` |
| `UI/wood_button_click.wav` | `SoundAssets.ClickButton`; `ButtonBase`; `GlobalButtonSoundPlayer` | button click | PCM 16-bit, 44.1 kHz, mono | Needs audition; legacy anchor only | UI / button | `8494153e…236a` |
| `Game/Card/draw_card.wav` | `SoundAssets.DrawCard`; currently not played | card draw | PCM 16-bit, 44.1 kHz, mono | Needs audition; unreferenced | Card / draw | `3d394d6d…ac56` |
| `Game/Card/touch_card.wav` | `CardUI`, `CardUIZoom`, `TutorialCardUI`; game/tutorial/spectating scenes | hover/select touch | PCM 16-bit, 44.1 kHz, mono | Needs audition; one clip currently owns multiple gestures | Card / hover or select, not both unless approved | `d978347a…9065` |
| `Game/Magic/drop.mp3` | `AbstractDrop.prefab` | drop/cast | MP3, 24 kHz, joint stereo | Ineligible final format; needs audition | Transient spell / drop only | `27fedbb5…e976` |
| `Game/Magic/explosion.wav` | `ShockOverload`, `AbstractExplode`, `TornadoStrike` | attack/explode | PCM 16-bit, 44.1 kHz, mono | Needs audition; mixed ownership | Neutral transient / explode | `7f370458…f7d8` |
| `Game/Magic/shoot.wav` | `ChainLightning`, `TideCall`, `AbstractShot`, tutorial `FireShot` | launch/attack | PCM 16-bit, 44.1 kHz, mono | Needs audition; cross-element reuse | Neutral projectile / launch | `78d92f60…4ebc` |
| `Game/Magic/wind.mp3` | `SandStorm`, `RazorGale` | cast/attack | MP3, 24 kHz, joint stereo | Ineligible final format; needs audition | Wind spell / attack | `bb96b301…b293` |
| `Game/Magic/explode/fire_explode.wav` | `MagmaExplosion`, `FireExplode`, `MagmaFist` | impact/explode | PCM 24-bit, 44.1 kHz, mono | Requires conversion if approved; needs audition | Fire / impact or death | `1f7f4afe…5245` |
| `Game/Magic/explode/lightning_explode.wav` | unreferenced | impact/explode | PCM 24-bit, 44.1 kHz, mono | Requires conversion if approved; needs audition | Lightning / impact | `e4e3c395…ca9f` |
| `Game/Magic/explode/rock_explode.wav` | `Overgrowth`, `LeafExplode`, `RockExplode`, `WaterExplosion` | impact/explode | PCM 24-bit, 44.1 kHz, mono | Needs audition; known concept mismatch risk | Rock / impact only | `9d9adbff…5bf` |
| `Game/Magic/explode/water_explode.wav` | `WaterExplode` | impact/explode | PCM 24-bit, 44.1 kHz, mono | Requires conversion if approved; needs audition | Water / impact | `accf9c9a…98c4` |
| `Game/Magic/explode/wind_explode.wav` | `WindExplode` | impact/explode | PCM 24-bit, 44.1 kHz, mono | Requires conversion if approved; needs audition | Wind / impact | `de2c5907…0ba2` |
| `Game/arrow_shot.wav` | unreferenced | intended launch | PCM 24-bit, 44.1 kHz, stereo | Requires mono/16-bit conversion if approved; needs audition | Physical projectile / launch | `135d0d1e…d1d3` |
| `Game/heal.wav` | `ManaWell`, `HealingTotem`, `WindTotem`, `LifeTree` | attack/heal | PCM 24-bit, 44.1 kHz, stereo | Needs audition; attack versus heal ownership ambiguous | Nature/arcane / heal | `3a92a257…2f3` |
| `Game/hit.wav` | inherited `HitSoundPlayer.prefab`; tutorial `FireShot`, `AquaArcher` | attacker `OnAttack` legacy playback despite misleading filename | PCM 16-bit, 44.1 kHz, mono | Needs audition; verified attack owner, not HP-hit owner | Legacy attack rollback only until replacement is approved | `45a7507e…c82` |
| `Game/light_explode.wav` | `AbstractDrop`, `Effects/Explode` | drop/explode | PCM 16-bit, 44.1 kHz, mono | Needs audition; mixed event reuse | Neutral magic / small impact | `8befb3f7…d39d` |

## Immediate Findings

- Approval is auditory, not inferred by inventory. Two sources are currently
  approved as listed above; all other new generated clips remain unapproved.
- `drop.mp3` and `wind.mp3` cannot ship as final assets.
- Seven WAV files are 24-bit; three of those are stereo. Final approved copies
  must be PCM 16-bit, 44.1 kHz, mono without overwriting the source candidate.
- `rock_explode.wav` currently spans rock, nature, and water concepts. It must
  not remain a universal impact.
- `hit.wav` is attached to the shared attack player while its name implies a
  target hit. Ownership must be resolved before the vertical slice.
- Asset provenance and license are not recorded in the repository. Record both
  before accepting or replacing a clip.

## Approval Record Template

For every accepted clip, append a row containing candidate ID, source/tool,
license, prompt or recording note, editor, processing chain, isolated-listening
result, Unity-context result, approver, and approval date. A hash change
invalidates the prior approval.

The full prompts and connector limitations for the two selected sources are
recorded in `.agents/skills/create-game-audio/references/golden-assets.md`.
Provider/provenance is ElevenLabs generation under the project account; license
eligibility for distribution is pending project-owner verification. Neither
source has passed Unity-context or WebGL parity yet, so these are approved source
choices rather than final integration approvals.

## 2026-07-21 Generated Implementation Set

These clips were generated through ElevenLabs `text_to_sound_effects` as raw
44.1 kHz PCM, wrapped as mono 16-bit WAV, silence-trimmed, capped to the style
guide duration, given a 10 ms fade-out, and peak-balanced by event role. They
are runtime candidates pending in-game auditory approval; provenance is
ElevenLabs generation under issue #377.

| Runtime asset family | Files | Runtime use |
|---|---|---|
| Interaction | `wood_button_click_v3`, `card_touch_v2`, `field_confirm` | all buttons, card touch/hover, valid field submission |
| Creature body | `creature_spawn`, `creature_move`, `creature_hit`, `creature_death` | mobile creature lifecycle |
| Building body | `building_spawn`, `building_hit`, `building_death` | static building lifecycle |
| Shared state | `generic_attack`, `heal` | attack only without legacy owner; confirmed healing |
| Element accents | `fire_accent`, `water_accent`, `nature_accent`, `lightning_accent`, `rock_accent`, `wind_accent` | layered under spawn/attack/hit/death according to the explicit 93-type map |

This set is now rejected as a broad runtime assignment. Individual files may be
reconsidered only through a new listening comparison under the 2026-07-23
realistic style guide; no file inherits approval from having been wired.

The connector could not play audio because PortAudio is unavailable, and Unity
MCP was unreachable. Therefore these remain candidates even though they are
fully wired; the Draft PR cannot become Ready before a human listening pass.

## Candidate Cleanup Requirement

Review candidates were moved from runtime Resources to the gitignored
`.sfx-work/issue-377/review-candidates` audition area on 2026-07-23. Rejected
files must not return to importable runtime Resources. Only golden normalized
assets belong under `Assets/Resources/Sound`.

## 2026-07-23 UI Wood Button Batch

- Family: `ui-wood`
- Event: standard button press
- Provider: ElevenLabs Sound Effects MCP
- Requested duration: 0.50 seconds (connector minimum)
- Looping: off
- Output: MP3, 44.1 kHz, 128 kbps, stereo candidate
- Prompt influence: 0.75 design target; connector did not expose the parameter
- Runtime status: audition only; no candidate activated

Prompt:

> A compact solid hardwood button mechanism pressed once by a fingertip on a
> wooden game console, recorded in a dry quiet studio, heard extreme close-up,
> One-shot, realistic clean Foley. Temporal sequence: soft skin contact -> short
> dense wooden compression knock -> immediate dry release. No plastic, toy,
> cartoon pop, door knock, desk mouse, metal, bell, chime, glass, sparkle, radio
> crackle, speaker breakup, or reverb.

| Candidate | SHA-256 | Listening status |
|---|---|---|
| `ui_wood_button_candidate_a_20260723.mp3` | `f71a03700291e7c6449a9da82afebe2150befd794aabc4adb3aa1d562ff35dbe` | rejected 2026-07-23 (user: bad ending) |
| `ui_wood_button_candidate_b_20260723.mp3` | `18f1c7c4e7d151afb10f15776ce65ac86a914ed3d3264ea0f8a981de914a3c70` | rejected 2026-07-23 (user: bad ending) |
| `ui_wood_button_candidate_c_20260723.mp3` | `91e8ae4c4676a9de14895668c3ce26ba657a5dc3f4dc61f5bb1bb524d4573686` | rejected 2026-07-23 (user: bad ending) |
| `ui_wood_button_candidate_d_20260723.mp3` | `35cc26b7d6bf3984230a2b02d11009ad32de08ad15b4c282baf04caffdfbad6d` | rejected 2026-07-23 (user: bad ending) |

### Batch failure analysis (waveform/spectral measurement, not a listening pass)

User verdict: the tail/ending of every candidate sounds wrong. Decoded-PCM
measurements agree and localize the causes:

- All four files are 0.48 s where the click preset allows 0.12–0.35 s. The
  connector's 0.5 s minimum forced an over-long duration; the model filled the
  unused time with invented room resonance, and one file was cut at the
  boundary. This is the primary root cause.
- All four are far below level targets (sample peaks -26.9 to -51.2 dBFS
  versus the -1 dBFS ceiling standard), so audition players normalize them and
  amplify the tail artifacts further.
- `a`: after the main click decays (~0.30 s), the tail holds prominent tonal
  resonances near 600/1000/1400/3000/4400 Hz plus broadband hiss — a faint
  ringing "box" tail instead of dry silence.
- `b`: effectively silent (peak -51.2 dBFS); a failed generation, unusable.
- `c`: audio never decays to silence before the file boundary — zero trailing
  silence, a -38.8 dBFS single-sample spike inside the final 3 ms, and a
  non-zero final sample: an audible end click from hard truncation.
- `d`: same ringing-tail pattern as `a` (resonances near 800/1400/1600 Hz)
  at very low level (peak -40.4 dBFS).

Regeneration requirements for the next button batch: obtain a path that
honors 0.20–0.25 s duration (direct API or post-trim), verify peak level and
tail decay with `audio_probe.py` before presenting candidates, and reject any
candidate whose envelope has not fallen below -60 dBFS by the file end.

## 2026-07-23 UI Wood Button Batch 3 (direct API + post-processing chain)

- Provider: ElevenLabs `POST /v1/sound-generation` (direct REST, not MCP)
- Parameters actually sent: `duration_seconds: 0.5`, `prompt_influence: 0.75`
- Post-processing: `postprocess_candidate.py --peak -3 --target-duration 0.22`
  (lead-silence strip, 0.22 s trim, tail fade to silence, peak -3 dBFS,
  mono 44.1 kHz 16-bit WAV)
- Batch 2 (same-day, softer prompt) was fully auto-rejected before
  presentation: all four source peaks -34.6 to -47.2 dBFS with 11-39 dB SNR —
  normalization would expose the noise floor at -14 to -42 dBFS. Lesson
  recorded: level-suppressing prompt words ("soft", "restrained") drive the
  model to near-silent output; batch 3 prompt demands
  "strong professional recording level" instead.
- Batch 3 generation also produced quiet failures (a, c, f rejected at
  -36.9 to -40.9 dBFS source peak); passing candidates were kept until four
  survived the automatic gates.

Prompt (batch 3):

> A dense hardwood button pressed firmly by a fingertip on a wooden game
> board, dry close studio, extreme close-up, One-shot, crisp clean Foley at
> strong professional recording level. Temporal sequence: fingertip contact ->
> one short solid wooden knock -> clean dry decay to silence. No plastic
> tick, no toy, no cartoon pop, no desk mouse, no metal, no bell, no chime,
> no glass, no reverb, no quiet distant recording.

Presented audition files (processed, in `.sfx-work/issue-377/ui-wood/button/`):

| Candidate | SHA-256 | Measurement notes | Listening status |
|---|---|---|---|
| `ui_wood_button_b3_b_20260723_proc.wav` | `377134d4cbe743ea8ee926ca7855b4d575db754c8e6420a10c7dda1ec2248da2` | strongest 1.1 kHz wood-body resonance | rejected 2026-07-23 (user: all similar, picked 4th) |
| `ui_wood_button_b3_d_20260723_proc.wav` | `a9bc34032538f5cb0617051dd6df0115b1e851edc37798b9bcbbdb177dbdfeb8` | small tail residue at -27 dB rel peak | rejected 2026-07-23 |
| `ui_wood_button_b3_e_20260723_proc.wav` | `55d27b7907c7952a99e12996fdae7f2dd25403ed4d0889dd1ce080b10ec0c626` | spectrally cleanest tail | rejected 2026-07-23 |
| `ui_wood_button_b3_g_20260723_proc.wav` | `3259e5ec38bad65492fb3351b52dba3b2407839070204f33ab505f6e24db380e` | fastest decay, quietest ending (-90 dBFS) | **APPROVED by user listening 2026-07-23** (presented 4th) |

All four end below -73 dBFS within the file with no truncation click.

Golden integration (2026-07-23): candidate `g` copied byte-for-byte to
`Assets/Resources/Sound/UI/wood_button_click_v4.wav` (same SHA-256
`3259e5ec…b380e`), import meta set per standard (Force To Mono, 2D,
Decompress On Load, preload on, normalize off), and
`SoundAssets.ClickButton` re-anchored to `Sound/UI/wood_button_click_v4`.
Legacy `wood_button_click.wav` retained as rollback anchor. Unity-context
(Editor/WebGL, rapid-repeat, volume 0/50/100) verification still pending.

Process delegation note: from this point the user delegated per-batch
candidate selection to Claude ("다 거기서 거기" — pick and proceed);
selections are measurement-based, recorded here, and remain subject to the
user's final ear veto before PR Ready.

## 2026-07-23 Card and Field Batches (delegated selection)

Same pipeline as the button batch (direct API `duration_seconds: 0.5`,
`prompt_influence: 0.75`, postprocess chain, measurement gates). Selection was
delegated by the user; picks are measurement-based (cleanest tail, no tonal
ring, clean file ending) and remain subject to the user's final ear veto.
Prompts recorded in `create-game-audio/references/golden-assets.md`.

| Event | Batch result | Selected | Production asset | SHA-256 |
|---|---|---|---|---|
| Card hover | 4/4 passed gates | `card_hover_b` (end -76 dBFS, no tonal) | `Sound/Game/Card/card_hover_v1.wav` (0.14 s, peak -12 dBFS) | `1d8c9e5153dddb84cb36c875481145841fdc3ac7203894e7c92249b0ba19afcb` |
| Card select | 4/4 passed | `card_select_d` (end -90 dBFS; `a` had 3.1 kHz tonal peak) | `Sound/Game/Card/card_select_v1.wav` (0.18 s, peak -3 dBFS) | `30e9de16b0543e0267f1ca7d37fad3444a6b299fb8ca6dc56aa3e1b965289cc8` |
| Card deselect | 4/4 passed | `card_deselect_c` (end -69 dBFS) | `Sound/Game/Card/card_deselect_v1.wav` (0.16 s, peak -6 dBFS) | `8272d3a6d8238ec3b25d1c3250205c99a42b32beeaeef3348136f6ae39f20456` |
| Field confirm | 1/4 passed (a/b/c auto-rejected at -20.7 to -21.5 dBFS source peak) | `field_confirm_d` (end -62 dBFS, clean) | `Sound/Game/field_confirm_v2.wav` (0.18 s, peak -6 dBFS) | `ce7776a6c1ec43a49c77ed1964c3e36837a0f6cc26e369fa0844dc5d5e414dc6` |

Loudness hierarchy baked into assets: hover (-12) < deselect (-6) = field
(-6) < select (-3) = button (-3).

Wiring (2026-07-23): `SoundAssets.TouchCard` accessor replaced by `CardHover`
/ `CardSelect` / `CardDeselect`; `CardUIZoom` hover keeps its 0.12 s
cooldown; `CardUI`/`TutorialCardUI` now play select on selection and deselect
on cancel; `SoundAssets.FieldConfirm` re-anchored to `field_confirm_v2`.
Legacy `card_touch_v2.wav`, `touch_card.wav`, `field_confirm.wav` retained
as rollback anchors. Card draw remains unwired pending stable card identity
(parent plan rule). Unity-context verification pending for all four.

## 2026-07-23 Fire Attack Canonical WAV

Approved source `fire_flare_candidate_04.mp3` (hash `9b0f75d1…12fc7`,
user-approved 2026-07-23) converted through the standard chain
(`postprocess_candidate.py --peak -3`; lead-trim, tail fade, mono 44.1 kHz
16-bit) and installed as `Assets/Resources/Sound/Game/Fire/fire_attack_v1.wav`
(0.84 s, peak -3 dBFS, end -80.8 dBFS, no truncation).

New SHA-256:
`792311378e15cd82a98bea50f3cb689de4d28c060aee186d7dbe112962a4234b` —
conversion changes the hash, so this WAV needs a renewed ear pass (delegated
selection applies, ear veto pending). Not yet referenced by any profile:
`FireCreature.Attack` assignment happens in the Unity Editor session after
the catalog builder runs. FFmpeg was unavailable; the recorded chain uses
libsndfile 1.2.2 via the soundfile package instead, without gain/EQ beyond
the documented peak normalization and tail fade.

## 2026-07-23 Vertical Slice Lifecycle Batches (Fire / Stone, delegated selection)

Same pipeline (direct API, influence 0.75, postprocess `--peak -4`).
Miniature diorama prompts: spawn = paper pop-up unfold + material arrival;
death = prop collapse into material. Staged for profile assignment in the
Unity session — no runtime references yet.

| Event | Batch | Selected | Production asset | SHA-256 |
|---|---|---|---|---|
| Fire spawn | 4/4 passed | `c` (smooth decay, rerise -42 dB) | `Sound/Game/Fire/fire_spawn_v1.wav` (0.80 s) | `27a628e7049065a237252e9a16636ba1e3f3614bbeea15c8595ea1fc6974a9b2` |
| Fire hit | 4/4 passed | `b` (fastest clean decay) | `Sound/Game/Fire/fire_hit_v1.wav` (0.35 s) | `b7c20072e825a1d0f4a52e3a789a77041264984fcd956a2590bccebd4d7d865b` |
| Fire death | 2/4 passed (a, d quiet-rejected) | `c` (gradual extinguish, no late blip) | `Sound/Game/Fire/fire_death_v1.wav` (1.10 s) | `2e77064b5fbe061ca5960fab70fa6ffbdb1552d9a7e78747d83f0681f1542a41` |
| Stone spawn | 4/4 passed | `d` (natural unfold→set-down→settle shape) | `Sound/Game/Stone/stone_spawn_v1.wav` (0.75 s) | `a162cb94722f28925a9c362df1599f41d71f7ba89bb489ce1cac016b4de65c1d` |
| Stone hit | 3/4 passed (d quiet-rejected) | `b` (lowest tail residue) | `Sound/Game/Stone/stone_hit_v1.wav` (0.35 s) | `4c19eba5de87400eb7fd38402a91295eb14b02304d4795df2b1d2604519808ac` |
| Stone death | 4/4 passed | `d` (pebble spill settling by 0.95 s, clean end) | `Sound/Game/Stone/stone_death_v1.wav` (1.20 s) | `069d08fd9c0d5fb24a12edf67709c53c10a3da06182e3912ca7dd8f7587556ec` |

All peaks -4 dBFS (below UI select/button -3). Multi-transient spawn/death
envelopes re-rise by design; the rerise gate applies to one-shots only.
Unity-context listening and the user's ear veto pending for all six.

## 2026-07-23 Runtime Resources Cleanup (second pass)

- `SoundAssets.ClickButton` re-anchored from unapproved `wood_button_click_v3`
  back to the legacy `Sound/UI/wood_button_click` per the Phase 0 baseline
  rule. `wood_button_click_v3.wav` was moved out of runtime Resources to
  `.sfx-work/issue-377/ui-wood/button/retired/` for future audition.
- The rejected 2026-07-21 generated set (`Sound/Game/Lifecycle/*`,
  `Sound/Game/Element/*`, 15 clips) was removed from runtime Resources after
  verifying zero code references and zero serialized GUID references. Files
  remain recoverable from git history; the golden-asset rule (nothing
  unapproved ships from Resources) now holds for the whole Sound folder.

## 2026-08-03 Shared-Slot Batch (melee / explosion / card hover)

First batch generated under the 2026-07-27 concept doc (attack split into
melee / ranged / explosion; elements survive only in spawn). Direct REST
`POST /v1/sound-generation`, `duration_seconds` 0.5 (API floor), postprocess
trimmed each to its concept-doc target length and baked the concept-doc level
hierarchy instead of the usual `--peak -3`.

`audio_probe.py` gained a metallic-ring gate this session
(`ring_prominence_db` ≥ 12 **and** `hf_decay_to_-40dB_s` ≥ 0.15 → auto-reject),
added because the recurring user verdict was "챙 소리가 너무 난다" and the
existing probe measured nothing that would catch it. The peak search band was
raised 1.5 kHz → 2.5 kHz after wooden body resonance scored as a clang;
`wood_button_click_v4` and `card_hover_v1` were re-probed and still pass.

13 generations, 5 died before listening: 2 near-silent (-35.6 / -40.6 dBFS),
3 metallic (melee 06 @ 16.0 kHz/24.1 dB/0.198 s, explosion 01, explosion 05).

| Event | Batch | Approved (user listening, 2026-08-03) | Source | SHA-256 |
|---|---|---|---|---|
| Melee attack (all creatures) | 4 presented | `candidate_07` | `.sfx-work/issue-377/object-melee/attack/candidate_07_20260803_proc.wav` (0.25 s, -10 dBFS) | `ccc24e16cbc500b512694c08792eaf9917a282175912108aa601a39f77703552` |
| Card hover (rework, 0.28 s → 0.12 s) | 4 presented | `candidate_02` | `.sfx-work/issue-377/ui-card/hover/candidate_02_20260803_proc.wav` (0.12 s, -16 dBFS) | `c1a005fad43c617fe9c26900b283224cf7104c32424d2b55dfe1f8bf5132beba` |
| Explosion (all projectiles/spells) | round 1: 5 presented, all rejected | see round 2 below | — | — |

Both approved files are already mono PCM_16 44.1 kHz, so they move to Unity
without a conversion step and keep the approved hash. Rejected candidates stay
in `.sfx-work` (gitignored) and are not committed.

### Explosion round 2 (2026-08-03)

Round 1 failed because one prompt produced all five candidates — the user
heard five versions of the same sound, not five options. Round 2 split the
**material concept** into four, two candidates each, so the choice was about
what the event is made of rather than which take was cleanest.

| Concept | Result |
|---|---|
| 무너짐 — wooden blocks toppling into a heap | 2 survived |
| 종이 터짐 — paper ball bursting open | 2 survived |
| 두 겹 — one damped thud, then debris scattering | 1 survived (`c` metallic, `a` near-silent) |
| 흙 흩어짐 — dry soil and pebbles thrown across felt | **0 survived** — pebbles skittering on felt ring bright (9.8 kHz / 0.22 s); the concept itself fails the metallic gate |

| Event | Approved (user listening, 2026-08-03) | Source | SHA-256 |
|---|---|---|---|
| Explosion | `r2_twolayer_b` — 두 겹 concept | `.sfx-work/issue-377/object-explosion/impact/r2_twolayer_b_20260803_proc.wav` (0.45 s, -6 dBFS, end -90.3 dBFS, rerise -35.8 dB) | `5af69fa26f8ef02aaabfdf6bdddd9a32f11f0281e7b344a0fc6d2eeb8929a84a` |

All three shared slots from the 2026-07-27 concept doc are now settled:
melee `candidate_07`, explosion `r2_twolayer_b`, card hover `candidate_02`.
Ranged attack was already approved earlier (`transient_release_02`). Next:
install into Resources, assign to profile slots, Unity verification.

### Runtime application (2026-08-03)

| Slot | Production asset | Profile | Runtime types |
|---|---|---|---|
| Explosion | `Sound/Game/Shared/explosion_v1.wav` | `TransientExplode` (spawn) | 9 explode types |
| Ranged attack | `Sound/Game/Shared/ranged_attack_v1.wav` | `TransientShot` (spawn) | 8 shot types |
| Card hover | `Sound/Game/Card/card_hover_v2.wav` | `SoundAssets.CardHover` | UI |

**Filter exception, recorded on purpose.** `ranged_attack_v1.wav` is the
earlier-approved `transient_release_02` and it fails the metallic gate added
this session (3547 Hz / 12.2 dB / 0.216 s), and runs 0.80 s against the
concept doc's 0.30 s target. Both facts were reported; the user chose to ship
it as-is. This is the only case so far where a human overrode an auto-reject —
check this clip first if attack audio feels wrong in Play Mode.

Melee (`candidate_07`) stays approved but unwired: the melee/ranged split is
decided by the server's `attack_range`, and only six units are known in this
repo. The file is not installed into Resources, so nothing unused ships to
WebGL.

`.meta` files for the three installed clips and two new profiles were
hand-authored with fresh GUIDs, using `wood_button_click_v4.wav.meta` as the
import-settings template. Unity Editor import verification is pending.
