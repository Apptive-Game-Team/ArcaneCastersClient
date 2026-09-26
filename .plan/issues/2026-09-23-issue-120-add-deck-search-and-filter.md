# 2026-09-23 — 덱 수정 화면에 도감식 검색·정렬·필터 추가

- Date: 2026-09-23
- GitHub Issue: #120
- Status: Complete

## Goal

덱 수정 화면의 보유 카드 목록에서 카드 이름 검색, 이름·속성·마나 비용 정렬, 원소 필터를 함께 사용할 수 있게 한다.

## Non-goals

- 덱 구성 규칙이나 서버 API를 변경하지 않는다.
- 도감 화면의 UI나 필터 동작 자체를 변경하지 않는다.
- 카드 잠금 조건과 해금 진행도 표시를 변경하지 않는다.

## Context / Constraints

- Unity 2022.3 프로젝트이며 기존 `ManageDeckScene`의 씬 작성 UI 패턴을 유지한다.
- 카드의 표시 이름은 `Magic` 로컬라이제이션 표를 사용하고, 속성과 마나 비용은 `CombinedMagicData`에서 읽는다.
- 검색·정렬·필터가 동시에 적용되어야 하며 기존 카드 선택과 hover 팝업 동작이 보존되어야 한다.

## Approach (Checklist)

- [x] **Step 0: Recon** (`MagicBookFilterController`, `MagicInfoFactory`, `DeckManagementController`, `DeckManagementView`, `ManageDeckScene` 비교)
- [x] **Step 1: Implementation** (보유 카드 검색·정렬·필터 상태와 컨트롤러 추가, 씬에 도감식 컨트롤 배치 및 직렬화 연결)
- [x] **Step 2: Tests** (C# 컴파일, 씬·프리팹 직렬화 참조 확인, 로컬라이제이션 테이블 수 일치, `git diff --check`)
- [x] **Step 3: Rollout / Rollback** (별도 플래그·마이그레이션 없이 클라이언트 기능으로 배포, 문제 시 단일 기능 커밋 revert)
- [x] **Step 4: UI polish** (영문 드롭다운 캡션 자동 축소·말줄임, 검색창에 기존 갈색 9-slice UI 배경 적용)

## Validation

- **Commands to run:** `dotnet build Assembly-CSharp.csproj -v minimal`, `git diff --check`, 씬 YAML의 스크립트/필드 참조 검사
- **Expected output:** 컴파일 오류 없음, 공백 오류 없음, 검색·정렬·원소 필터를 조합해도 카드 추가와 잠금 표시가 정상 동작

## Risks & Rollback

- **Risks:** 비동기 로컬라이제이션 완료 전 목록이 잠시 기본 순서로 보이거나, 씬 직렬화 참조가 누락될 수 있다.
- **Rollback steps:** 기능 커밋을 `git revert`하여 새 컨트롤러와 씬 연결을 함께 되돌린다.

## Open Questions

- 없음. 도감의 현재 원소 필터와 정렬 축을 기준으로 구현한다.
