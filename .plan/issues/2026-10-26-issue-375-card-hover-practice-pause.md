# 2026-10-26 — 카드 호버 설명과 연습경기 일시정지 추가

- Date: 2026-10-26
- GitHub Issue: #375 / ArcaneCastersGame#179
- Status: Implemented / PR 준비

## Goal
게임씬 손패 카드에 포인터를 올리면 이름, 마나, 설명과 능력치를 표시한다. Practice 경기에서만 호버 동안 서버 시뮬레이션과 제한시간을 멈춘다.

## Non-goals
PVP/PVE 일시정지, DB 변경, 버전 변경.

## Context / Constraints
기존 작업은 별도 worktree로 보존한다. CardUIZoom/ZoomedCardUI를 확장하고 MagicBookDetailText를 재사용한다. 설명창은 기존 UI 패널/텍스트를 복제한 프리팹으로 작성하고 raycast를 차단하지 않는다. 서버가 권한과 Practice 여부를 판정한다.

## Approach (Checklist)
- [x] **Step 0: Recon**: 기존 카드 hover, 프레임 전송, GameTimer, 게임 루프 확인.
- [x] **Step 1: Implementation**: 카드 상세 프리팹과 비동기 설명 갱신 버전 검사, 화면 경계 배치. 호버 소유 카드 검사로 이전 카드의 exit가 새 hover를 지우지 않게 한다. OnDisable/포커스 상실 시 해제한다.
- [x] **Step 1: Implementation**: 인증된 참가자의 `cardHover` 입력에 `hovering` bool을 추가한다. 서버는 Practice와 양의 userId인 실제 참가자만 허용한다. 클라이언트는 0.5초마다 갱신하고 서버는 2초 lease가 만료되면 자동 해제한다.
- [x] **Step 1: Implementation**: 루프는 입력 큐와 연결 유지 전송을 계속 처리한다. 정지 중 simulation frame 번호, 마나/카드 드로우/봇/물리/컴포넌트 갱신을 멈춘다. 제한시간은 pause 시작 시각을 보관하고 재개 시 endTime을 보정한다. pause 상태는 frame/sync에 bool로 전송한다. 클라이언트는 서버 확인 후 Time.timeScale을 보관/복원한다.
- [x] **Step 2: Tests**: 권한/모드 거부, lease 만료, 반복 pause, 서버 타이머 및 시스템 생략/전송 유지 테스트. 클라이언트 컴파일과 프리팹 참조 검증, 가능한 경우 실제 Editor 화면 검증.
- [ ] **Step 3: Rollout / Rollback**: 서버와 클라이언트 PR을 연결한다. 서버부터 적용하며 기존 클라이언트 입력은 그대로 처리한다.

## Validation
- **Commands to run:** 서버 `gradlew test`, 클라이언트 C# 컴파일, YAML 참조 검사, `git diff --check`, root `graphify update .`.
- **Expected output:** 서버 테스트 통과, 수정한 클라이언트 코드 컴파일 오류 없음, 깨진 YAML 참조 없음.

## Risks & Rollback
- **Risks:** 일시정지 중 프레임이 끊기면 클라이언트가 연결 종료로 판단한다. 따라서 시뮬레이션 정지 중에도 frame/sync를 계속 전송한다. 비동기 설명은 새 카드에 이전 텍스트를 쓰지 않도록 버전을 확인한다.
- **Rollback steps:** 두 기능 커밋을 revert한다.

## Open Questions
없음.

## Plan Review
- Fast: NONPASS → 갱신은 unscaled time 사용, 항복/경기 종료는 정지보다 우선, 프레임마다 새 DTO를 생성하여 이벤트를 중복 전송하지 않는다. 포커스 이탈/Hide는 즉시 timeScale을 복원하고 늦은 pause 확인은 현재 hover가 있을 때만 반영한다.
- Medium: NONPASS → FrameDataSystem DTO 생성/전송 경로를 재사용하고 마나/드로우만 정지 중 생략한다. BarController 이동은 unscaledDeltaTime을 사용한다. 전투의 unscaled tween 경로를 확인한다.
- 정지된 simulation frame 증가를 막기 위해 incrementFrameNum을 WordOnlineLoop의 입력 처리와 pause 판정 이후로 옮긴다. WordOnlineLoop만 실제 GameLoop 구현이며 PveLoop가 이를 상속한다.
- 검증에는 3초 이상 호버, 정지 중 항복, pause/resume 반복과 lease 만료, frame/sync bool 전달, 이벤트 중복 방지를 포함한다.
- Heavy 1: NONPASS → 정지 중 매 전송에 최신 snapshot을 만들고 sync로 보낸다. sync 간격 사이에 정지한 후 재접속해도 최신 상태를 받는지 검증한다. simulation frame 번호와 정지 중 전송 주기를 분리한다.
- Heavy 2: PASS.

## 구현 및 검증 결과
- 기존 CardUIZoom을 확장하고 GameCardDetails 프리팹을 GameScene에 연결했다. 설명 길이에 맞춰 높이를 조절하고 MagicBookDetailText의 설명/능력치를 사용한다.
- Practice만 서버 시뮬레이션과 GameTimer를 멈춘다. 정지 중 최신 sync와 ping을 유지하고 호버 lease 만료, 해제, 경기 종료에 재개한다.
- 서버 전체 1,222개 테스트 통과. Unity 2022.3.34f1 import/compile 및 Editor 검증 통과: 씬 프리팹 참조, 실제 ko-KR 설명, raycast 통과, 좌우 경계 배치, Hide, 시간 배율 복원, 늦은 확인 프레임 무시.
- docs/pr-media/375/card-hover-prefab.png는 Unity에서 실제 프리팹을 렌더한 검증용 화면이다. 서버에 연결한 실경기 화면은 아니다.
- root `graphify update .`는 CLI가 설치되어 있지 않아 실행하지 못했다. graphify-out/graph.json도 없다.
- 실제 서버와 Unity Play Mode를 연결한 경기 검증은 남아 있다.
