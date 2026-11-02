# 2026-10-29 — 모션 코믹 사선 컷과 말풍선 적용

- Date: 2026-10-29
- GitHub Issue: #391 / PR #392
- Status: Ready for review

## Goal
7개 PNG를 그대로 사용하면서 가로 화면 하나에 사선 컷을 누적한다. 검정·흰색 컷 테두리와 종이 설명 상자, 붉은 외침, 흰 말풍선으로 원본 만화의 인상을 반영한다.

## Non-goals
신규 그림/음성 생성, 이미지 파일의 영구 자르기, 온보딩 진입·대사·입력 동작 변경, 버전 변경.

## Context / Constraints
기존 feature/391 / PR #392에 후속 반영. 시작 시 Jua SDF.asset 변경이 이미 있으므로 보존한다. Graphify 그래프 없음. 기존 ComicIntroPlayer/ComicPanelView와 UI 프리팹을 재사용한다. 마스크 안에서만 그림을 확대/이동하고 테두리·말풍선은 고정한다. 그림은 EnvelopeParent로 틀을 채우므로 인물/눈/세계수의 잘림을 실제 캡처로 확인한다.

## Approach (Checklist)
- [x] **Step 0: Recon** 기존 프리팹/Editor generator, 테스트, MCP Editor 상태 확인.
- [x] **Step 1: Implementation** 작은 ComicShapeGraphic(MaskableGraphic)은 직렬화된 normalized 경계점/색/선폭만 그린다. 도형 생성은 ComicIntroSetup에만 둔다. 중심에서 보이는 다각형 채우기/외곽선을 생성하며, 컷은 볼록한 사선 사각형, 말풍선은 중심에서 보이는 종이/방사형 외침/타원+꼬리 형태만 사용한다. 기존 RectMask2D를 표준 UI Mask+Graphic으로 교체하고 검정 바깥선·흰 내부선을 둔다. 기존 Image와 Graphic을 중복 부착하지 않고 전용 shape 오브젝트를 만든다. 1280×720 가로 화면에서 7개 컷이 밀착/겹치며 순서대로 나타나도록 재배치한다. 테두리/마스크/대사는 고정 계층에 두고 Artwork만 확대/이동한다. 흐름 스크립트는 유지한다. 모든 시각 구조는 Editor에서 프리팹에 저장한다.
- [x] **Step 2: Tests** EditMode에서 실제 polygon mask, mesh 범위/삼각형, 한국어·영어 대사 넘침을 확인한다. 기존 가림 검사의 AABB 비교를 실제 후속 컷 polygon/외곽선과 대사 안전 텍스트 영역의 교차 검사로 바꾼다. 새 장식 Graphic은 raycastTarget=false로 두고 실제 EventSystem/GraphicRaycaster로 그림과 대사 위의 입력이 진행 버튼으로 전달되며 skip 버튼을 가리지 않는지 검사한다. 기존 PlayMode 2개를 재실행해 누적/완료/스킵을 보존. Unity MCP의 실제 최종 화면과 4:3 화면을 캡처해 빈 공간·잘림·말풍선 꼬리·외침 선을 점검.
- [x] **Step 3: Rollout / Rollback** 기존 PR을 새 실제 캡처와 검증 결과로 갱신. 사용자가 제공한 작업 규칙에 따라 commit/push하며 root submodule pointer는 dev 병합 전 변경하지 않는다. 되돌릴 때 이 후속 커밋만 revert하면 사각형 판으로 돌아간다.

## Validation
- **Commands to run:** Unity MCP refresh/compile, 지정 EditMode/PlayMode tests, 실제 Game View 캡처; prefab/scene fileID 검사; scoped git diff --check; root graphify update . 명령 가용성 확인.
- **Expected output:** 원본 PNG 변경 없음. 한 화면 7컷과 8대사 유지. 각 컷은 사선 마스크 안에서만 표시되며 대사는 읽을 수 있다. 컴파일/재생 오류 없음.

## Risks & Rollback
- **Risks:** 사용자 정의 polygon fan은 중심에서 모든 경계가 보이는 모양만 지원한다. Mask가 stencil을 쓰므로 실제 UI 렌더 확인이 필수다. 좁은 컷은 피사체의 중요한 부분을 잘라내거나 말풍선이 겹칠 수 있다.
- **Rollback steps:** 후속 변경 커밋만 되돌린다. 원본 PNG는 유지한다. 초기 dirty font는 시작 시점의 상태로 보존하고 HEAD로 되돌리거나 커밋하지 않는다.

## Open Questions
없음. 원본의 세로 페이지를 그대로 축소하지 않고 가로 구도로 재배치한다.

## Plan Review
- FAST REVIEW: NONPASS → 후속 컷 AABB 대신 polygon 교차 검사, 실제 UI raycast 검사 추가로 반영.
- MEDIUM REVIEW: PASS → runtime은 경계점 그리기만 담당. 도형 생성은 Editor에 한정하고 기존 Image와 중복 부착하지 않음. Artwork 외 계층은 고정.

- HEAVY REVIEW: PASS.
- EditMode 6/6 통과. UI raycast 검사는 실제 렌더 깊이가 필요한 PlayMode로 이동했다.
- 사용자 정의 Graphic의 CanvasRenderer 연결과 열린 씬 덮어쓰기 오류를 보완했다.

## Final Results
- EditMode 6/6, PlayMode 3/3 통과. 실제 raycast hit에서 ExecuteHierarchy로 진행/스킵 버튼까지 전달되는 경로를 검사했다.
- Unity MCP 실제 1920×1080 및 1024×768 화면을 확인했다. 모든 7개 컷과 8개 대사, 말풍선 꼬리/붉은 외침/종이 상자와 사선 테두리 표시 확인.
- 지팡이 끝과 마지막 눈이 보이도록 첫/마지막 그림의 pivot을 상단으로 조정했다. 그림 파일과 진행 스크립트는 변경하지 않았다.
- 최종 Unity 콘솔 오류 0개. 신규 프리팹 fileID 검사 및 scoped whitespace 검사 통과.
- WebGL player 빌드/브라우저 검증은 미실행. Graphify update .는 CLI 미설치로 실행하지 못했다(기존 그래프 없음).
- 실제 캡처: docs/pr-media/391/comic-diagonal.jpg, comic-diagonal-first.jpg. 사용자 dirty font는 시작 시 백업을 기준으로 보존한다.
