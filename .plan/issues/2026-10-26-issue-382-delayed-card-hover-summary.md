# 2026-10-26 — 게임씬 카드 1초 호버와 마법 간단 설명 추가

- Date: 2026-10-26
- GitHub Issue: #382
- Status: Implemented

## Goal

게임씬에서 같은 손패 카드에 포인터를 1초 유지하면 짧은 마법 설명을 표시한다.
설명에서 parameter 및 능력치 수치를 제외하고 모든 마법의 핵심 행동을 한국어와 영어로 제공한다.

## Non-goals

도감·덱의 상세 설명과 서버의 마법 동작·정지 프로토콜은 변경하지 않는다.
튜토리얼의 기존 확대 카드는 즉시 표시한다. 버전은 올리지 않는다.

## Context / Constraints

`CardUIZoom`은 손패 호버, 서버 정지 요청과 시간 배율 복원을 소유한다.
`GameScene`은 `detailPrefab`을 지정하며 튜토리얼은 기존 확대 카드를 사용한다.
호버 대기 중에는 연습경기를 정지하지 않고 설명이 열릴 때부터 서버 확인 흐름을 적용한다.
기존 `MagicBook` 테이블의 `summary.<snake_case>` 키로 간단 설명을 추가해 Addressables 등록을 재사용한다.
유효한 `Magic` 이름 전체와 현재 DB의 마법 목록을 대조한다. 이전 마법도 설명을 제공한다.
조회 키는 `summary.` + `StringUtils.ToSnakeCase(magic.serverName)`으로 고정한다.
서버 `magic.text`는 별칭일 수 있으므로 간단 설명의 키로 쓰지 않는다. 누락 시 빈 본문을 사용하고 raw key나 상세 수치를 표시하지 않는다.
간단 설명은 핵심 행동 1문장과 필요한 제한 1문장 이내로 작성한다. 한국어 80자, 영어 180자 이내를 목표로 하며 아군 피해와 대공 전용 같은 제한을 보존한다.
원래 작업 폴더의 폰트 수정사항은 별도 작업 폴더를 사용해 보존한다.

## Approach (Checklist)

- [x] **Step 0: Recon** — 기존 호버·정지·프리팹·번역 구조와 마법 행동을 확인한다.
- [x] **Step 1: Implementation** — `CardUIZoom`에 실제 시간 1초 대기를 추가한다. 대기 카드는 `hoveredCard`, 열린 설명은 `hasHover`로 구분한다. 포인터 이탈·카드 변경·삭제·포커스 상실·경기 종료에 대기를 취소하고 이전 비동기 응답을 무효화한다. 같은 카드 재진입 호출은 대기를 재시작하지 않는다. `ZoomedCardUI`의 게임씬 설명만 `MagicBook`의 간단 설명으로 바꾸고 본문 높이 제한과 `GameCardDetails.prefab` 초기 높이를 함께 줄인다. 기존 `MagicBookDetailText`는 유지한다. 전체 마법의 양쪽 언어 설명과 `.agents/docs/card-hover-pause.md` 지침을 갱신한다.
- [x] **Step 2: Tests** — 1초 경계, 빠른 카드 전환, 취소와 정지 복원, 번역 키·ID·양쪽 언어 누락 및 수치 부재를 검증한다. 기존 Unity 참조로 C#를 컴파일하고 가능한 경우 실제 Editor에서 시간 동작·패널을 검증한다. PR용 검증 이미지를 제공한다.
- [x] **Step 3: Rollout / Rollback** — `feature/382`에서 커밋·푸시하고 `dev` 대상 PR을 연다. 이슈·PR의 담당자와 feature 라벨을 확인한다.

## Validation

- **Commands to run:** 전체 마법과 `summary.*` 키 대조, 중복 ID 및 locale 대응 검사, 변경 코드 C# 컴파일, `git diff --check`, Unity 검증이 가능하면 import 및 동작 검사, 루트에서 `graphify update .`.
- **Expected output:** 1초 전에는 패널과 정지 요청이 없고 1초 이후 같은 카드의 짧은 설명이 표시된다. 이탈·전환은 대기를 초기화한다. 모든 마법의 설명은 양쪽 언어에서 비어 있지 않고 상세 수치·파라미터가 없다.
- **Timing procedure:** 실제 `CardUIZoom`의 대기 상태를 구성한 Editor 검증에서 `hoverReadyTime`을 현재 실제 시간보다 0.01초 뒤로 설정하면 미표시, 이미 지난 시간으로 설정하면 표시되는지 검사한다. 카드 전환 시 새 deadline, 이탈·비활성화·포커스 상실 시 대기 취소, `Time.timeScale = 0`에서도 deadline 진행, 대기 중 `ApplyServerPause(true)` 무시를 검사한다. 가능하면 `Show`를 통한 진입도 검증하고 batch mode의 포커스 제한으로 실행할 수 없는 검사는 명시한다. 실제 설명 프리팹을 렌더해 한국어·영어 긴 요약의 잘림을 확인한다.

## Plan Review

- FAST REVIEW: NONPASS — 키 기준과 시간 검증 방법을 명시하라는 의견을 반영했다. 짧은 문구 작성 기준과 관련 지침 갱신도 추가했다.
- MEDIUM REVIEW: PASS — 대기와 표시 상태를 분리하고 기존 상세 텍스트를 보존한다. 프리팹과 런타임 높이를 함께 축소한다.
- Rejected feedback: 없음.
- HEAVY REVIEW: PASS — 키·커버리지·대기·정지 시점과 실행 검증 계획을 확인했다.

## Verification Results

- 한국어·영어 간단 설명 108개씩을 추가했다. 현재 DB 81종과 기존 이름 테이블의 마법·PVE 전용 둥지를 모두 포함한다. 기존 상세 설명은 변경하지 않았다.
- Unity 2022.3.34f1에서 프로젝트를 임포트·컴파일했다. `CardHoverSummaryTests`는 8개 통과, 0개 실패, 3개 생략이다. 생략 항목은 batch Editor에 포커스가 없어 실행할 수 없는 Show 진입·튜토리얼 즉시 표시·정지 확인이다.
- 실제 번역 API에서 canonical serverName 조회와 누락 시 빈 본문을 확인했다. 타이머 경계·이탈·비활성화·삭제·포커스 상실에 대한 취소와 대기 중 정지 확인 무시를 검사했다.
- 실제 설명 프리팹을 한국어·영어로 렌더했다. 전체 216개 번역의 최대 본문 필요 높이는 85.89로 제한 168 이내였다. 프리팹 검증 이미지이며 실경기 캡처는 아니다.
- 번역 ID·키 중복과 누락, 수치·placeholder 부재, 기존 번역 보존, 프리팹 참조와 raycast 통과, `git diff --check`를 확인했다.
- Graphify는 CLI가 없어 `graphify update .`를 실행하지 못했다. 실서버 경기의 수동 검증은 남아 있다.

## Risks & Rollback

- **Risks:** 일시정지 중에도 대기가 진행되어야 하므로 실제 시간을 사용한다. 설명 비동기 응답은 기존 renderVersion으로 취소한다. 소환물·과거 마법의 설명은 이름에서 행동을 추측하지 않고 서버 정의를 확인한다. Unity가 없으면 실제 UI 검증 한계를 PR에 기재한다.
- **Rollback steps:** 후속 PR의 커밋을 되돌린다. 데이터·서버 마이그레이션은 없다.

## Open Questions

- 없음. 마나 비용은 카드의 기존 비용 표시로 유지하고, 설명 본문에서는 모든 능력치·parameter 값을 제거한다.
