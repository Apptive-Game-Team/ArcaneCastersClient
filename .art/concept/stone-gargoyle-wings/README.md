# 돌 가고일 날갯짓 3프레임

2026-11-05 기존 돌 가고일을 기준으로 내장 image_gen에서 세 자세를 한 시트로 생성했다. 사용자는 중간 자세를 거쳐 되돌아오는 `1 → 2 → 3 → 2 → 1 → 2…` 왕복을 선택했다.

| 파일 | 자세 |
| --- | --- |
| `StoneGargoyleFly1.png` | 날개를 위로 든 자세 |
| `StoneGargoyleFly2.png` | 날개를 수평으로 편 중간 자세 |
| `StoneGargoyleFly3.png` | 날개를 아래로 내린 자세 |

`StoneGargoyle.prefab`에 기존 `SpriteFrameAnimator`를 붙였다. 세 PNG를 `1, 2, 3, 2` 배열로 연결하고 반복하면 경계의 1을 공유하면서 `12321` 왕복이 이어진다. 프레임 간격은 0.16초, 한 주기는 0.64초다. 서버 전투 동작과 스탯은 바꾸지 않는다.

세 프레임은 192x165 RGBA, PPU 100, Bottom Center 피벗과 같은 캔버스를 사용한다. 원본을 724x724 세 칸으로 나누고 눈의 가로 위치와 발톱 하단을 맞춘 뒤 하나의 공유 크롭 박스와 배율로 축소했다. 프레임마다 별도로 잘라 몸통이 움직이는 문제를 피했다. Full Rect mesh를 사용해 날개 높이에 따라 Sprite bounds와 체력바 기준이 달라지지 않도록 했다. 수치는 `alignment.json`에 있다.

`review-sheet.png`는 밝고 어두운 바탕에서 세 자세를 비교한다. `wing-flap-pingpong-preview.gif`는 선택한 왕복 루프다. `wing-flap-preview.gif`는 최초 제안인 123 순환이며 현재 프리팹은 사용하지 않는다. 생성 프롬프트는 `../stone-gargoyle-wing-prompts/three-frames.txt`다.

파란 키 배경을 제거했고 날개와 몸통 사이의 닫힌 빈틈도 투명하게 처리했다. 세 프레임의 바닥 접점은 같으며 네 모서리 alpha 0, 불투명 픽셀의 파란 잔색 0이다.

## Unity 검증

Unity 2022.3.34f1에서 저장된 프리팹을 그대로 로드해 Rust 전투 기록을 `MagicPreview`로 재생했다. 프레임 배열 `1,2,3,2`, 간격 0.16초, loop 활성화와 세 Sprite bounds 일치를 런타임에서 확인했다. 0.1초마다 수집한 137개 샘플의 중복 자세를 접으면 `1,2,3,2` 순서가 반복된다. 지상 적 네 마리에 대한 강하·타격·상승을 포함한 13.7초 GIF는 960x540, 10fps, 2,566,610bytes다. 실제 서버 접속 영상이 아니라 실제 Rust 시뮬레이션 결과를 Unity 렌더러에서 재생한 녹화다.

EditMode `ObjectSfxTests.CatalogCoversPrefabsAndHasNoLegacySoundOwnersOrMissingScripts` 1개가 통과했다. 소스 프레임·타임스탬프·MP4는 모노레포의 무시 경로 `.captures/stone-gargoyle-wing-pingpong-20261105/`에 보관했다. PR 검수용 비교 시트와 간단한 루프 GIF는 `docs/pr-media/415/`에 둔다.
