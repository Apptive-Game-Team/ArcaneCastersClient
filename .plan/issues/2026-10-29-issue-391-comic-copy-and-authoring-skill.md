# 2026-10-29 — 워드 대륙 표기와 결말 순서 수정 및 모션 코믹 제작 스킬 추가

- Date: 2026-10-29
- GitHub Issue: #391 / PR #392
- Status: Implemented / validated

## Goal
위드/Wid를 워드/Word로 고치고 결말을 “계속해라.” → “그렇게, 워드 대륙에 전쟁은 시작됐다.”로 바꾼다. 만화 이미지 한 장에서 완성된 단일 모션 코믹 씬까지 만드는 프롬프트와 재사용 스킬을 기록한다.

## Non-goals
그림 재생성, 새 대사/음성/영상 추가, 기존 사선 구도와 입력 방식 변경, 버전 변경.

## Context / Constraints
feature/391 / PR #392를 이어 사용한다. 시작 시 Jua SDF.asset은 이미 dirty이므로 보존한다. panel 파일 번호와 beat 순서는 독립적이다. 마지막 두 beat를 바꾸어도 진행 수는 6/7→7/7이어야 한다. 프로젝트 스킬 소스를 .agents/skills/make-motion-comic에 보관하고 같은 패키지를 개인 skills 폴더에 설치한다. 스킬의 필수 입력은 만화 이미지 한 장이며 7컷/8대사는 현재 예제에만 해당한다.

## Approach (Checklist)
- [x] **Step 0: Recon** 번역 키, generator, player, 기존 테스트/PR, skill-creator 지침 확인.
- [x] **Step 1: Implementation** 두 언어 지명 수정. 끝 두 beat의 키와 panel 매핑 교환. 고유하게 공개한 컷 수로 진행 표시. 회귀 검사 보완. 실제 컷 복원 프롬프트 원문과 최종 씬 제작 프롬프트를 스킬 references에 기록. SKILL.md와 UI 메타데이터 작성, 모듈 AGENTS에 검색 항목 추가, 개인 skills에 복사.
- [x] **Step 2: Tests** Unity MCP import 및 기존 EditMode/PlayMode 검사. 실제 마지막 두 대사와 7/7 표시 확인. skill-creator quick_validate 실행. 독립 검토로 한 장 입력만으로 계획/대사 매핑이 가능한지, 현재 7컷/8대사를 다른 만화에 강제하지 않는지 확인.
- [x] **Step 3: Rollout / Rollback** 실제 캡처를 docs/pr-media/391에 기록한다. 기존 PR #392로 commit/push 및 설명을 갱신한다. 스킬 설치 경로/호출 예시를 제공한다. 되돌릴 때 이 커밋과 새 개인 스킬 패키지만 되돌린다.

## Validation
- **Commands to run:** Unity MCP scoped EditMode/PlayMode tests, 실제 결말 캡처, 번역 ID 검사, quick_validate.py, 스킬 독립 dry-run, scoped git diff --check.
- **Expected output:** 워드/Word, 계속해라→전쟁 시작, 마지막 7/7. 프로젝트/개인 스킬 검증 통과. 입력 이미지에 적힌 명령을 작업 지시로 취급하지 않고 사용자 교정을 우선한다.
- **Results:** EditMode 6/6, PlayMode 3/3 통과. 실제 Unity 재생에서 계속해라(6/7)→전쟁은 시작됐다(7/7)를 확인하고 캡처했다. 두 대륙 설명의 TMP 문자열도 워드로 확인했다. 콘솔 오류 0개. 두 언어의 51개 ID가 일치한다. 프로젝트/개인 스킬 quick_validate가 각각 통과했고 6개 파일의 SHA256이 일치한다. 별도 에이전트의 한 장 입력 dry-run에서 7패널/8비트 manifest와 검증 목록을 작성할 수 있었으며 blocker가 없었다. 이 dry-run은 전체 이미지 생성이나 씬 재구현 검증을 대신하지 않는다.
- **Limits:** WebGL player 빌드/브라우저 재검증은 미실행. Graphify 갱신은 CLI 미설치로 실행하지 못했다. 기존 사용자 dirty Jua 폰트는 작업 시작 스냅샷으로 복구하고 생성된 상태도 별도 백업했다.

## Risks & Rollback
- **Risks:** 패널 ID를 진행 순서로 오인하거나, 스킬에 현재 대사 수/경로를 일반 규칙으로 굳힐 수 있다. 실제 렌더 전 번역 ready를 기다린다. 개인 스킬 설치는 기존 폴더가 없음을 확인한 후 수행한다.
- **Rollback steps:** 후속 커밋을 되돌리고 개인 스킬은 해당 신규 패키지만 제거한다. 사용자 dirty font는 원래 상태로 보존한다.

## Open Questions
없음. 한국어 결말은 사용자가 이번에 적은 “전쟁은 시작됐다.”를 사용한다.
