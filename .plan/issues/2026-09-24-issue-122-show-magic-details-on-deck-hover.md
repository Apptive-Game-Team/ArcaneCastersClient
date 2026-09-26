# 2026-09-24 — 덱 편집 카드 호버에 도감 상세 정보를 표시한다

- Date: 2026-09-24
- GitHub Issue: #122
- Status: Implemented

## Goal

덱 수정 페이지의 마법 목록에서 마법에 마우스를 올리면 도감과 동일한 전투 수치 및 설명을 기존 상세 정보 영역에 표시한다.

## Non-goals

- 덱 검색·필터 UI 또는 카드 목록 배치를 변경하지 않는다.
- 도감 자체의 레이아웃을 재설계하지 않는다.
- 서버의 마법 데이터나 로컬라이제이션 키를 변경하지 않는다.

## Context / Constraints

- `MagicBookScene.MagicInfo`는 전투 수치와 도감 설명을 조합하지만, `DeckScene.DeckInfoMagicPopup`은 설명만 표시한다.
- 카드 hover에 실제로 표시되는 영역은 정보 버튼의 `DeckInfoMagicPopup`이 아니라 `HaveCardMagicPopup`이다.
- 두 화면이 같은 상세 텍스트 생성 로직을 공유해야 이후 변경에서도 정보가 어긋나지 않는다.
- 기존 hover 지연, 상세 창 위치, pointer exit 동작은 유지한다.

## Approach (Checklist)
- [x] **Step 0: Recon** (`MagicInfo`, `DeckInfoMagicPopup`, hover 이벤트와 기존 상세 영역 확인)
- [x] **Step 1: Implementation** (공유 상세 텍스트 생성기 추가, 도감과 덱 hover에서 함께 사용)
- [x] **Step 1a: Correct hover surface** (`HaveCardMagicPopup`의 기존 그림·속성 행을 도감 상세 카드로 교체)
- [x] **Step 1b: Runtime layout fix** (기존 `GridLayoutGroup`과 `VerticalLayoutGroup` 중복 추가로 발생하던 hover `NullReferenceException` 제거)
- [x] **Step 1c: Description typography** (설명 말줄임을 줄바꿈 표시로 변경하고 도감 설명과 동일한 Pretendard ExtraBold 폰트 적용)
- [x] **Step 2: Tests** (C# 컴파일 성공; Computer Use에 Unity 창이 없고 Unity MCP에 연결된 인스턴스가 없어 실제 hover 확인은 제한됨)
- [x] **Step 3: Rollout / Rollback** (데이터 마이그레이션 없음, 커밋 revert로 롤백)

## Validation
- **Commands to run:** `dotnet build Assembly-CSharp.csproj -v minimal`, 관련 EditMode 테스트, Unity MCP로 ManageDeckScene hover 확인
- **Expected output:** 컴파일 오류 없음. 도감과 hover 상세 창이 같은 `MagicBookDetailText.BuildAsync` 결과를 사용함.

## Risks & Rollback
- **Risks:** 비동기 로컬라이제이션 완료 전에 다른 마법으로 hover가 이동할 수 있음; 기존 request id 검증으로 오래된 결과를 무시한다.
- **Rollback steps:** 이 이슈의 커밋을 `git revert`한다.

## Open Questions
- 없음
