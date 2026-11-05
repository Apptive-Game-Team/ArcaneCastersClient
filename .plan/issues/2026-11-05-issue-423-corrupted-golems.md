# 2026-11-05 — 돌 골렘 부족의 타락 외형 적용

- Date: 2026-11-05
- GitHub Issue: #423
- Status: 구현 및 정적 검증 완료, Unity 실기 확인 대기

## Goal
게임의 돌 골렘 부족 유닛 전체에 코믹스의 붉은 눈 적용.

## Non-goals
능력치·행동·이름·기존 프레임 포즈·웹사이트·코믹스 수정. 인간 병기 Towerback과 PveBrickGolem 제외.

## Context / Constraints
코믹스 2화 art-v4/page-03-diverse.png, 6화 art-revised-v3/page-12.png에서 돌·이끼·몸체는 유지되고 눈만 붉게 빛난다. 게임의 master-v2 렌더링과 원본 alpha, 크기, 접지, GUID를 보존한다.

## Approach (Checklist)
- [x] **Step 0: Recon** MiniRock prefab과 MiniRockSwarm 스프라이트 연결 확인. RockGolem 기본·공격, RockMage, WallGolem 포함, 총 5장.
- [x] **Step 1: Implementation** 원본별 built-in image_gen 편집. 생성된 붉은 눈만 추출해 원본 눈 영역에 맞춰 합성하고 몸체 픽셀 보존.
- [x] **Step 2: Tests** 원본 크기·alpha 일치, 눈 외 픽셀 동일, prefab GUID 불변, 기본·공격 프레임 연결, 64px와 원본 전후 비교 확인.
- [ ] **Step 3: Rollout / Rollback** 독립 feature/423 브랜치와 dev 대상 PR. 배포는 별도. 이미지 5장 복원으로 되돌린다.

## Validation
- **Commands to run:** 이미지 검증 스크립트, git diff --check, 스프라이트 시트 생성.
- **Expected output:** 5장 모두 alpha·크기·눈 외 픽셀 보존, 붉은 눈 확인, 기존 meta 불변.

## Risks & Rollback
- **Risks:** 생성기의 몸체 재해석은 눈 영역만 사용해 차단한다. 64px에서 붉은 눈 가독성을 확인한다. Unity 실기 재생 검증 여부를 PR에 명시한다.
- **Rollback steps:** 해당 커밋 revert 또는 이미지 5장 복원.

## Open Questions
- 없음. 사용자가 게임 안 부족 유닛 전체를 대상으로 확정했다.

## Plan Review
FAST REVIEW의 프레임 누락·눈 마스크 범위 지적을 반영했다. 상위 prefab의 빈 swapSprite와 RockGolem 공격 override를 확인했다. MEDIUM REVIEW와 수정 계획의 HEAVY REVIEW는 PASS다.

## Verification Results
다섯 이미지의 alpha·캔버스·접지·눈 밖 RGBA 동일 검증을 통과했다. 변경 픽셀 수는 462/110/110/342/96이다. 64px와 전후 비교를 확인했다. ImageMagick은 설치되어 있지 않아 make-sheets.sh 대신 Pillow로 같은 전체 스프라이트 검토 시트를 생성했다. Unity Editor 실제 재생은 수행하지 않았다.
