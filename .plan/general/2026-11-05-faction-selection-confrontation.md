# 2026-11-05 — 세력 선택 화면 대치 구도 적용

- Date: 2026-11-05
- GitHub Issue: #425
- Status: Implemented

## Goal
승인한 시안대로 불타는 군단과 세계수 정령을 대치시킨다. 원본 스프라이트를 배치하고 각 영역의 호버와 키보드 포커스에 따라 배경과 유닛 강조를 전환한다. 설명 문구는 표시하지 않는다.

## Non-goals
서버 선택 계약, 시작 카드, 전투 튜토리얼과 기존 보상 패널은 변경하지 않는다.

## Context / Constraints
feature/397에서 분기한 feature/425를 별도 worktree에서 작업한다. 원래 checkout의 기존 사용자 변경을 보존한다. 선택 컨트롤러의 로딩·재시도·중복 요청 방지는 유지한다. UI는 기존 prefab으로 Editor에서 구성한다.

## Approach (Checklist)
- [x] **Step 0: Recon** 공유 패널과 선택 컨트롤러, 실제 유닛 및 UI prefab 확인.
- [x] **Step 1: Implementation** 선택 전용 prefab, hover/focus presenter, 재생성용 Editor builder 추가. 기존 컨트롤러에 선택 전용 view 연결.
- [x] **Step 2: Tests** 실제 prefab 렌더, 영역 클릭과 포커스·전환·비활성화 검사 및 기존 튜토리얼 테스트 실행.
- [x] **Step 3: Rollout / Rollback** 선택 scene와 전용 view만 연결하며 기존 panel은 상태·오류 표시용으로 유지.

## Validation
- **Commands to run:** Unity Editor import, 전용 builder/capture, FactionTutorialTests 및 선택 UI 테스트, git diff --check.
- **Expected output:** 원본 sprite 참조, 두 진영의 중앙 대치, 호버 배경과 강조, 설명 문구 없음, 클릭은 한 번만 전달.
- **Result:** Unity 2022.3.34f1 컴파일과 UI fixture 렌더(한국어·영어, 16:9·4:3), Edit Mode 9개와 Play Mode 8개 통과. 인증된 실제 계정의 서버 선택·튜토리얼 완료는 검증하지 않았다. Graphify 실행 파일이 없어 graph update는 실행하지 못했다.

## Risks & Rollback
- **Risks:** 작은 화면의 유닛 겹침과 입력 영역, 비동기 로딩 중 중복 선택.
- **Rollback steps:** 선택 view 참조와 scene 연결을 되돌리면 기존 panel 선택이 동작한다.

## Open Questions
없음.

## Plan Review
FAST: 공유 panel 루트 비활성화가 controller 코루틴을 중단하는 문제를 지적했다. 같은 루트를 유지하고 Canvas와 GraphicRaycaster만 전환하도록 수정했다.
MEDIUM: PASS. 선택 전용 view와 기존 상태 panel로 범위를 한정했다.
HEAVY: PASS. 첫 방향키 입력으로 선택 영역에 포커스가 진입하도록 처리하고 explicit navigation을 구성했다.
