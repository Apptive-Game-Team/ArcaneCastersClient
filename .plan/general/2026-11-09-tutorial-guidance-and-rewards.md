# 2026-11-09 — 튜토리얼 손 안내·진영 체험 마법·보상 설명 수정

- Date: 2026-11-09
- GitHub Issue: Client #397 / PR #398
- Status: 검증 완료, 기존 PR 반영

## Goal

같은 단계에서 같은 카드를 성공적으로 사용한 뒤에는 손 안내를 반복하지 않는다.
HELL은 메테오, NATURE는 토네이도만 체험한다. 완료 문구를 요청한 내용으로 바꾸고
보상 카드에 마우스를 올리거나 탭하면 이름과 간단 설명을 표시한다.

## Non-goals

시작 보상 5종, 유닛 수치, 기존 로비 후속 안내는 유지한다. 전투 일시정지는 추가하지 않는다.

## Context / Constraints

기존 Client #398과 Rust 게시 브랜치에서 작업하며 다른 사용자 변경을 보존한다.
Unity 조작과 검증은 MCP로만 진행한다. 기존 GameCardDetails.prefab과 summary.* 현지화를 재사용한다.
서버 stage 문자열과 숫자 의미를 유지해 진행 중인 전투와 테스트를 깨지 않는다.

## Approach (Checklist)

- [x] **Step 0: Recon** 드라이버의 카드/필드/마나바 손 반복, 서버 NEXT, 완료 패널과 팝업 확인.
- [x] **Step 1: Implementation** 성공한 CAST 후 (stage, magic)을 기억해 해당 손 안내 전체를 숨긴다.
  선택 취소나 실패한 CAST는 소비하지 않고, formation의 탱커와 근접 카드는 각각 최초 안내한다.
  이 기억은 씬 내 재시도·폴링 복구에도 유지한다. 다음 단계의 같은 카드는 새로 안내한다.
  서버 NEXT는 shot 뒤 HELL=meteor/NATURE=tornado로 이동하고 HELL의 meteor 뒤는 anti_air로 이동한다.
  과거 단계에서 재접속한 전투도 다음 NEXT부터 새 순서를 따른다. 불필요한 주문 권한은 추가하지 않는다.
  두 진영 모두 6단계로 끝난다. 메테오 성공 문구의 '다음 지원 마법' 표현을 수정한다.
  완료 본문은 한국어로 '기본적인 전투를 배웠습니다. 축하합니다!\n아래 카드들을 획득했습니다!'로 표시한다.
  영어도 의미를 맞춘다. 기존 보상 배열과 아이콘은 유지한다.
  ZoomedCardUI에 CombinedMagicData와 RectTransform으로 표시하는 경로를 추가하고 기존 CardUI 경로를 공유한다.
  보상 프레임에 포인터 컴포넌트를 붙이고 패널에 GameCardDetails 프리팹을 연결한다.
  마우스 진입/이탈은 표시/숨김, 클릭·탭은 표시 유지, 다른 카드나 패널 종료는 교체/숨김 처리한다.
  비동기 설명은 팝업 교체·숨김 후 갱신하지 않는다. 레이아웃은 기존 생성기로 저장한다.
  프리팹만 저장하는 생성 경로를 분리해 선택/전투 씬의 무관한 재생성 변경을 피한다.
  숨김은 현재 anchor와 비교해 이전 카드의 늦은 exit가 새 팝업을 닫지 않게 한다.
- [x] **Step 2: Tests** 성공/실패 시 안내 소비, 반복 소환, 추천 카드 변경, 다음 단계·재시도 확인.
  서버 양 진영의 실제 전투 6단계 완료와 진영별 NEXT·권한·재시도를 확인한다.
  완료 카드의 두 로케일 설명, hover/tap, 패널 종료/다른 카드 교체와 늦은 비동기 응답을 확인한다.
  MCP로 import/compile, 지정 Unity EditMode/PlayMode, 보상 팝업 캡처를 수행한다.
  기존 전투 설명의 1초 지연과 legacy 확대 카드 위치도 회귀 검사한다.
- [x] **Step 3: Rollout / Rollback** 서버를 먼저 적용하고 클라이언트를 적용한다.
  기존 PR #398에 반영하고 로컬 Rust 실행 파일도 교체한다. 기능 커밋을 함께 revert하면 복구된다.

## Validation

- **Commands to run:** cargo fmt --check, cargo clippy --all-targets -- -D warnings, cargo test --workspace
  (기존 Windows 실패 2건은 기준선과 구분). Unity FactionTutorialTests/FactionTutorialPlayModeTests,
  CardHoverSummaryTests. git diff --check, graphify update .
- **Expected output:** 진영별 실제 전투 6단계 완료, 각 단계/카드 최초 사용만 안내,
  보상 카드 hover/tap에서 올바른 짧은 설명과 완료 문구 표시. 기존 로비 흐름 유지.

## Risks & Rollback

- **Risks:** stage 숫자를 재배열하면 기존 전투 의미가 바뀐다. 숫자는 유지하고 NEXT만 분기한다.
  완료 패널은 별도 Canvas이므로 팝업을 그 Canvas 아래 마지막 sibling으로 둔다.
- **Rollback steps:** 이 작업의 클라이언트와 서버 커밋을 함께 revert한다.

## Open Questions

없음. 실습 카드와 시작 보상은 기존 합의대로 분리한다.

## Plan Review

- FAST REVIEW: PASS. 일반 전투/legacy 확대 카드 회귀와 이전 카드 exit의 소유권 확인 추가.
- MEDIUM REVIEW: PASS. 패널만 생성하는 경로를 분리해 무관한 씬 재생성 방지 추가.
- HEAVY REVIEW: PASS. 구현 차단사항 없음.
- Rejected feedback: 없음.

## Verification Results

- Unity MCP: EditMode 20개, 전투 PlayMode 8개, 로비 후속 안내 PlayMode 3개 통과.
- 서버: fmt/clippy 통과, 기존 Windows 실패 2건을 제외한 workspace 1,411개 통과·3개 무시.
  제외한 두 건은 이전 작업에서 변경 전 기준선과 같은 실패를 확인한 테스트다.
- production 전투 fixture와 로컬 새 게스트 HTTP 검증에서 HELL/NATURE 각각 6단계 완료.
- 완료 문구와 보상 카드 hover/tap을 두 로케일·10종에서 검증하고 실제 프리팹 렌더 2장 확인.
- 로비 fixture에서 enabled=false만으로 OnEnable HTTP 요청이 남아 401을 받았다.
  코루틴과 WordOnline.* 어셈블리까지 격리한 뒤 3개 모두 통과했다.
- Unity 생성 prefab의 빈 필드 뒤 공백 외 diff check 통과. WebGL 빌드는 수행하지 않았다.
  Graphify CLI가 없어 update를 실행하지 못했다.
