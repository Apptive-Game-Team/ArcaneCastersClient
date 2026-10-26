# 2026-10-26 — 친선전 초대 전달 확인과 친구 접속 상태 표시 추가

- Date: 2026-10-26
- GitHub Issue: #385 (서버 ArcaneCastersLobby#98)
- Status: Implemented; Unity UI verification pending

## Goal
친구 접속 여부와 초대 전달 여부를 구분해 표시한다. 초대 생성 성공만으로 상대에게 전달됐다고 표시하지 않는다.

## Non-goals
초대 저장소 영속화, 다중 서버 초대 공유, 기존 게임 방 생성의 불확실한 실패 및 동시 수락 전체 재설계는 제외한다.

## Context / Constraints
기존 친구 행과 초대 창을 재사용한다. lobby의 기존 미커밋 변경을 보존하기 위해 origin/dev 별도 worktree를 사용한다. 접속 상태와 초대 상태는 따로 보유하고 함께 표시한다.

## Approach (Checklist)
- [x] **Step 0: Recon** 기존 서버 Online 기본값, 초대 생성 응답 무시, 수신창 30초 재시작 확인.
- [x] **Step 1: Implementation** Redis heartbeat TTL 35초, 로그인 중 전역 클라이언트 10초 heartbeat. 친구 DTO만 Offline 지원. 초대 deliveredAt/revision, 수신자 ACK와 최근 송신 초대 조회. FriendManager가 요청과 폴링 소유. 친구 목록은 userId로 기존 행 갱신. ACK 멱등 재시도 및 revision/계정별 응답 가드. 생성 시 서버 온라인 검사.
- [x] **Step 2: Tests** 서버 접속 TTL·초대 ACK 권한/멱등·상태조회, 클라이언트 계약·만료·낡은 응답 처리 검증 및 컴파일.
- [ ] **Step 3: Rollout / Rollback** 서버 먼저 배포 후 클라이언트 배포. DB migration 없음.

## Validation
- **Commands to run:** gradlew test, dotnet build, git diff --check, graphify update .
- **Expected output:** 대상 테스트 실제 실행과 컴파일 성공. Unity 두 계정으로 전달·오프라인·취소·재접속 확인은 Editor가 있을 때 수행.

## Risks & Rollback
- **Risks:** heartbeat가 끊긴 후 최대35초 동안 online 유지. 구버전 클라이언트는 heartbeat/ACK를 보내지 않음. 초대는 기존 단일 프로세스 메모리 저장소이며 서버 재시작 시 복구되지 않음. 기존 accept 동시 요청/불확실한 방 생성 문제는 별도 후속 작업.
- **Rollback steps:** 서버·클라이언트 변경 커밋 revert.

## Open Questions
없음. 전달 완료는 상대가 읽었다는 의미가 아니라 클라이언트가 초대 창 표시를 확인했다는 의미이다.

## Review
Fast: ACK 멱등 재시도·서버 상태 검사·응답 역전 가드 반영. 방 생성 불확실성은 별도 범위로 남기며 FAILED 상태를 새로 도입하지 않음.
Medium: 접속/초대 상태 독립 표시 및 기존 행 재사용 반영.

Heavy: accept 완료 시 최신 레코드를 원자적으로 갱신해 deliveredAt/revision을 보존하는 조건으로 PASS.

## Verification results
클라이언트 및 새 테스트 코드 컴파일 통과. 순수 계약 테스트 4개를 Unity Mono 런타임에서 실행해 통과. Editor가 프로젝트를 점유하여 Unity batch 테스트 및 실제 두 계정 UI 검증은 미완료. graphify 명령 미설치로 그래프 갱신 불가.

