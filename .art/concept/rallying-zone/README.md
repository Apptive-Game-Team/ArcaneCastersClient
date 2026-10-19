# rallying_totem 집결 구역 고리

Issue #338. `RallyingTotem.prefab` 의 자식 `RallyZone` 이 그리는 집결 구역(`rallying_totem.effect_radius`, 2) 경계 그림이다.

상태 오라 `fire_aura.png` 는 512x295 타원이라 `AuraRadiusScaler` 로 원을 만들면 불꽃이 세로로 1.74배 늘어나고, `fire_field.png` 는 용암 장판이라 피해 지역으로 읽힌다. `ANIMATION-ASSETS.md` 의 "상태 오라를 키워서 범위 표시로 쓰지 않는다" 에 따라 새로 그렸다.

## 생성

- prompt: `prompts/shared-prefix.txt` + `prompts/variant-*.txt`
- 참조 이미지: `.art/anchors/master-v2/MasterStyleKey.png`, `.art/anchors/master-v2/HellfireDemon.png`, `Assets/Resources/Game/sprites/RallyingTotem.png`
- `codex exec --approve-for-me` 로 변형마다 1장씩, 모두 2장. 두 장 모두 평평한 초록(#00FF00) 배경으로 나왔다.
- `.art/tools/key-out-background.py --key green` 으로 배경을 빼고, alpha bounding box 를 정사각으로 맞춘 뒤 512x512 로 줄였다. 고리 안쪽 배경이 테두리에서 닿지 않아 `enclosed_pixels` 가 크게 찍히지만, 그 픽셀은 피사체가 아니라 고리 안쪽 배경이다. 남은 초록 픽셀은 v1 13개, v2 4개다.

## 후보

- `RallyZone-v2-war-stakes-512.png` (채택) — 얇은 띠에 말뚝 8개. `Assets/Art/Images/Effect/AreaOfEffect/RallyZone.png` 와 같은 파일이다.
- `RallyZone-v1-cracked-ground-512.png` (미채택) — 두꺼운 금 간 지각 띠에 비대칭 뿔.
- `rally-zone-preview.png` — 반지름 2, 45° 카메라(세로 0.707배)로 토템과 `FireSpirit` 2마리를 실제 배율로 합성한 비교.
