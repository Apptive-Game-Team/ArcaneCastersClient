# 2026-11-02 — 게임 씬 진입 전 STOMP 연결 완료 대기 추가

- Date: 2026-11-02
- GitHub Issue: #400
- Status: Validated

## Goal

로비에서 WebSocket과 STOMP CONNECTED를 기다린 뒤, 완료된 연결을 유지하며 게임 씬으로 전환한다. 2초마다 진행 중인 핸드셰이크를 닫던 최초 연결 재시도를 수정한다.

## Non-goals

게임 서버의 DB 조회와 월드 초기화 순서를 변경하지 않는다. 기존 서버는 이 준비를 마친 뒤 매칭 정보를 응답한다. 로비 timeout과 게임 종료 통보401은 별도 서버 이슈로 유지한다.

## Context / Constraints

실제 deploy 세션 bot-156의 WebSocket3회가 모두 HTTP101로 열렸지만2초 간격 재시도와 STOMP DISCONNECT가 반복됐다. 실제 stompjs2.3.3+플러그인 재현에서2.2초 연결은 기존2초 예산으로 실패하고 긴 예산으로 성공했다. 개별 왕복의 실측 지연 원인은 확정하지 않았다.

## Approach (Checklist)
- [x] **Step 0: Recon** 배포 이미지/소스, 생성 응답, 브라우저/서버/nginx 로그 대조.
- [x] **Step 1: Implementation** GameServerConnection이 고유 callback 대상과 persistent transport를 소유한다. GameSceneEntry가 CONNECTED 뒤에만 씬을 전환한다. 최초 연결은 시도당10초, 최대3회, 실패 후1초 대기한다. 게임 씬은 같은 연결을 인계받고 프레임 구독을 시작한다. WebGL/Native의 이전 연결 callback과 종료가 새 연결을 변경하지 못하게 한다. 매칭 준비 안내와 연결 중 표시, 실패 후 세션 재검증 및 재시도를 제공한다.
- [x] **Step 2: Tests** 실제 Unity에서 지연 연결, 소유자 취소, 씬 인계, 실패 후 현재 씬 유지를 검증한다. JS bridge와 실제 Native 소스 회귀 runner 및 Editor/WebGL C# compile을 확인한다.
- [ ] **Step 3: Rollout / Rollback** dev 대상 draft PR을 작성한다. merge/deploy는 수행하지 않는다.

## Validation
- **Commands to run:** node --test scripts/tests/stomp-plugin.test.cjs; dotnet run --project scripts/tests/native-stomp/native-stomp.csproj; Unity EditMode GameServerConnectionTests/ConnectAttemptScheduleTests/StompConnectGateTests; git diff --check.
- **Expected output:** CONNECTED 전에는 게임 씬을 로드하지 않는다. 씬 인계로 두 번째 socket이 생성되지 않는다. 오래된 callback과 파괴된 host의 종료가 새 연결을 닫지 않는다. 실패하면 현재 화면에서 다시 시도할 수 있다.

## Risks & Rollback
- **Risks:** native 비동기 callback 경쟁, 같은 프레임의 이전 host 파괴, 중복 진입, 준비 도중 화면 이탈, 종료된 세션 재시도, 실패 뒤 passive ticket 갱신이 재시도 버튼을 가리는 경우.
- **Rollback steps:** 해당 client commit과 parent pointer commit을 revert한다. DB migration/version bump는 없다.

## Open Questions

실제 itch WebGL과 deploy 서버에서 로그인 후 연결 완료/씬 인계/경기 진행을 최종 확인한다.
검증 결과: Unity EditMode 26/26 통과, JS bridge 6/6 통과, 실제 Native transport 회귀 runner 통과, Editor/WebGL 조건부 C# compile 통과. 실제 itch WebGL 로그인/경기 진행은 미검증이며 draft PR에서 확인한다.
