# 2026-10-26 — 인게임 카드 마법 유형 문양 배지 추가

- Date: 2026-10-26
- GitHub Issue: #371
- Status: Implemented; Unity Editor verification pending

## Goal
유닛, 건물, 드랍, 폭발, 슛을 카드 우상단의 작은 원형 문양으로 구분한다.

## Non-goals
원소, 서버 데이터, 카드 비용, 버전은 변경하지 않는다.

## Context / Constraints
기존 `CombinedMagicData.castKind`를 사용한다. UI는 FlatCircle과 잉크색 벡터 문양을 쓴다.
마나는 우하단에 있다. 확대 카드는 CardImage를 상속하지만 CardUI는 제거되어 있다.
기존 작업을 보존하도록 origin/dev에서 feature/371 worktree를 만들었다.

## Approach (Checklist)
- [x] **Step 0: Recon** — 유형 데이터, 카드 프리팹, 확대 카드 상속 확인.
- [x] **Step 1: Implementation** — SVG 5종과 PNG, 재사용 배지 프리팹, 손패·튜토리얼·확대 카드 연결.
- [x] **Step 2: Tests** — YAML 참조·GUID·계층 검사, 16/24px 문양과 오프라인 카드 렌더 검수. Editor 실행은 미확인.
- [ ] **Step 3: Rollout / Rollback** — dev 대상 PR, 문제 시 변경 커밋 revert.

## Validation
- **Commands to run:** 아이콘 SVG 렌더, PNG alpha/크기 검사, YAML 참조 검사, git diff --check, graphify update .
- **Expected output:** 유형 5종 구분, Unknown/null 숨김과 아이콘 제거, 유효→Unknown→유효 재표시, raycast 비활성.
- Unity CLI의 installed/running 목록이 비어 있어 Editor import/Play Mode 검증은 남는다. 오프라인 이미지를 실행 스크린샷으로 표시하지 않는다.
- YAML 검사를 기존 ConfirmPage.prefab에도 적용한 뒤 변경 프리팹 3종의 로컬 참조·중복 anchor·부모/자식 관계를 확인했다. 5종 Sprite의 투명도·128×128 크기·GUID·매핑과 두 확대 씬의 상속을 확인했다.
- 실제 MagicCastKindBadge.cs와 MagicCastKind.cs를 Roslyn으로 컴파일하고 Unity API 대역으로 12개 조건을 확인했다. 5종 매핑, Unknown 시 sprite 제거·비활성, 미래 enum 값, 재바인딩이 통과했다. 이 검사는 Unity import/실행 확인을 대신하지 않는다.
- `graphify update .`는 CLI가 설치되어 있지 않아 실행되지 않았다. 기존 graph.json도 없다.

## Risks & Rollback
- **Risks:** 화면이 작을 때 문양 가독성, 중첩 프리팹 참조 오류.
- **Rollback steps:** 이 기능의 커밋을 revert한다.

## Open Questions
없음.

## Plan Review
- FAST REVIEW: NONPASS → 확대 카드에서 비활성 배지를 포함해 직접 검색, Unknown 시 sprite 제거, 16px 검수 반영.
- MEDIUM REVIEW: PASS — 프리팹 하나와 명시적 Sprite 매핑으로 범위 유지.
- HEAVY REVIEW: PASS — 위 수정과 Editor 검증 한계 반영 후 구현 진행.
