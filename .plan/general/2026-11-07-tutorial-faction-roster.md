# 2026-11-07 — 튜토리얼 진영별 실습 카드 교체

- Date: 2026-11-07
- GitHub Issue: Client #397
- Status: 기존 PR #398 반영

## Goal

튜토리얼 전용 카드를 역할 순서 `[슛, 근접, 원거리, 탱커, 공중]`로 교체한다.
HELL은 fire_shot, rock_golem, rock_mage, wall_golem, stone_gargoyle이다.
NATURE는 chain_lightning, electric_spearman, aqua_archer, tree_golem,
thunder_spirit이다. 두 진영이 모든 단계를 실제 전투로 완료해야 한다.

## Non-goals

사용자가 시작 보상 변경을 제외했다. 로비·DB 지급표와 보상 아이콘은 유지한다.
유닛의 전역 성능과 세력 선택 화면의 장식용 군대는 변경하지 않는다.

## Context / Constraints

Rust는 최신 origin/dev에서 분리한 worktree에서 수정한다. 클라이언트는 기존
튜토리얼 브랜치 위 로컬 브랜치에서 수정한다. 다른 작업의 파일은 보존한다.
기존 클라이언트 PR #398에 반영한다. Rust의 기존 튜토리얼 PR은 병합되어 있으므로
서버 커밋을 별도 브랜치로 게시하고 비교 링크를 #398의 적용 의존성에 연결한다.

## Approach (Checklist)

- [x] **Step 0: Recon** 현재 카드 배열의 인덱스와 보상 UI 공유, 실제 공격 마스크 확인.
- [x] **Step 1: Implementation** 서버 카드 배열을 교체하고 탱커 감지를 WallGolem으로
  맞춘다. 대공은 원거리 유닛, 조합은 탱커와 근접 유닛, 기존 explosion 식별자의
  여섯째 단계는 공중 유닛 실습으로 바꾼다. 기존 stage 문자열은 유지한다.
  클라이언트는 실습 배열과 보상 배열을 분리하고 필요한 카드 검증과 안내를 맞춘다.
  보상 아이콘은 기존 보상 배열을 쓰고 formation 판별은 새 실습 배열을 쓴다.
  공중 소환 단계의 손과 전투 테스트는 사거리 안의 (6,0,5)를 사용한다.
  기존 explosion stage 값이 공중 실습의 호환 식별자라는 설명을 코드에 남긴다.
- [x] **Step 2: Tests** 누락된 신규 유닛 매개변수를 기존 등록·테스트 자료에서 가져와
  fixture에 추가한다. 실제 전투로 두 진영 전 단계 완료, 마나·시간, 배열과 보상
  분리, 한국어·영어 문구와 PlayMode 표시를 확인한다.
- [x] **Step 3: Rollout / Rollback** 기존 PR에 클라이언트 커밋과 Rust 비교 링크를
  반영한다. 서버와 클라이언트는 함께 적용하고 필요하면 둘 다 revert한다.

## Validation

- **Commands to run:** Rust cargo fmt --check, cargo clippy --all-targets -- -D warnings,
  cargo test --workspace. Unity FactionTutorialTests, FactionTutorialPlayModeTests.
  git diff --check, 루트 graphify update .
- **Expected output:** 두 진영 7단계 완료. 실습 마법과 보상 마법이 섞이지 않는다.

## Risks & Rollback

- **Risks:** 기존 무리·폭발용 적 수가 새 단일 유닛에 과할 수 있다. 실제 전투 결과에
  따라 튜토리얼의 적 수와 배치만 조정한다. 전역 유닛 수치는 바꾸지 않는다.
- **Rollback steps:** 서버와 클라이언트의 이 작업 커밋을 함께 revert한다.

## Open Questions

없음. 사용자가 튜토리얼에서 쓰는 카드만 변경하도록 답했다.

## Plan Review

- FAST REVIEW: NONPASS → 공중 소환 안내·테스트 좌표를 (6,0,5)로 명시해 사거리 초과 방지.
- MEDIUM REVIEW: PASS → 보상/실습 호출처 분리와 stage 호환 설명 반영.
- HEAVY REVIEW: PASS. 구현 검토에서도 실제 결함은 발견되지 않음.

## Verification Results

- Unity 2022.3.34f1 EditMode 9개, PlayMode 9개 통과. 실습/보상 배열 분리,
  실제 카드 prefab, 한국어·영어 문구, 두 진영 소환 지점과 탱커/근접 안내,
  토네이도 손 고정과 기존 후속 로비 안내를 확인했다.
- Rust 튜토리얼 테스트 6개에서 양 진영 7단계 완료. 최종 fixture는 신규 유닛과
  돌 골렘의 Database V122·V123 수치를 사용한다.
- cargo fmt --check와 workspace clippy --all-targets -- -D warnings 통과.
- 전체 cargo test --workspace는 Windows의 기존 테스트 2개가 실패했다.
  matches_java_instant_to_string은 1ns가 0ns로 줄고,
  a_closed_port_is_an_io_failure는 ConnectException 대신 Timeout을 받는다.
  두 건 모두 변경 전 a208c27에서도 각각 같은 실패를 재현했다.
  이 두 테스트를 제외한 전체 회귀 검사에서는 1,410개 통과, 기존 무시 3개,
  실패 0개를 확인했다.
- 실제 서버 계정 E2E와 WebGL 빌드는 실행하지 않았다.
  graphify update .는 CLI가 없어 실행하지 못했다.
