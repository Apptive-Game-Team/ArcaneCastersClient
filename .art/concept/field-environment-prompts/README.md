# Field environment — prompt batch

`GameScene` 의 전투 필드 환경 아트를 master style(2.5D cut-paper)로 교체하기 위한
프롬프트 묶음이다. 대상은 `Assets/Art/Images/Background/` 의 레거시 페인터리 아트
6종이며, `PRODUCTION-STATUS.md` 에 등재된 적이 없다.

## 왜 교체하는가

- 현재 나무·바위·풀은 부드러운 유화 브러시 톤이다. `STYLE.md` 의 공유 렌더링 기법
  (큰 매트 종이 면, 값 3단, 외곽선 없음, 브러시 노이즈 없음)과 정면으로 어긋난다.
- `background.png` 는 1800x1000 비정방형인데 `PopupBookGround` 는 정방형 Plane
  (현재 40x40 유닛)에 이 텍스처를 늘여 쓴다. 가로세로가 1.8:1 로 눌린 채
  유닛당 약 45px 밖에 안 되어 화면에서 흐릿하다. 새 아트는 **정방형 + 이음매 없는
  타일링**이어야 한다.

## 씬이 거는 제약

| 항목 | 값 | 출처 |
|---|---|---|
| 카메라 | perspective, `(9, 21, -16)`, X축 45° 회전, FOV 25 | `.agents/docs/scene-space.md` |
| 바닥 | 월드 XZ 평면, `PopupBookGround` (Plane, scale 4 = 40x40 유닛), 머티리얼 `Art/Materials/ground.mat` 의 `_MainTex` | `GameScene.unity` |
| 나무·바위 | 카메라와 같은 45° 로 미리 회전된 빌보드, pivot Bottom Center, scale 1.5 | `GameScene.unity` |
| 정렬 | `TransparencySortMode: CustomAxis (0, 1, 2)` | `ProjectSettings/GraphicsSettings.asset` |

빌보드가 카메라 평면과 평행하므로 나무·바위는 **정면 입면도**로 그린다. 바닥
텍스처만 **바로 위에서 내려다본 평면도**다. `STYLE.md` 의 "three-quarter camera
facing right" 는 캐릭터 규칙이고, 이 두 그룹에는 적용하지 않는다 — 대신 아래 각
프롬프트가 카메라를 명시적으로 고정한다.

## 교체 시 기하 보존

`SKILL.md` 의 "Replacing an existing sprite" 규칙대로 **기존 캔버스와 alpha
bounding box, 바닥 여백을 유지**한다. 씬의 76개 배치 좌표가 현재 크기에 맞춰
손으로 찍혀 있어서, 크기가 달라지면 배치를 전부 다시 잡아야 한다.

| 파일 | 현재 크기 | 목표 | 비고 |
|---|---|---|---|
| `background.png` | 1800x1000 | **2048x2048, 불투명, 이음매 없음** | 정방형 Plane 에 늘여 쓰므로 정방형이어야 한다 |
| `tree_1.png` | 277x242 | 동일 | pivot Bottom Center, 밑동이 캔버스 바닥에 닿아야 한다 |
| `tree_2.png` | 265x222 | 동일 | 위와 같음 |
| `tree_3.png` | 234x218 | 동일 | 위와 같음 |
| `tree_4.png` | 328x264 | 동일 | 위와 같음 |
| `rock.png` | 452x166 | 동일 | 위와 같음 |

나무 4종은 **세트로 함께 나가야 한다**. 한 그루만 cut-paper 로 바뀌면 같은 줄에
선 나머지 세 그루와 기법이 달라 보이는 쪽이 더 나쁘다.

`grass_1.png` / `grass_2.png` 는 이번 요청 범위 밖이지만 같은 레거시 기법이다.
나무 세트가 통과하면 같은 문장으로 이어서 뽑는 것을 권한다.

## 생성 후 절차

1. magenta 키 제거 (`background.png` 은 키 없음 — 불투명 전면).
2. 위 표의 캔버스 크기로 리사이즈, 네 모서리 alpha 0, 64px 실루엣 검수.
3. `Assets/Art/Images/Background/` 로 이동 — 파일명 그대로. 씬은 guid 로 참조하므로
   `.meta` 를 유지하면 배치가 그대로 살아 있다.
4. `PRODUCTION-STATUS.md` 에 행을 추가한다.

## 바닥 텍스처 진행 상황 (이슈 #60)

나무 4종과 바위는 PR #55 에서 교체를 마쳤다. **바닥만 남았다** — 재생성본이
원본보다 나빠 되돌렸고, 후속 작업을 이슈 #60 으로 분리했다.

프롬프트 계보:

| 버전 | 방향 | 결과 |
|---|---|---|
| `ground-background.txt` | 큰 매트 종이 패치 | 경계가 뭉개지고 낱장 잎이 오브젝트로 읽힘 |
| `ground-background-v2.txt` | 경계 또렷한 패치, 잎 제거 | 군복 무늬. 풀 질감 소멸 — 반려 |
| `ground-background-v3.txt` | 풀잎을 표면 전체에 | 건초더미. 잎 하나가 1 유닛 |
| `ground-background-v4.txt` | 넓은 톤 영역 + 성긴 풀 다발 | 방향 전환으로 생성 중단 |
| `ground-background-v5.txt` | low-poly 테셀레이션 | 스타일 앵커 없이 돌리다 중단 |
| `ground-background-v6.txt` | low-poly + 성긴 흙 + MasterStyleKey 첨부 | 방향 확정. facet 이 큼 |
| `ground-background-v7.txt` | facet 가로 70개 | 텍스처는 잘아졌으나 씬에서 굵음 |
| `ground-background-v8.txt` | facet + 각진 풀 조각 + 자갈 3중 레이어 | 묘사 살아남. 축소하면 뭉개짐 |
| `ground-background-v9.txt` | facet 가로 200개, 디테일 전부 축소 | 현재 최선 |

생성물은 `out/background-v*.png` 에 있다.

### 알아낸 제약

- 이미지 생성 출력이 **1254x1254 고정**이고 `PopupBookGround` 가 40 유닛이라
  유닛당 31px 뿐이다. 화면에 보이는 건 평면의 절반이라 텍스처가 2배로 확대된다.
  그래서 프롬프트로 facet 을 잘게 해도 화면에서는 굵게 보인다. **타일링 배수를
  같이 정해야 한다** — 프롬프트만으로는 해결되지 않는다.
- 2x 타일링을 쓰려면 `background.png.meta` 의 `wrapU`/`wrapV` 를 `1`(Clamp)에서
  `0`(Repeat)으로, `Art/Materials/ground.mat` 의 `_MainTex` `m_Scale` 을
  `{2, 2}` 로 바꾼다.
- 밝기는 `-modulate` 로 올리면 채도까지 같이 올라가 누렇게 바랜다. +20% 부터
  바닥이 나무보다 밝아져 필드가 떠 보인다.

### codex 로 생성할 때

`-i`/`--image` 는 variadic 이라 **뒤따르는 프롬프트 문자열까지 이미지 파일
인자로 먹는다.** 프롬프트는 stdin 으로 넘긴다:

```bash
printf '%s' "$PROMPT" | codex exec -s workspace-write -C "$PWD" \
  --image .art/anchors/master-v2/MasterStyleKey.png -
```
