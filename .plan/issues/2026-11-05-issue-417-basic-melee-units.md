# 2026-11-05 — 기본 근접 유닛 마나 통일과 전기 창병 추가

- Date: 2026-11-05
- GitHub Issue: #417 (DB #136, Rust #31)
- Status: Implementation

## Goal
비전 보병 기준의 돌골렘·전기 창병을 마나 35에 제공한다.

## Non-goals
시작 덱과 세력 보상 전체 개편, 나머지 기본 마법 교체, Java 서버 기능 추가.

## Context / Constraints
비보 hp 360, damage 70, interval 1, speed 1.4 기준이다. 돌골렘은 414/63/1÷0.95/1.19, 창병은 306/77/1÷1.10/1.82로 구성한다. 세 마나는 35다. 돌골렘 넉백과 잔해는 유지한다. 전기 창병은 Lightning 지상 단일 근접 유닛이며 기존 번개 종족 사망 효과를 따른다. 기존 작업은 별도 worktree로 보존한다.

## Approach (Checklist)
- [x] **Step 0: Recon**: DB 등록·Rust AttackMob·Unity 기존 프리팹 확인, 이슈 생성.
- [ ] **Step 1: Implementation**: DB 새 마이그레이션, Rust 등록, Unity 현지화·프리팹·돌골렘 축소. 신규 아트는 후보를 먼저 생성하여 승인 후 Assets에 연결한다.
- [ ] **Step 2: Tests**: MagicRegistry를 통한 소환, Lightning 지상 공격·공중 제외·공격 주기·마나 검증, DB 정적 및 scratch 적용/재적용, Unity GUID와 현지화 확인.
- [ ] **Step 3: Rollout / Rollback**: 세 draft PR 연결, 서버·클라이언트 준비 후 DB 적용. 배포는 이번 범위 아님.

## Validation
- **Commands to run:** cargo fmt --check, cargo clippy --all-targets -- -D warnings, cargo test, validate-migrations.sh origin/feature/134, SQL scratch 검증, Unity 가능 범위 검사.
- **Expected output:** Rust 전체 통과, 중복·누락 등록 없음, DB 두 번 적용 가능, Unity 참조 연결.

## Risks & Rollback
- **Risks:** 실전 승률은 미검증이며 속성·넉백·사망 효과 때문에 단순 DPS만으로 동등함을 보장하지 않는다. Unity Editor 실행 여부를 PR에 명시한다.
- **Rollback steps:** 미배포 PR은 revert, 적용한 마이그레이션은 수정하지 않고 forward-fix로 복구한다.

## Open Questions
- 신규 아트 후보 승인 대기. 기능 코드는 먼저 검증한다.

## Plan Review
FAST NONPASS의 소환→공격 경로 테스트와 아트 승인 단계를 반영했다. MEDIUM PASS, 수정안 HEAVY PASS.
