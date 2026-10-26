# 2026-10-26 — 비전 보병 양손 마력 칼날 외형 적용

- Date: 2026-10-26
- GitHub Issue: #372
- Status: Draft

## Goal
사용자가 선택한 C안의 넓은 어깨와 양손 마력 칼날을 기본·공격 프레임에 적용한다.

## Non-goals
서버 전투 수치, 공격 범위, 애니메이션 코드, 버전, 다른 유닛 변경.

## Context / Constraints
기존 128x81 캔버스, PPU 70, Bottom Center 및 두 PNG의 GUID를 유지한다. 승인한 C안을 대상 디자인으로, master-v2를 재질 기준으로 사용한다. 몸 높이와 발 접점을 기존 이미지와 비교한다.

## Approach (Checklist)
- [x] **Step 0: Recon** 프리팹, 메타데이터, 기존 테스트와 아트 문서 확인.
- [ ] **Step 1: Implementation** 투명한 기본·공격 쌍을 생성하고 공통 크롭·배율로 정규화한다. 공격 프레임은 앞팔만 바꾸고 머리·몸통·발을 고정한다. 선택 기록, WORLD, 개별 마법 문서와 제작 상태를 갱신한다.
- [ ] **Step 2: Tests** 투명도, 몸 크기, 바닥 간격, 피벗, GUID 연결 및 64px 두 배경 비교. 실행 가능하면 기존 InfantryCarrierPrefabTests를 실행한다.
- [ ] **Step 3: Rollout / Rollback** feature/372와 PR #374를 갱신한다. 배포·병합은 포함하지 않는다.

## Validation
- **Commands to run:** 이미지 채널·바운딩 박스 검사, 변경 경로 git diff --check, Unity EditMode InfantryCarrierPrefabTests.
- **Expected output:** 두 프레임의 캔버스·PPU·발 위치 일치, 투명 배경, 승인한 디자인 유지. Unity 미실행이면 명시한다.

## Risks & Rollback
- **Risks:** 생성 시 프레임 간 몸 배율 변화, 배경 잔상, 공격 칼날에 따른 몸 축소.
- **Rollback steps:** 이번 변경의 두 PNG와 아트 문서만 이전 커밋으로 복원한다.

## Open Questions
없음. C안 선택으로 외형 적용이 승인되었다.

## Review
Fast 지적을 반영해 개별 마법 문서의 낡은 마도봉 설명도 갱신한다. 기존 alpha>128 범위는 기본 (41,0)-(103,81), 공격 (38,3)-(128,81)이다. 두 프레임의 바닥 간격은 0px이다. 높이 81px와 발 기준을 유지하며, C안의 넓은 어깨로 인한 기본 자세 가로폭 증가는 의도한 변경이다. 머리·몸통 크기는 비교 시트로 별도 확인한다. 공격 팔 때문에 몸이 축소되면 팔 확장 범위를 줄여 재생성한다.
Medium은 PASS. 분화구 운반수까지 덮어쓰는 기존 finalizer는 실행하지 않고 이번 두 프레임만 처리한다. 비교 이미지에는 정렬과 축소 결과를 남긴다.
