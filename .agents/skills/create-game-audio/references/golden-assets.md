# Golden Audio Assets

## Target Material Hits

The user selected three supplied WAVs on 2026-10-19: creature_onhit_1.wav,
wood_onhit_1.wav and rock_onhit_1.wav. Runtime copies live in
`Assets/Resources/Sound/Game/Hit/`. They use the existing postprocess chain
(mono PCM16 44.1 kHz, silence trim, tail fade, -3 dBFS peak), with no hard duration
cap. Full source/runtime hashes, provenance limitations, material assignments
and validation are recorded in `docs/material-hit-sounds.md`. Reuse these files;
do not regenerate them as replacements without a new user selection.

## In-game Forest Ambience

| Field | Value |
|---|---|
| Family | `ambience-forest` |
| Event | GameScene background ambience |
| Production asset | `Assets/Art/Sounds/BGM/25 Rpg Game Tracks/in-game-bgm.wav` |
| Approved source | `ingame_forest_air_intermittent_wind_candidate_v4_no_pops.wav` |
| Duration | 30.0 seconds |
| Format | Mono PCM WAV, 48 kHz, 16-bit |
| Looping | On |
| Peak | -22.67 dBFS |
| RMS | -41.75 dBFS |
| SHA-256 | `08df0f14343689d6ab63bd52a0a045645caf7595eb92c91521009588983e7842` |
| Approved by | User listening pass |
| Approval date | 2026-07-23 |
| Isolated result | Passed user listening; radio/speaker pops removed |
| Unity/WebGL result | Pending |
| Provider/provenance | ElevenLabs-assisted candidate/prototype workflow under project account |
| Distribution license | Project-owner eligibility verification pending |

Prompt direction:

```text
Natural forest air, soft wind through distant tree canopies, and sparse dry foliage,
in an open temperate forest clearing, heard from a stationary wide listener perspective,
Ambience, Loop, realistic environmental field recording.
Temporal sequence: quiet forest air -> gradual medium breeze -> sparse leaf movement ->
calm interval -> another gust -> return to quiet air.
No music, no foreground birdsong, no storm, no constant strong wind, no dramatic swell,
no clicks, no pops, no speaker crackle.
```

Do not regenerate this asset to reproduce it. Preserve and reuse the approved production file.

## Fire Attack Release

| Field | Value |
|---|---|
| Family | `magic-fire` |
| Event | Fire served-object attack release |
| Production asset | Pending canonical WAV; provisional runtime MP3 removed |
| Approved source | `.sfx-work/issue-377/review-candidates/FireReleaseBatch02/fire_flare_candidate_04.mp3` |
| Duration | 0.85 seconds requested |
| Format | Stereo MP3, 44.1 kHz, 128 kbps |
| Looping | Off |
| Prompt influence | 0.80 design target; unavailable in the connected MCP |
| Model/version | ElevenLabs Sound Effects model/version not exposed by the MCP |
| Generation date | 2026-07-23 |
| Processing chain | Direct MCP output copied byte-for-byte; no processing before source approval |
| SHA-256 | `9b0f75d18fc0083e98c7b6b87f444818e881a1394764163c4845686649712fc7` |
| Approved by | User listening pass |
| Approval date | 2026-07-23 |
| Isolated result | Candidate 4 selected; preferred small natural flare |
| Unity/WebGL result | Pending |
| Provider/provenance | ElevenLabs Sound Effects under project account |
| Distribution license | Project-owner eligibility verification pending |
| Final conversion | Pending PCM WAV 16-bit, 44.1 kHz, mono; new hash requires renewed approval |
| Rejected candidates | Batch 01 candidates 01–04 and Batch 02 candidates 01–03 |
| Rejection summary | Too metallic/impact-like or explosive; candidate 4 had the clearest small natural “화르륵” flare |

Prompt:

```text
A small sheet of natural flame rapidly catching and fluttering across dry kindling
in an open forest clearing, heard close from a third-person player perspective,
One-shot, realistic dry fire Foley.
Sequence: faint air breath, quick soft flare-up, lively flame flutter, short airy fade.
Light combustion only. No explosion, blast, impact, bass thump, pressure boom, metal,
bell, chime, glass, tonal ping, synth, or cartoon sound.
```

Use this clip only for the Fire attack event. Do not reuse it for spawn, hit, or
death. The earlier `fire_accent.wav` layering is not approved under the revised
realistic direction; each later Fire lifecycle event requires its own complete
listening-approved clip.

## UI Wood Button Click

| Field | Value |
|---|---|
| Family | `ui-wood` |
| Event | standard button click (all `ButtonBase` + global fallback) |
| Production asset | `Assets/Resources/Sound/UI/wood_button_click_v4.wav` |
| Approved source | `.sfx-work/issue-377/ui-wood/button/ui_wood_button_b3_g_20260723_proc.wav` |
| Duration | 0.22 seconds |
| Format | Mono PCM WAV, 44.1 kHz, 16-bit, peak -3 dBFS |
| Looping | Off |
| SHA-256 | `3259e5ec38bad65492fb3351b52dba3b2407839070204f33ab505f6e24db380e` |
| Approved by | User listening pass (batch presented as 4 files; user picked the 4th) |
| Approval date | 2026-07-23 |
| Generation | ElevenLabs `POST /v1/sound-generation`, `duration_seconds: 0.5`, `prompt_influence: 0.75` |
| Post-processing | `postprocess_candidate.py --peak -3 --target-duration 0.22` |
| Unity/WebGL result | Pending |
| Rejected candidates | b3_b, b3_d, b3_e (user: all similar); b3_a/c/f + batch 2 entire (auto-reject: source peak below -20 dBFS) |

Prompt (450-char limit applies to the API):

```text
A dense hardwood button pressed firmly by a fingertip on a wooden game board,
dry close studio, extreme close-up, One-shot, crisp clean Foley at strong
professional recording level. Temporal sequence: fingertip contact -> one
short solid wooden knock -> clean dry decay to silence. No plastic tick, no
toy, no cartoon pop, no desk mouse, no metal, no bell, no chime, no glass,
no reverb, no quiet distant recording.
```

Lesson: level-suppressing words ("soft", "restrained") make the model output
near-silence (peaks -35 to -47 dBFS, unusable SNR). Demand strong recording
level in the prompt and shape softness in post instead.

## Card Paper Family (hover / select / deselect)

| Field | hover | select | deselect |
|---|---|---|---|
| Production asset | `Sound/Game/Card/card_hover_v1.wav` | `Sound/Game/Card/card_select_v1.wav` | `Sound/Game/Card/card_deselect_v1.wav` |
| Approved source | `card_hover_b_20260723_proc.wav` | `card_select_d_20260723_proc.wav` | `card_deselect_c_20260723_proc.wav` |
| Duration / peak | 0.14 s / -12 dBFS | 0.18 s / -3 dBFS | 0.16 s / -6 dBFS |
| SHA-256 | `1d8c9e51…afcb` | `30e9de16…9cc8` | `8272d3a6…0456` |
| Selection | measurement-based (user-delegated), 2026-07-23 | same | same |

Family: `card-paper`. Generation: direct API, 0.5 s, influence 0.75,
postprocess trim per event. Hover must stay clearly quieter than select
(enforced by per-event `--peak`).

Prompts: coated paper card + felt-covered wooden table; hover = light
fingertip brush; select = firm tap and place; deselect = lift off felt.
All end with "at strong professional recording level" (quiet-output lesson)
and the standard exclusions (no coin, glass, metal, bell, sparkle, reverb).

## Field Confirm

| Field | Value |
|---|---|
| Family | `field-ground` |
| Production asset | `Sound/Game/field_confirm_v2.wav` |
| Approved source | `field_confirm_d_20260723_proc.wav` |
| Duration / peak | 0.18 s / -6 dBFS |
| SHA-256 | `ce7776a6c1ec43a49c77ed1964c3e36837a0f6cc26e369fa0844dc5d5e414dc6` |
| Selection | measurement-based (user-delegated), 2026-07-23; a/b/c auto-rejected (quiet) |

Prompt: fingertip tapping once on short dry grass over firm soil on a
miniature game board; exclusions include footstep, digging, wood knock.

## Fire / Stone Lifecycle Sets (vertical slice)

| Event | Production asset | Duration / peak | SHA-256 (short) |
|---|---|---|---|
| Fire spawn | `Sound/Game/Fire/fire_spawn_v1.wav` | 0.80 s / -4 dBFS | `27a628e7…a9b2` |
| Fire hit | `Sound/Game/Fire/fire_hit_v1.wav` | 0.35 s / -4 dBFS | `b7c20072…865b` |
| Fire death | `Sound/Game/Fire/fire_death_v1.wav` | 1.10 s / -4 dBFS | `2e77064b…2a41` |
| Stone spawn | `Sound/Game/Stone/stone_spawn_v1.wav` | 0.75 s / -4 dBFS | `a162cb94…5c1d` |
| Stone hit | `Sound/Game/Stone/stone_hit_v1.wav` | 0.35 s / -4 dBFS | `4c19eba5…08ac` |
| Stone death | `Sound/Game/Stone/stone_death_v1.wav` | 1.20 s / -4 dBFS | `069d08fd…56ec` |

Families `creature-fire` / `building-stone`. Selection measurement-based
(user-delegated), 2026-07-23. Full hashes and batch stats in the asset
manifest. Prompt grammar: spawn = paper pop-up unfold + material arrival;
death = prop collapse into component material (ember extinguish / pebble
spill). Assign to FireCreature / StoneBuilding profile slots in Unity;
fire attack anchor is `Sound/Game/Fire/fire_attack_v1.wav`.
