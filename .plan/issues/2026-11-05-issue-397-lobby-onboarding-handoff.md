# 2026-11-05 — 세력 전투 뒤 기존 로비 튜토리얼 연결

- Date: 2026-11-05
- GitHub Issue: #397
- Status: Complete

## Goal

세력 전투와 보상 안내를 마치면 기존 덱 버튼 안내부터 덱 관리, 마법서,
로비 기능, 연습 전투와 마무리 안내까지 이어진다.

## Non-goals

기존 안내 내용, 전투 규칙, 보상과 서버 API는 바꾸지 않는다.

## Context / Constraints

`feature/397`, PR #398에 이어서 수정한다. 기존 매니저는 `TutorialScene`에만
있으며 새 흐름은 그 씬을 거치지 않는다. 로비 데이터 초기화 이후 매니저를
생성해야 하며 `Start`가 전투 단계로 되돌리지 않아야 한다.

## Approach (Checklist)

- [x] **Step 0: Recon** 기존 상태 전환과 새 전투의 완료 저장, 로비 초기화 확인.
- [x] **Step 1: Implementation** `CompleteBattle`은 계정·서버별 `lobby` 체크포인트를
  저장한다. 로비 초기화 후 대기 상태일 때만 `GlobalTutorialManager`를 생성하고
  `DeckState`부터 시작한다. 기존 인스턴스는 재시작하지 않는다. 마지막 안내에서만
  `completed`를 저장하고, 계정·서버 변경 시 진행 중 매니저는 저장 없이 종료한다.
  최종 완료 콜백에서도 소유자를 확인해 `Update`보다 먼저 호출되는 경우를 보호한다.
  로비 안내 중에는 덱을 먼저 조회하고 성장·퀘스트 보상 조회는 안내가 끝난 뒤
  실행한다. 보상 API가 응답하지 않아도 덱 안내와 화면 이동을 막지 않는다.
- [x] **Step 2: Tests** 전투 완료와 최종 완료 구분, 재접속 복구, 기존 완료 사용자
  제외, 중복 시작 방지, 매니저의 다음 프레임과 계정 전환을 검증한다.
- [x] **Step 3: Rollout / Rollback** 기존 PR #398에 커밋한다. 마이그레이션은 없다.

## Validation

- **Commands to run:** Unity EditMode `FactionTutorialTests`, PlayMode 튜토리얼
  테스트와 로비 연결 fixture, `git diff --check`, 루트 `graphify update .`.
- **Expected output:** 새 전투 뒤 덱 안내가 시작되고 기존 안내가 끝날 때만
  완료된다. 중단 후 재접속 시 전투를 반복하지 않고 덱 안내부터 재시작한다.

- **결과:** Unity 2022.3.34f1 EditMode 14개와 PlayMode 10개 통과.
  실제 LobbyScene, ManageDeckScene, MagicBookScene에서 기존 안내 콜백을 진행해
  연습 전투 안내까지 도달함을 확인했다. 네트워크 전투 드라이버는 fixture에서
  비활성화하고 표시 Update를 직접 실행해 서버 완료 응답의 로비 안내 저장을 검증했다.
  완료 계정 제외, 중복 시작, 마지막 완료 저장과 계정 변경 직후 완료 거부도 확인했다.
  원격 브랜치의 세력 선택 화면 변경 위에 적용한 뒤 다시 검증했다.
- **제한:** 실제 서버 계정 E2E와 WebGL 빌드는 실행하지 않았다.
  루트 `graphify update .`는 CLI가 설치되지 않아 실행하지 못했다.

## Risks & Rollback

- **Risks:** 로비 초기화와 씬 전환 순서, 영속 매니저의 계정 범위.
- **Rollback steps:** 이 연결 커밋을 revert한다.

## Open Questions

없음. 중단 후에는 세부 설명 위치 대신 덱 버튼 안내부터 재개한다.

## Plan Review

- FAST REVIEW: NONPASS → 최종 완료 콜백의 계정·서버 확인과 같은 프레임 테스트 반영.
- MEDIUM REVIEW: PASS → 선택적 보상 요청을 안내 이후로 미루고 기존 completed 유지 명시.
- HEAVY REVIEW: PASS → 최종 소유자 확인과 선택적 요청 분리를 검토함.
- Rejected feedback: 없음.
