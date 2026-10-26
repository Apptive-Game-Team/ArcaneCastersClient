# 2026-10-26 — 비전 보병 양손 마력 칼날 외형 적용

- Date: 2026-10-26
- GitHub Issue: #372
- Status: Complete

## Goal
사용자가 선택한 C안의 넓은 어깨와 양손 마력 칼날을 기본·공격·걷기 2프레임에 적용한다. 이동 중 걷기와 공격 우선 전환을 비전 보병 전용 컴포넌트로 연결한다.

## Non-goals
서버 전투 수치, 공격 범위, 버전, 다른 유닛 변경.

## Context / Constraints
기존 128x81 캔버스, PPU 70, Bottom Center 및 두 PNG의 GUID를 유지한다. 승인한 C안을 대상 디자인으로, master-v2를 재질 기준으로 사용한다. 몸 높이와 발 접점을 기존 이미지와 비교한다.

## Approach (Checklist)
- [x] **Step 0: Recon** 프리팹, 메타데이터, 기존 테스트와 아트 문서 확인.
- [x] **Step 1: Implementation** 기본·공격 및 걷기 두 장을 생성하고 배율과 지지하는 발 기준으로 정규화한다. InfantryPoseController와 variant를 연결하고 선택 기록, WORLD, 개별 마법 문서와 제작 상태를 갱신한다.
- [x] **Step 2: Tests** 투명도, 바닥 간격, 피벗, GUID 연결 및 64px 두 배경 비교 완료. Unity EditMode 11개, PlayMode 1개 통과.
- [x] **Step 3: Rollout / Rollback** feature/372와 PR #374에 적용한다. 배포·병합은 포함하지 않는다.

## Validation
- **Commands to run:** 이미지 채널·바운딩 박스 검사, 변경 경로 git diff --check, Unity EditMode InfantryCarrierPrefabTests.
- **Expected output:** 네 프레임의 캔버스·PPU·바닥 간격 일치, 투명 배경, 승인한 디자인 유지. 연속 이동 이벤트가 걷기 위상을 초기화하지 않으며 공격이 우선한다.

## Risks & Rollback
- **Risks:** 생성 시 프레임 간 몸 배율 변화, 배경 잔상, 공격 칼날에 따른 몸 축소.
- **Rollback steps:** 이번 변경의 두 PNG, variant, 테스트와 아트 문서를 이전 커밋으로 복원하고 새 걷기 PNG·메타와 전용 controller를 제거한다.

## Open Questions
없음. C안 선택으로 외형 적용이 승인되었다.

## Scope Update
사용자가 걷기 2프레임을 요청했다. 전용 InfantryPoseController 하나가 기본·걷기·공격 sprite를 소유한다. OnMoved 이벤트가 이어지는 동안 0.16초 간격으로 두 발걸음을 번갈아 표시하고, 이동 갱신이 max(0.08초, 2개 서버 프레임) 이상 끊기면 기본 자세로 복귀한다. 후속 이동 이벤트는 종료 시각만 연장한다. OnAttack이 발생하면 0.16초 공격 자세를 우선 표시한다. 최초 바인딩·비활성화 시 타이머와 자세를 초기화하고, 비활성 이벤트는 무시하며 파괴 시 이벤트를 해제한다. ArcaneInfantry에서만 기존 hopping과 attack swap 컴포넌트를 제거해 충돌을 막는다. 걷기는 양발을 번갈아 들고 머리·몸통의 배율·중심은 기본 프레임에 맞춘다. 프리팹 import 검사와 이동→걷기 교대→공격 우선→정지·비활성화 복귀 테스트를 추가한다.

## Review
Fast 지적을 반영해 개별 마법 문서의 낡은 마도봉 설명도 갱신한다. 기존 alpha>128 범위는 기본 (41,0)-(103,81), 공격 (38,3)-(128,81)이다. 두 프레임의 바닥 간격은 0px이다. 높이 81px와 발 기준을 유지하며, C안의 넓은 어깨로 인한 기본 자세 가로폭 증가는 의도한 변경이다. 머리·몸통 크기는 비교 시트로 별도 확인한다. 공격 팔 때문에 몸이 축소되면 팔 확장 범위를 줄여 재생성한다.
Medium 및 Heavy는 보완 후 PASS. 분화구 운반수까지 덮어쓰는 기존 finalizer는 실행하지 않고 비전 보병 네 프레임만 처리한다. 비교 이미지에는 정렬과 축소 결과를 남긴다.

## Results
Unity 2022.3.34f1에서 InfantryCarrierPrefabTests 11개와 InfantryPosePlayModeTests 1개가 통과했다. 비활성화 콜백은 EditMode가 아닌 PlayMode로 검증한다. 기본·공격·걷기 모두 높이 81px와 0px 바닥 간격이다. `docs/pr-media/372/infantry-frames.png`와 `infantry-walk.gif`는 런타임 PNG로 만든 비교 자료이며 실제 게임 녹화는 아니다. Graphify 갱신은 실행 파일이 설치되지 않아 수행하지 못했다.
