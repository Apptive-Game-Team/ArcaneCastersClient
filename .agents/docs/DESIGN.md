---
version: "alpha"
name: "WordOnline Client Design System"
description: "Visual identity and design tokens for the WordOnline Unity WebGL client, describing UI base structures, colors, typography, shapes, and layouts."
colors:
  background: "#1E2327"       # Default dark background slate (WebGL container theme)
  primary: "#2FB8A8"          # Vibrant Turquoise/Teal used for primary buttons in tutorials
  secondary: "#2D3543"        # Dark charcoal/slate-blue used for secondary buttons
  brown-base: "#D99F71"       # Warm Brown/Tan for default panel backgrounds (UI-Base)
  card-base: "#E2AA7D"        # Soft peach/tan used for cards/reward panel backdrops
  panel-bg: "#DBD8D8"         # Light warm grey used for main panel backgrounds (Lobby/Join panels)
  text-light: "#D7DEE8"       # Light blue-grey text for dark backgrounds
  text-dark: "#000000"        # Black text for light backgrounds (buttons/panels)
  overlay: "#0000007D"        # Translucent black backdrop overlay (alpha ~ 0.49)
  disabled: "#C8C8C880"       # Disabled element overlay color
typography:
  family-primary: "Pretendard"
  family-bold: "Pretendard-Bold"
  family-extrabold: "Pretendard-ExtraBold"
  label-button:
    fontFamily: "Pretendard-Regular"
    fontSize: "25px"
    fontWeight: 400
  label-bold:
    fontFamily: "Pretendard-Bold"
    fontSize: "25px"
    fontWeight: 700
rounded:
  border-slice: "30px"        # 9-slice sprite borders (30, 30, 30, 30) for rounded panel/button corners
spacing:
  button-margin: "10px"
  panel-padding: "20px"
components:
  ui-base:
    backgroundColor: "{colors.brown-base}"
    rounded: "{rounded.border-slice}"
    width: "200px"
    height: "50px"
  button-primary:
    backgroundColor: "WoodPlankButton.png, white tint (plain wood)"
    textColor: "#3A2616"
    rounded: "{rounded.border-slice}"
    width: "100px"
    height: "50px"
  button-secondary:
    backgroundColor: "WoodPlankButton.png, #9A8B7C tint (darker desaturated wood)"
    textColor: "{colors.text-dark}"
    rounded: "{rounded.border-slice}"
    width: "100px"
    height: "50px"
  button-variant:
    backgroundColor: "{colors.brown-base}"
    textColor: "{colors.text-dark}"
    rounded: "{rounded.border-slice}"
    width: "200px"
    height: "50px"
  lobby-panel:
    backgroundColor: "{colors.panel-bg}"
    width: "500px"
    height: "400px"
  reward-panel:
    backgroundColor: "{colors.card-base}"
    width: "500px"
    height: "400px"
---

## Overview

The WordOnline WebGL Client visual identity features a warm, tactile, and game-oriented interface. The design combines structured panels, rounded cards, and high-contrast interactive elements. By using a warm limestone backdrop texture (`background.png`), classic brown framing, and clear typographic hierarchy built on the **Pretendard** font family, the interface balances legibility with a premium gaming feel.

## Colors

The design system employs a curated palette consisting of warm earthy tones for structure and high-contrast cooler tones for interactions:

- **Earthy Structural Colors:**
  - **Brown Base (`#D99F71`):** A warm, grounding brown/tan tone. Drives the default framing (`UI-Base`, `Button Variant`).
  - **Card Base (`#E2AA7D`):** A softer peach-tan tone used for reward popups and card backings to draw focus.
  - **Panel Background (`#DBD8D8`):** A neutral light-grey container backdrop.
- **Interactive Colors:**
  - **Primary Interactive (`#2FB8A8`):** A vibrant turquoise/teal. No longer used as a button background — `PrimaryButton.prefab` now uses the wood-plank sprite with a white tint. No remaining usage under `Assets/` as of this change; kept here only as a defined token in case another interactive element still wants it.
  - **Secondary Interactive (`#2D3543`):** A dark charcoal/slate-blue. No longer used on `SecondaryButton.prefab`, which now uses the wood-plank sprite tinted `#9A8B7C`. Still used as a background color elsewhere (`Assets/Scenes/GameScene.unity`, `Assets/Scenes/LobbyScene.unity`), so the token is not stale for those.
- **Text & Feedback Colors:**
  - **Text Light (`#D7DEE8`):** Light blue-grey for text on dark backgrounds.
  - **Text Dark (`#000000`):** Pitch black for high contrast on light/brown panels.
  - **Overlay (`#0000007D`):** Dark translucent backdrop for modal screens.

## Typography

Typography relies entirely on the **Pretendard** typeface family, configured in Unity using TextMeshPro (TMP) SDF assets:

- **Pretendard-Regular (SDF GUID: `9a8c64c89aee44fa2ac41fff91221f41`):** Used for standard button labels, options, description bodies, and dialog texts. Normal sizing runs around `25px`.
- **Pretendard-Bold (SDF GUID: `600c4f1b47bd94ef3a97ca54333d5085`):** Used for key information sections, lobby lists, and confirm pages.
- **Pretendard-ExtraBold (SDF GUID: `b15c5506cbcd44b48b4a55fb77fea659`):** Reserved for scene headers, magic info titles, and reward headings.

## Layout

Layout positioning focuses on standardized panel dimensions and 9-sliced stretching:

- **Dialogs and Settings Panels:** Sized at `500x400` pixels, centered in canvas space, with a dark full-screen overlay backdrop (`Overlay`).
- **Standard Action Buttons:** Standard size defaults to `200x50` pixels. Tutorial/Modal buttons use a smaller footprint of `100x50` pixels.
- **Background Layering:** All menus should place the `Background` Image (guid `4fe2d02ca2c9b49fc938a577493218e1`) as the very first child of the scene's primary Canvas.

## Shapes

Shapes feature soft, rounded contours:

- **9-Sliced Roundness:** Framed panels (`UI-Base`, `Brown-UI-Base`, `RewardUI`) utilize a 9-sliced border setting of `30` pixels (`spriteBorder: {x: 30, y: 30, z: 30, w: 30}`). This ensures corners remain smooth and round across all panel scales.
- **Drop Shadows:** Base frames include a Unity UGUI `Shadow` component with a shadow color of `#0000007D` (translucent black) and an offset distance of `x: 0, y: -10` to add elevation.

## Components

The system includes preconfigured prefab components located in [Assets/Prefabs/UI](file:///Users/jeong-yunseong/development/word-online/dev/word-online/client/Assets/Prefabs/UI):

- [UI-Base.prefab](file:///Users/jeong-yunseong/development/word-online/dev/word-online/client/Assets/Prefabs/UI/UI-Base.prefab): The root UI panel using the card sprite (`Card.png`) with white tinting.
- [Brown-UI-Base.prefab](file:///Users/jeong-yunseong/development/word-online/dev/word-online/client/Assets/Prefabs/UI/Brown-UI-Base.prefab): Extends `UI-Base` with the wood-frame panel sprite `WoodFramePanel.png`, white tint, `m_PixelsPerUnitMultiplier: 1` (on an 800x450-reference canvas). `Button Variant.prefab` and `UI/Debug/DebugItemButton Variant.prefab` instance it too, but each overrides `m_Sprite`, `m_Color` and `m_PixelsPerUnitMultiplier` back to the plank look on their own `Image`, so this change does not affect them.
- [Button Variant.prefab](file:///Users/jeong-yunseong/development/word-online/dev/word-online/client/Assets/Prefabs/UI/Button%20Variant.prefab): Default 200x50 button incorporating a brown panel base and Pretendard-Regular text.
- [PrimaryButton.prefab](file:///Users/jeong-yunseong/development/word-online/dev/word-online/client/Assets/Prefabs/UI/Tutorial/PrimaryButton.prefab): 100x50 wood-plank action button, white tint, label `#3A2616`.
- [SecondaryButton.prefab](file:///Users/jeong-yunseong/development/word-online/dev/word-online/client/Assets/Prefabs/UI/Tutorial/SecondaryButton.prefab): 100x50 wood-plank dismissal button, darker desaturated tint `#9A8B7C`, label `#000000` (6.4:1 contrast).

## Do's and Don'ts

### Do's:
1. Always inherit custom panels from [UI-Base.prefab](file:///Users/jeong-yunseong/development/word-online/dev/word-online/client/Assets/Prefabs/UI/UI-Base.prefab) to preserve 9-slice rendering consistency.
2. Use **Pretendard-Regular** for labels and **Pretendard-Bold** or **Pretendard-ExtraBold** for headers.
3. Wire scene buttons to use `ButtonBase` or `DisableableButtonBase` to trigger standard audio clicking behaviors.

### Don'ts:
1. Avoid introducing raw camera colors or custom background textures in menus; always use the default `background.png` (GUID `4fe2d02ca2c9b49fc938a577493218e1`).
2. Do not use pure white or generic CSS/Unity colors for panels. Stick to `#D99F71` (Brown Base), `#E2AA7D` (Card Base), or `#DBD8D8` (Panel Background).
3. Do not instantiate custom fonts; use the registered TextMeshPro SDF assets under `Assets/Art/Fonts`.
