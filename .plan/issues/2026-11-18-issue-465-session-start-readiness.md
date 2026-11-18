# 2026-11-18 — 양쪽 준비 확인 후 경기 시작 적용

- Date: 2026-11-18
- GitHub Issue: ArcaneCastersClient#465, ArcaneCastersGameRust#46, ArcaneCastersLobby#125
- Status: Implemented — draft PR 준비

## Goal

매칭 결과 전달 후 양쪽 인증 연결과 구독, 씬 초기화, 초기 snapshot 적용을 확인하고 서버가 경기를 한 번만 시작한다.

## Non-goals

Java 서버는 최신 dev 지침에 따라 보관용으로 유지한다. 준비 중 오류를 경기 패배로 처리하지 않는다. 실제 배포 로그 없이 현재 튕김의 원인을 단정하지 않는다.

## Context / Constraints

사용자 추가 결정에 따라 매칭 정보를 받으면 GameScene을 먼저 열고 준비 overlay 뒤에서 접속과 snapshot 적용을 수행한다. 서버 loop의 생존 여부와 전투 진행 여부는 다르다. GameTimer는 wall clock이며 ping timeout은 별도 scheduler에서 실행된다. 기존 SyncFrameHandler는 일부 예외를 삼킨다.

## Approach (Checklist)
- [x] **Step 0: Recon** 최신 dev 기반 별도 worktree, 이슈 생성, 일반 매칭·practice·PVE·친선전·복구 경로 확인.
- [x] **Step 1: Implementation** lobby HTTP capability header `X-Game-Start-Protocol: 1`을 각 요청의 인자로 전달한다. ticket/초대에 저장하고 양쪽 지원일 때 SessionDto/CreateSessionRequest의 startProtocolVersion=1을 전송한다. 없는 ticket과 old client는 0이며 기존 흐름을 유지한다. 새 서버 응답에서 version을 확인하고 MatchedInfo/SessionRecoveryInfo에 보존한다. local/debug/bot-only는 기존 흐름이다.
- [x] 서버 전용 StartGate 하나가 연결 ID, 구독 ID, epoch, snapshot token, 단계와 생성 후 180초 절대 deadline을 소유한다. STOMP는 자기 control 채널만 등록할 수 있고 ready는 현재 인증 연결과 정확한 frame 구독에 귀속한다. disconnect/unsubscribe와 마지막 ACK, 시작·timeout을 같은 잠금으로 직렬화한다. control 상태는 재구독/재요청에서 다시 보낸다.
- [x] server의 기존 StartedLoop wrapper가 gate를 확인하고 준비 중 gameplay action drain 없이 생존 tick만 수행한다. init 직후 timer pause, 초기 pending 오브젝트를 등록한 뒤 snapshot을 만들며 frame/mana/AI는 진행하지 않는다. SceneReady 이후 개인 snapshot을 control 응답에 포함하고 같은 token으로 재전송한다. 모든 현재 연결의 적용 ACK 뒤 3→2→1 카운트다운을 진행하고, 도중 disconnect가 없으면 running 전이, timer 재개와 ping 기준 갱신 후 내부 loop를 실행한다. 준비 중 gameplay input/ping takeover는 차단한다. PVE state는 시작 후 pveSync로 복구한다.
- [x] 준비가 취소되거나 시작 전에 loop가 실패하면 기존 세션 remove 소유권을 활용해 Abandoned/CLIENT_PREPARATION_TIMEOUT 또는 START_FAILED로 저장하고 사용자 상태와 로비를 정리한다. MMR/승수/PVE 완료/일반 승패 통계는 갱신하지 않는다.
- [x] client는 GameScene을 먼저 열어 양쪽 플레이어 정보를 보여주는 준비 overlay를 띄운다. persistent connection에서 control 구독과 readiness 재시도를 수행한다. 씬의 Start 완료 프레임 이후 frame 구독과 SceneReady를 보낸다. 초기 sync의 엄격한 적용 성공 시에만 token ACK를 보낸다. 준비 중에는 기존 30초 frame silence 판정을 적용하지 않고 제한된 재연결·전체 deadline으로 취소한다. running 재접속도 상태/snapshot을 복구한다.
- [x] **Step 2: Tests** gate 전이·경합·stale connection·인가·subscription·snapshot token·대기 시간 불변·취소 단일 정리·봇/PVE·legacy 혼합·복구 version 보존과 client lifecycle을 검증한다.
- [x] **Step 3: Rollout / Rollback** server→lobby→client 순서로 배포 가능한 opt-in 계약이다. PR은 draft로 작성하며 실제 배포는 별도다. protocol=0으로 생성한 세션은 기존 흐름을 유지한다.

## Validation
- **Commands to run:** cargo fmt --check, cargo clippy --all-targets -- -D warnings, cargo test; lobby Gradle test; client C# compilation 및 가능한 Unity 테스트.
- **Expected output:** 준비 중 전투 불변, 양쪽 ACK 전 시작 없음, 취소/시작 각각 한 번, 끊긴 연결의 준비 무효, 기존 계약 테스트 통과.

## Risks & Rollback
- **Risks:** Unity 실행·인증된 실제 2인 네트워크 검증 가능 여부는 환경에 따라 다르므로 실행하지 못한 검증은 명시한다. 새 header의 CORS 허용을 확인한다.
- **Rollback steps:** client capability 전송을 중단하면 새 매칭은 기존 흐름으로 복구된다. 이미 생성된 준비 세션은 version을 끝까지 유지한다.

## Open Questions
- 없음. first-pass의 timer/ping, action queue, 취소 통계, snapshot 적용 예외, PVE replay, 모든 생성 경로 지적을 반영했다.

사용자 추가 요구: 느린 네트워크를 위해 준비 전체180초, 개별 연결20초, 경기 중 복구30초를 적용한다. 생성 전 서버 capability를 확인해 구버전 서버에 준비 세션이 생성되지 않게 한다.

화면 구현은 별도 UI worktree에서 병렬 진행한다. overlay 뒤에서 실제 초기 snapshot을 적용하고 running 확인 때 시작! 표시 후 걷어낸다. 카운트다운 중 입력과 전투는 정지한다.

## 구현 및 검증 결과

- 대전은 실제 nullable matchMmr·이름·외형을 표시하고 모험은 별도 콘텐츠 그룹에 실제 제목과 스테이지를 표시한다. 모험 화면에는 VS나 상대 MMR을 표시하지 않는다.
- GameScene 진입 후 overlay 뒤에서 초기 상태를 적용하고 서버 3→2→1→시작 확인 뒤 화면을 걷는다. gameplay 입력은 overlay가 닫힐 때까지 차단한다.
- Unity 연결/준비 화면/입력 차단/모험 분기 테스트 50개 통과. ko/en 실제 Unity 프리팹 렌더 확인.
- 로비 전체 테스트 통과(느린 환경의 HTTP 테스트 초기화 timeout20s 설정). Rust Linux 전체 테스트, fmt, clippy 통과. 실제 외부 환경의 두 Unity 클라이언트 동시 플레이와 배포는 수행하지 않았다.
- 준비180초와 경기 중 재접속을 포함하도록 복구 정보 TTL을30분으로 연장한다. Graphify CLI가 없어 graphify update는 실행하지 못했다.
