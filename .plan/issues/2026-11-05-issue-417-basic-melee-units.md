# 2026-11-05 — 기본 근접 유닛 마나 통일과 전기 창병 추가

- Date: 2026-11-05
- GitHub Issue: #417 (DB #136, Rust #31)
- Status: Draft PRs created (DB #137, Rust #35, client #418)

## Goal
비전 보병 기준의 돌골렘·전기 창병을 마나 35에 제공한다.

## Non-goals
시작 덱과 세력 보상 전체 개편, 나머지 기본 마법 교체, Java 서버 기능 추가.

## Context / Constraints
비보 hp 360, damage 70, interval 1, speed 1.4 기준이다. 돌골렘은 414/63/1÷0.95/1.19, 창병은 306/77/1÷1.10/1.82로 구성한다. 세 마나는 35다. 돌골렘 넉백과 잔해는 유지한다. 전기 창병은 Lightning 지상 단일 근접 유닛이며 기존 번개 종족 사망 효과를 따른다. 기존 작업은 별도 worktree로 보존한다.

## Approach (Checklist)
- [x] **Step 0: Recon**: DB 등록·Rust AttackMob·Unity 기존 프리팹 확인, 이슈 생성.
- [x] **Step 1: Implementation**: DB V123, Rust 등록, Unity 현지화·프리팹·돌골렘 축소. 신규 아트는 사용자 승인 후 Assets에 연결했다.
- [x] **Step 2: Tests**: MagicRegistry를 통한 소환, Lightning 지상 공격·공중 제외·공격 주기, DB 정적 및 전체 scratch 적용/재적용, Unity GUID와 현지화 확인. Unity 2022.3.34f1 EditMode 8개 통과.
- [x] **Step 3: Rollout / Rollback**: DB #137, Rust #35, client #418의 draft PR을 생성했다. 서버·클라이언트 준비 후 DB를 적용한다. 배포는 수행하지 않았다.

## Validation
- **Commands to run:** cargo fmt --check, cargo clippy --all-targets -- -D warnings, cargo test, validate-migrations.sh origin/feature/134, SQL scratch 검증, Unity 가능 범위 검사.
- **Expected output:** Rust 전체 통과, 중복·누락 등록 없음, DB 두 번 적용 가능, Unity 참조 연결.

## Risks & Rollback
- **Risks:** 실전 승률은 미검증이며 속성·넉백·사망 효과 때문에 단순 DPS만으로 동등함을 보장하지 않는다. Unity Editor 실행 여부를 PR에 명시한다.
- **Rollback steps:** 미배포 PR은 revert, 적용한 마이그레이션은 수정하지 않고 forward-fix로 복구한다.

## Open Questions
- 없음. 실전 승률과 WebGL 플레이 검증은 아직 수행하지 않았다.

## Plan Review
FAST NONPASS의 소환→공격 경로 테스트와 아트 승인 단계를 반영했다. MEDIUM PASS, 수정안 HEAVY PASS.
