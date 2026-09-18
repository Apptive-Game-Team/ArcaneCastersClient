# 남은 이펙트 교체 (이슈 #65)

`.art/PRODUCTION-STATUS.md` 의 `옛 스타일로 남은 게임 플레이 자산` 표를
처리한다. 이슈 #65 의 대조에서 찾은, 그때까지 문서 어느 표에도 행이 없던
자산들이다.

`make-game-art` 스킬의 지시대로 프롬프트를 먼저 전부 쓰고 commit 한 뒤에
생성을 시작한다. `image_gen` 은 짧은 창(25~30장, 약 4시간)과 주간 상한
두 가지로 제한되고, 2026-09-15 에 주간 상한에 걸려 다음 생성이 닷새 뒤로
밀린 적이 있다. 프롬프트가 repository 에 있으면 생성이 막혀도 이어받을 수
있다.

## 대상

| 프롬프트 | 출력 경로 | 원본 크기 | 진영 팔레트 |
|---|---|---|---|
| `razor-gale-frame-0.txt` | `Assets/Resources/Game/explode/razor_gale_frame_0.png` | 192x155 | Wind |
| `razor-gale-frame-1.txt` | `Assets/Resources/Game/explode/razor_gale_frame_1.png` | 192x155 | Wind |
| `electric-explode-frame-0.txt` | `Assets/Resources/Game/explode/electric_explode_frame_0.png` | 192x116 | Lightning |
| `electric-explode-frame-1.txt` | `Assets/Resources/Game/explode/electric_explode_frame_1.png` | 192x116 | Lightning |
| `projectile-wind.txt` | `Assets/Resources/Projectiles/wind.png` | 512x91 | Wind |
| `cloud-halo.txt` | `Assets/Resources/Game/cloud.png` | 510x512 | Wind |
| `background-grass-1.txt` | `Assets/Art/Images/Background/grass_1.png` | 238x117 | Nature |
| `background-grass-2.txt` | `Assets/Art/Images/Background/grass_2.png` | 466x99 | Nature |

생성 순서는 이 표 순서다. 눈에 가장 먼저 걸리는 `razor_gale` 부터 간다.
`background-grass-2.txt` 는 마지막이다 — `grass_2.png` 은 어느 씬에서도
참조가 없어서, 남은 창을 다른 자산에 쓰는 편이 낫다.

`Assets/Resources/Game/explode/lightning_explode.png` 은 이 목록에 없다.
`Assets` 의 prefab·scene 과 database 의 `V001_20260916__baseline.sql` 양쪽
모두에서 참조가 0건이라, 교체가 아니라 삭제 대상이다.

## 프레임 쌍을 맞추는 방법

`razor_gale` 과 `electric_explode` 는 각각 두 장이 `SpriteFrameAnimator` 로
0.12초 간격에 `loop: 1` 로 도는 두 프레임이다. pivot 은 둘 다 중앙
`(0.5, 0.5)`, `spritePixelsToUnits` 는 100, 두 프레임의 캔버스 크기가 같다.
프레임이 바뀔 때 이펙트가 튀지 않으려면 두 장의 캔버스 크기와 배율이 같아야
한다.

twin-panel(한 캔버스에 두 프레임을 나란히 그리고 반으로 자르기)은 쓰지
않는다. `.art/concept/frame-pairs/README.md` 에 적힌 대로 `CloudDragon` 에서
열 번 넘게 실패했고, 생성기가 두 자세를 넣으려고 본체를 계속 줄였다.

대신 `RockTurret` 이 통과한 방법을 쓴다.

1. frame 0 을 단독으로 생성해 확정한다.
2. 확정한 frame 0 을 `-i` 레퍼런스로 붙여 frame 1 을 생성한다.
3. 두 장의 alpha bounding box 의 **합집합**을 공유 crop box 로 삼고, 배율
   하나를 두 장에 똑같이 적용해 함께 내보낸다.
4. `.art/tools/check-frame-pair.py` 로 확인한다.

## 키 색

전부 `--key magenta` 다. Wind 는 민트 녹색이고 Nature 는 잎 녹색이라
`--key green` 이 자산을 먹는다. Lightning 은 금색이라 red 와 green 이 둘 다
높아서 역시 magenta 가 안전하다. `STYLE.md` 의 어느 팔레트에도 magenta 는
없다.

```bash
.art/tools/key-out-background.py raw.png cut.png --key magenta
.art/tools/finalize-candidate.py cut.png <출력 경로> --max-size <원본 긴 변>
```

`key-out-background.py` 가 찍는 `enclosed_pixels` 가 0이 아니면 키 색이
자산에 앉은 것이다. 허용치를 늘리지 말고 키 색을 바꾼다. 다만 `razor_gale`
과 `electric_explode` 는 갈래 사이가 트여 있어 실제 구멍으로 0이 아닌 값이
나올 수 있다 — `ShockOverload`(11145)와 `RazorGale`(16368)이 그랬다. 그때는
불투명 픽셀에 magenta 잔색이 0px 인지로 판정한다.

## 자산당 시도 상한

한 자산에 세 번까지만 생성한다. 세 번에 안 되면 뒤로 미루고 방식을 바꾼다.
막힌 자산에 창을 다 쓰면 나머지가 전부 다음 창으로 밀린다.

## 결과 (이슈 #76, 2026-09-18)

여덟 장 중 일곱 장을 적용했다. 생성물은 전부 `out/` 에 있다 — `-raw` 는
`image_gen` 이 돌려준 그대로이고, `-cut` 은 거기에
`key-out-background.py --key magenta` 만 돌린 것이다. 반려한 것도 남겨 뒀다.
`image_gen` 한도 때문에 다시 뽑는 것이 프롬프트를 다시 쓰는 것보다 훨씬 비싸다.

| 자산 | 통과한 시도 | 총 시도 | 비고 |
|---|---|---|---|
| `razor_gale_frame_0` | v1 | 1 | |
| `razor_gale_frame_1` | v1 | 1 | 확정한 frame 0 을 `-i` 로 붙였다 |
| `electric_explode_frame_0` | v1 | 1 | `image_gen` 이 magenta 대신 alpha 를 돌려줬다 |
| `electric_explode_frame_1` | v1 | 1 | frame 0 을 magenta 위에 올려 레퍼런스로 줬다 |
| `wind` | 없음 | 3 | 전부 기하 반려, 옛 그림 유지. `PRODUCTION-STATUS.md` 참조 |
| `cloud` | v2 | 2 | v1 은 가로로 퍼져서 반려 |
| `grass_1` | v3 | 3 | v1·v2 는 폭이 모자라 반려 |
| `grass_2` | v1 | 1 | 확정한 `grass_1` 을 `-i` 로 붙였다 |

`-v2.txt`, `-v3.txt` 는 실제로 통과한 프롬프트다. 원본 `.txt` 는 처음 쓴 그대로
두고 고치지 않았다. 둘의 차이가 이 batch 가 배운 것이다 — 그림이 캔버스 네 변에
닿아야 한다고 적고, 가로세로 비를 말이 아니라 숫자와 예시 픽셀 크기로 적는다.

프레임 쌍 둘은 `RockTurret` 방법대로 frame 0 을 먼저 확정하고, 두 장의 alpha
bounding box 합집합을 공유 crop box 로 삼아 배율 하나를 둘에 똑같이 적용해
내보냈다. 재기 전에 alpha 8 미만을 0 으로 내려야 한다 —
`.art/ANIMATION-ASSETS.md` 의 `폭발 이펙트 프레임 쌍` 절에 이유를 적었다.
같은 절에 `alignment: 7` 함정도 있다. 네 장 다 `spritePivot` 은 `(0.5, 0.5)` 로
적혀 있지만 런타임 피벗은 Bottom Center 라, 남는 세로 여백을 가운데로 나누면
이펙트가 그만큼 뜬다.
