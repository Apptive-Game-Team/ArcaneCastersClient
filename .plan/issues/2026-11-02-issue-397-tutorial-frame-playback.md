# 2026-11-02 — 튜토리얼 이동 프레임 재생 수정

- Date: 2026-11-02
- GitHub Issue: #397, Game #186
- Status: Implemented and verified

## Goal

실제 튜토리얼의 이동·공격이 HTTP 응답 주기로 뭉쳐서 재생되는 현상을 수정한다.
일반 전투의 위치 보간과 이벤트 표현을 그대로 사용하고 서버 20Hz 간격을 유지한다.

## Non-goals

전투 수치, 물리, 전체 네트워크 전송, 일반 전투의 PositionUpdater는 변경하지 않는다.
WebSocket 전환이나 별도 클라이언트 전투 시뮬레이션을 만들지 않는다.

## Context / Constraints

현재 드라이버는 응답 후 150ms를 기다리고, 받은 모든 프레임을 한 Unity 프레임에서
적용한 뒤 전체 snapshot으로 같은 위치·Attack 상태를 다시 갱신한다. PositionUpdater는
새 갱신마다 직전 목표 위치로 이동한 뒤 50ms tween을 시작하므로 이 경로가 순간 이동과
정지를 반복한다. 현재 마지막 프레임 번호는 tick뿐 아니라 CAST capture에도 증가한다.
수신 watermark와 재생 위치를 분리해야 하며, frame 번호를 50ms 시간으로 해석하면 안 된다.
기존 font 변경(LilitaOne SDF)은 보존한다. 원본 DB와 .env는 변경하지 않는다.

## Approach (Checklist)

- [x] **Step 0: Recon** (Inspect existing code, locate files)
  - Client FactionTutorialBattleDriver, PresentationFramePlayer, ObjectSyncer, PositionUpdater 및 Game TutorialBattle 확인.
- [x] **Step 1: Implementation** (Code changes, file paths)
  - game capture에 현재 구간의 실제 simulation tick, State에 tick·tickRate=20 추가. frame 번호는 기존대로 유지한다.
  - client DTO에 필드를 추가한다. 순수 TutorialPlaybackBuffer를 GameContracts assembly에 두어 시계 주입으로 검증한다.
  - 최초·revision 변경·명시적 복구·frame 누락·과도한 backlog/앱 정지에만 full snapshot을 적용한다.
  - 복구 시 queue·terminal 보류를 폐기하고 수신/재생 watermark 및 tick을 snapshot의 lastFrame/tick으로 함께 맞춘다. 복구 응답의 과거 프레임은 재생하지 않는다.
  - 정상 수신은 중복 frame을 제외해 queue에 넣는다. 수신 watermark로 다음 GET을 하고, 별도 재생 위치로 순서대로 dispatch한다.
  - 같은 simulation tick의 CAST capture는 같은 시점에 처리한다. 처음 움직임은 100ms buffer 뒤 50ms 간격으로 재생한다.
  - 중복은 tick이 아닌 frame으로 제거한다. 이미 표시한 tick에 늦게 도착한 새 CAST capture는 다음 Update에서 한 번 dispatch한다. 첫 CAST tick 0도 이 규칙을 쓴다.
  - 정상 Update에서 한 simulation tick 묶음만 적용한다. 긴 stall/500ms 초과 backlog는 최신 snapshot으로 복구하며 과거 공격을 몰아서 재생하지 않는다.
  - queue 고갈 시 시계를 멈추고 다음 움직임 batch를 재buffer한다. buffer 상한은 100 capture, tick lag 상한은 10 tick이다.
  - Poll은 응답 시간을 포함해 약 50ms 간격으로 직렬 실행한다. 재생 Update는 HTTP 대기와 분리한다.
  - 서버의 상태·카드·마나는 즉시 받되 CLEARED/FAILED/COMPLETED 안내는 queue와 마지막 이동 tween이 끝난 뒤 표시한다.
  - terminal은 driver에 revision·마지막 frame을 가진 대기 상태 하나로 보관한다. 입력은 즉시 막고 마지막 frame dispatch 후 1/tickRate가 지나면 한 번 표시한다. 더 받을 batch가 없어도 queue를 배출한다. revision 변경·복구·종료 시 기존 보류를 취소한다.
  - 상태 전환 시 카드 입력 취소, UUID 재시도, 소유권, revision 보호를 유지한다. 진입 시 GameConfig tick rate를 20으로 설정한다.
  - buffer는 frame/tick 순서·재생 시각·복구 필요 여부만 관리한다. HTTP·snapshot·UI는 driver가 맡는다. 간격과 GameConfig는 State.tickRate(현재 20) 한 곳에서 계산한다.
- [x] **Step 2: Tests** (Unit tests, manual verification steps)
  - EditMode: 150ms에 3 tick batch, 같은 tick CAST, 중복 응답, 순서/누락, revision, backlog, underrun, stalled clock, bounded queue 검증.
  - PlayMode: 실제 ServedObject가 batch 수신 시 즉시 끝 위치로 튀지 않고 연속된 Unity 프레임에 중간 위치를 통과하는지 검증. terminal 안내의 지연, frame gap 복구, 단계 교체도 확인.
  - game: 실제 fake clock tick과 CAST capture의 관계·20Hz metadata·구간 reset 확인 및 전체 회귀.
  - 로컬 실제 서버·Unity 신규 게스트 흐름에서 이동 수치와 프레임 dispatch 시각 기록. 가능하면 실제 재생 녹화도 확인.
- [x] **Step 3: Rollout / Rollback** (Feature flags, migration steps)
  - 기존 Game #187과 Client #398에 반영한다. game 계약을 먼저 적용하고 로컬 서버를 재시작한다. DB migration은 없다.

## Validation

- **Commands to run:** Unity EditMode/PlayMode, game `gradlew test`, 로컬 서버 API와 실제 Unity 재생 검증.
- **Expected output:** 중복 없는 tick 순서, 정상 렌더링 시 50ms dispatch 간격, HTTP batch 경계에서 snapshot 재갱신 없음, 두 세력 진행 유지.

## Risks & Rollback

- **Risks:** 약 100ms 표시 지연을 추가한다. 긴 연결 중단에서는 snapshot 복구로 위치가 한 번 보정될 수 있다.
- **Rollback steps:** 이 수정의 client/game commit만 revert한다. 계정·DB 상태는 변경하지 않는다.

## Open Questions

- 없음. 서버 시간 metadata를 포함하는 game/client를 함께 적용한다.

## Review

- FAST NONPASS: snapshot 기준점, 이미 표시한 tick의 CAST, terminal 보류 수명 주기를 구체화했다.
- MEDIUM PASS: buffer 책임을 제한하고 단일 terminal 대기 상태와 tickRate 기준을 반영했다.
- HEAVY PASS: 수정 계획의 복구 기준점·CAST·terminal 수명 주기와 실제 객체 재생 검증이 구현 가능한 것을 확인했다.
- 구현 리뷰에서 Time.time의 maximumDeltaTime 제한이 실제 앱 정지를 숨기는 것을 확인했다. 실제 시계로 별도 감지해 queue/terminal을 버리고 다음 snapshot으로 복구하도록 수정했으며 재검토 PASS를 받았다.

## Results

- game 전체 1,243개 통과, 실패·건너뜀 0개.
- client 전체 EditMode 728개 중 725개 통과, 실패 0개, 기존 무시 3개. 집중 버퍼·튜토리얼 검증 16개 통과.
- 최종 PlayMode 6개 통과: 기존 만화 3개와 단계 전환, 실제 객체 보간·공격 1회·terminal 지연, 실제 650ms 앱 정지 복구.
- 로컬 신규 게스트 → 만화 → 선택 → 슛 → 메테오 → 토네이도 → 공중 대응을 실제 서버/Unity에서 완료했다. 203개 tick 전이의 재생 간격 중앙값은 50.025ms였다.
- HTTP 요청은 그대로 사용한다. WebGL player 빌드·배포는 수행하지 않았다. Graphify 실행 파일과 그래프가 없어 갱신하지 못했다.
