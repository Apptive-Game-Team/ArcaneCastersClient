# 2026-10-28 — 튜토리얼 앞 모션 코믹 도입부 추가

- Date: 2026-10-28
- GitHub Issue: #391
- Status: Ready for review

## Goal
사용자가 제공한 7개 PNG와 원본 대사로 그림 → 대사 → 다음 컷을 순차적으로 표시하고, 완료하거나 건너뛰면 기존 전투 튜토리얼을 시작한다. 모든 컷은 페이지를 넘기지 않고 한 화면에 누적된다.

## Non-goals
영상 출력, 신규 그림 생성, 음성 녹음, 서버 변경, 온보딩 진행 enum/저장 정책 변경, 버전 변경.

## Context / Constraints
클라이언트 dev에서 feature/391로 분기했다. Graphify 그래프는 없다. 기존 TutorialScene 시작 버튼의 실제 진입은 GlobalTutorialManager 호출이다. sceneName override는 제거된 SceneChangeButton을 가리키는 잔여 설정이므로 삭제한다. 전투 씬을 미리 실행하면 마나/적 타이머가 진행되므로 별도 ComicIntroScene에서 연출한다. 기존 버튼 프리팹, Lilita One/Jua 폰트와 Onboarding 번역 테이블을 사용한다. 사용자가 지정한 만화 화면은 도입부 고유 배경으로 취급한다.

## Approach (Checklist)
- [x] **Step 0: Recon** 기존 AdventureStoryOverlayUI, TutorialScene, GlobalTutorialManager 및 공통 UI 프리팹 확인.
- [x] **Step 1: Implementation** Assets/Art/Images/Tutorial/Intro에 원본 PNG/메타 추가. Assets/Prefabs/UI/Tutorial 아래 컷/도입부 프리팹 작성. ComicIntroPlayer 및 컷 뷰로 한 화면에 7개 컷을 그림 누적, 약한 줌/이동, 등장 페이드, 타이핑, 적 대사 강조 구현. 클릭/Space/Enter로 진행하고 타이핑 중 첫 입력은 문장만 완성한다. 연타 방지, 전환 중 입력 처리, 마지막 입력 유출 방지. UI에 건너뛰기와 진행 표시 제공. 원본 대사 8개와 UI 문구를 ko-KR/en Onboarding 테이블에 추가. 전투 컷은 첫 대사 후 같은 그림에서 수호자 대사를 표시한다. 초기화/번역 대기 전부터 스킵 입력을 연결하고 실패 시 중단 없이 기존 튜토리얼로 이동.
- [x] **Step 2: Tests** Unity import/compile 실행. 프리팹/scene 참조 및 번역 키 검증. EditMode에서 실제 프리팹의 컷 수·이미지·배치·클릭 전이·빠른 입력·완료/스킵 콜백의 단일 호출 검증(런타임 타입은 reflection 사용). 가능한 경우 Editor에서 캔버스를 렌더하고 정상/좁은 화면 확인.
- [x] **Step 3: Rollout / Rollback** GlobalTutorialManager와 TutorialScene의 기존 진입을 ComicIntroScene으로 연결. 새 씬을 build settings 맨 뒤에 등록해 기존 index 보존. 실제 재생 검증/PR 미디어·문서·PR 생성. parent pointer는 dev 병합 전 변경하지 않음.

## Validation
- **Commands to run:** Unity batch import/compile 및 범위 한정 EditMode 검사; git diff --check; YAML fileID/GUID 검사; 번역 ID 집합 비교; graphify update . (명령 가용 시 root에서).
- **Expected output:** 컴파일 오류 없음, 7개 이미지/8개 대사 순서 및 두 언어 키 일치, 모든 로컬 참조 해석, 완료·스킵 시 튜토리얼 단 한 번 진입.

## Risks & Rollback
- **Risks:** Unity 실행/라이선스가 제한되면 실제 플레이 검증을 할 수 없다. 모바일 화면의 작은 컷과 대사 가독성을 확인해야 한다. 마지막 입력이 다음 씬으로 전파되지 않도록 한 프레임 이상 대기한다.
- **Rollback steps:** 이 변경을 되돌리면 기존 InteractiveTutorialScene 진입으로 복귀한다.

## Open Questions
없음. 대사는 첨부 원본을 그대로 사용하고 영어도 함께 제공한다.


## Review and Results
- FAST REVIEW: 대사는 8개이며 전투 컷에서 두 단계를 사용하도록 수정. 활성 이중 전환이 아닌 제거된 컴포넌트의 잔여 override 정리로 수정. 초기화 전 스킵과 같은 그림의 두 번째 대사 진행 명시.
- MEDIUM REVIEW: PASS. 범용 명령 파서 없이 직렬화된 8개 beat와 컷 뷰만 사용.
- HEAVY REVIEW: PASS.
- Unity MCP의 실제 Editor import/compile 및 Play Mode 재생 확인. 최종 콘솔 오류 0개.
- EditMode 5/5 통과: 7개 그림/8개 대사, 한국어·영어 텍스트 넘침, 이전 대사 가림 방지, 버튼/빌드 씬 연결.
- PlayMode 2/2 통과: 타이핑 완성/연타, 컷·대사 누적, 전투 컷 두 대사, timeScale=0, 완료 및 초기화 전 스킵 후 단일 튜토리얼 진입. 테스트의 튜토리얼 씬은 HTTP 요청 전에 비활성화했다.
- 최종 단일 화면 배치의 Unity Game View 1920×1080 재생을 확인했다. 첫 컷과 7개 컷 완성 상태의 실제 캡처는 docs/pr-media/391에 저장.
- 신규 프리팹/씬의 로컬 fileID 참조 검사 통과, 번역 3개 파일 ID 집합 일치, 7개 원본 PNG의 SHA-256 일치.
- WebGL player 빌드와 브라우저 테스트는 실행하지 않음. graphify update .는 실행 파일이 없어 갱신하지 못함(기존 그래프도 없음).
- 테스트가 변경한 동적 폰트 파일은 전체 백업과 해시 확인 후 원상복구했고 기능 변경에는 포함하지 않음.

- 최종 수용 조건: 마지막 대사에서 7개 그림과 모든 이전 대사가 같은 페이지에 남아 있어야 한다. 작은 컷에는 16~22pt 자동 크기 조절을 적용한다.
- 최종 단일 화면 수정 후 EditMode 5/5, PlayMode 2/2를 다시 통과했고 7개 컷과 8개 대사가 같은 화면에 남는 것을 실제 캡처로 확인했다.
