# 2026-11-05 — 돌 골렘 부족 몸체 약 70%의 잠식 적용

- Date: 2026-11-05
- GitHub Issue: #423
- Status: 구현·정적 검증 완료, PR #424 검토 대기

## Goal
몸체 약 70%의 머리·몸통·복부·팔과 다리에 검붉은 표면과 붉은 균열을 적용한다.
나머지 약 30%는 황갈색 돌과 초록 이끼를 남긴다. 비율은 시각적 목표다.

## Non-goals
능력치·행동·이름·웹사이트·코믹스 수정. 인간 병기 Towerback과 PveBrickGolem 제외.
전신 변색과 모든 이끼를 시들게 한 v2는 사용하지 않는다.

## Context / Constraints
사용자 피드백: 눈만 바꾼 v1은 약하고, 전신 변색 v2는 과하므로 핀처럼 부분적으로 적용한다.
최신 요청은 한쪽 팔만 바꾼 v3에서 몸체 약 70%로 범위를 넓히는 것이다.
핀의 2단계처럼 원래 재질이 남은 부분 잠식 개념은 유지한다.
게임의 master-v2 렌더링, 기존 캔버스·alpha bbox·바닥 여백·GUID·PPU·피벗을 유지한다.
생성물로 표면을 갱신했으므로 v1의 눈 밖 픽셀·전체 alpha 동일 검증은 현재 버전에 적용하지 않는다.

## Approach (Checklist)
- [x] **Step 0: Recon** MiniRock prefab의 MiniRockSwarm 연결, RockGolem 기본·공격, RockMage, WallGolem 총 5장 확인.
- [x] **Step 1: Implementation** built-in image_gen으로 다섯 이미지의 약 70% 잠식 생성. 선택 입력과 프롬프트 보존.
- [x] **Step 2: Tests** 캔버스·bbox·바닥 여백·metadata·prefab 참조, 붉은 눈·동일 팔 기본/공격과 64px 비교 확인.
- [ ] **Step 3: Rollout / Rollback** PR #424 검토 뒤 dev 병합은 별도. 원본 PNG 5장 복원으로 되돌린다.

## Validation
- **Commands to run:** v4/finalize.py, 이미지·prefab 참조 확인, git diff --check.
- **Expected output:** 5장 모두 기존 캔버스·bbox·바닥 여백 유지, metadata·prefab 변경 없음.

## Risks & Rollback
- **Risks:** 실제 Unity 재생은 미확인. 기존 alpha 윤곽 전체와 픽셀은 생성물로 달라진다.
- **Rollback steps:** feat(art) 변경 revert 또는 원본 PNG 5장 복원.

## Open Questions
- 없음. 최신 사용자 피드백을 적용했다.

## Verification Results
5장 모두 기존 캔버스·alpha bbox·바닥 여백을 유지했다. 입력 원본과의 종횡비 차이는 3.0% 이내다.
meta와 prefab 변경이 없으며 64px 및 원본 크기 비교를 확인했다. 전체 스프라이트 시트는
ImageMagick 미설치로 Pillow를 사용했다. Graphify 실행 파일은 설치되어 있지 않다.
Unity Editor 실제 재생·빌드·PR 병합·배포는 수행하지 않았다.
