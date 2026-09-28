---
version: "beta"
name: "Arcane Casters Client Design System"
description: "Flat, ink-outlined UI for the Arcane Casters Unity WebGL client, following the per-screen mockup (Super Auto Pets style)."
mockup: "https://claude.ai/artifact/B5mkt5ahBc287zAjfv9K7C (Screens page)"
colors:
  orange: "#FF9A1F"        # primary button, selected segment
  gold: "#FFD23F"          # selected tile ring, highlight
  green: "#5BD04C"         # on switch, magic already in the deck, progress fill
  mana: "#3B82F6"          # mana cost badge
  red: "#F0443A"           # destructive button (Delete)
  navy: "#0F2438"          # background of list screens (Magic Book, Manage Deck)
  tile: "#22425F"          # tile fill on navy
  tile-light: "#EEF3F8"    # tile fill on white cards, empty slot
  ink: "#1C1A2B"           # every outline, hard shadow, body text on white
  grey-text: "#5B6275"     # caption and label text on white
  dim: "#0A121E9E"         # modal backdrop (rgba 10,18,30,0.62)
typography:
  display: "Lilita One (Latin) with Jua fallback (Hangul)"
  body: "Pretendard (long descriptions only)"
  scale-mockup-px: { title: 40, button: 24, label: 20, caption: 18, small: 15 }
---

## Overview

Every element has a 3px ink (`#1C1A2B`) outline, a flat fill, and a hard
5px ink shadow straight below. No gradients, no soft shadows. One orange
button per screen marks the main action; every other button is white.

The source of truth is the Screens page of the mockup linked above. Match
it screen by screen.

## Units: mockup px to canvas units

The mockup is drawn at 1280x720. Convert every size by the canvas the object
lives on:

| canvas reference | factor | sprite `m_PixelsPerUnitMultiplier` |
|---|---|---|
| 800x450 | x0.625 | 4 |
| 1920x1080 | x1.5 | 1.6667 |
| 2000x1125 | x1.5625 | 1.6 |

The flat sprites are rendered at 2.5x mockup px with pixels-per-unit 100, so
the multiplier above keeps a 3px mockup outline 3px on screen. A different
multiplier makes the outline thicker or thinner than the rest of the screen.

Text sizes convert with the same factor: button text 24 mockup px is 15 on an
800 canvas and 37.5 on a 2000 canvas.

## Fonts (`Assets/Art/Fonts`)

- `LilitaOne SDF.asset` (guid `27eb6710ab8b4024989693d7f57479f3`, material
  fileID `2180264`): all titles, buttons, labels. Dynamic atlas; its fallback
  is `Jua SDF.asset` (guid `e6a59172da1347b78e224d6c7df59404`) for Hangul.
- `LilitaOne SDF Outline.mat` (guid `af8a3c93a6bf4ba08894f839cc6276fa`,
  fileID `2100000`): white text with ink outline and ink shadow. Use it for
  text on orange buttons, screen titles on the navy or grass background, and
  big numbers on colored badges. Set the text color to white.
- Pretendard stays for long body text only.

Both new font assets are dynamic (`m_AtlasPopulationMode: 1`) with empty glyph
tables, hand-written from TextMesh Pro's own `LiberationSans SDF - Fallback`.
Glyphs are rasterised at runtime; do not expect a baked atlas in the file.

## Sprites (`Assets/Art/Images/UI/Flat`)

All are white on transparent and tinted with `Image.color`; the ink outline
stays dark under any tint.

| sprite | guid | use | Image type |
|---|---|---|---|
| FlatButton | 479a46a33fab413d8d1eaf3fd2081950 | buttons, dropdowns, segmented control box | Sliced |
| FlatCard | 5fa9efed53cb40869b9ade226431f66c | white cards and modal panels | Sliced |
| FlatTile | 391554cb40254700a095ab20cfa32db1 | magic tiles, input fields, deck slots | Sliced |
| FlatTileRing | aa3a5cf97f0b495fa340cc8358cb1e90 | selection ring over a tile (tint gold or green) | Sliced |
| FlatPill | 07d54f842f674c798047bd4fe1ba0587 | player name pill, switch track | Sliced |
| FlatChip | 008da381db86437b85037fe7cf340d8a | element chip, count badge, progress and slider track | Sliced |
| FlatCircle | 5f2a460e72e346678b07bc73ef09a7ce | mana cost badge, avatar frame | Simple |
| FlatKnob | 6a88d5dc2f474108b249217be117bdca | switch knob, slider handle | Simple |
| SlotDashed | d25775de9e6e4e8b8c0157e4b653f41a | empty deck slot | Simple |
| Banner | ca4401f7043041a48ab453cb1fdd136e | slanted white title banner | Sliced |
| Sign | 35e83eaa0ca14255868a2211373ba394 | lobby signpost, flip x with scale -1 for left | Simple |
| IconBack, IconMenu, IconClose, IconPlus, IconSearch, IconLock, IconPlay, IconChevronDown | see `.meta` | white icon with ink shadow; tint ink on white buttons | Simple |

Element icons: `Assets/Art/Images/UI/Card/type_{fire,lightning,nature,rock,water,wind}.png`.
Logo: `Assets/Art/Images/UI/ArcaneCastersLogo.png` (lobby), `ArcaneCastersLogoWide.png` (login).
Lobby character: `Assets/Art/Images/Customize/PlayerCharacterStaffRaised.png`.

The flat sprites are rendered from the mockup's own CSS in headless Chromium
(`omitBackground`, device scale 2.5), so they match the mockup exactly. To
change one, edit the CSS and render again rather than painting the PNG.

## Components (`Assets/Prefabs/UI`)

- `Button Variant.prefab`: white FlatButton, ink Lilita One text 15 (800
  canvas), autosize 9 to 15, no word wrap, margins 10/2/10/5 (the bottom
  margin keeps the text off the 5px shadow). Its inherited soft `Shadow` from
  `UI-Base` is disabled because the sprite carries the shadow.
- `Button Primary.prefab` (guid `b50deebbc8674433b61fa1b960d8eb5a`): variant
  of `Button Variant` with orange fill and white outlined text.
- A red button is a `Button Primary` instance with `m_Color` set to `#F0443A`.

- Input fields are legacy `UnityEngine.UI.InputField` with legacy `Text`
  (font `Assets/Art/Fonts/Pretendard-Regular.otf`, whole-number size, ink
  colour), on a white FlatTile. Never `TMP_InputField`: on mobile WebGL only the
  legacy field opens the on-screen keyboard correctly. Legacy `Text` cannot use
  the Lilita One SDF font, so typed text stays in Pretendard.

On a 2000 or 1920 canvas, override the instance's image
`m_PixelsPerUnitMultiplier` and text sizes by the table above.

## Do's and Don'ts

Do:
1. Put exactly one orange button on a screen.
2. Title a sub-screen with the Banner sprite and orange outlined text, placed
   right of an orange back button in the top-left corner.
3. Use navy (`#0F2438`) behind list screens and the floor texture behind the
   lobby, login, settings and adventure screens.
4. Dim the screen behind a modal with `#0A121E9E`.

Don't:
1. Add a Unity `Shadow` or `Outline` component; the sprites carry both.
2. Use `Brown-UI-Base` tint or a soft drop shadow for new UI.
3. Mix a gradient into a flat element.
