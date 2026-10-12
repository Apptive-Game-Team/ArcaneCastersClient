# 2026-10-09 — 서버 리플레이를 받는 마법 미리보기

- Date: 2026-10-09
- GitHub Issue: [#266](https://github.com/Apptive-Game-Team/ArcaneCastersClient/issues/266)
- Status: Implemented; PR preparation

## Goal
첫 로비 진입에서 서버 카탈로그를 확인하고 최신 JSON을 기존 재생기로 재생한다.

## Non-goals
재생기/표시 규약 교체, 마법 아트 변경, PlayerPrefs에 JSON 저장, 버전 변경.

## Context / Constraints
런타임 Resources 녹화를 제거하고 85개 fixture와 GUID를 Editor 테스트 폴더로 이동한다.

## Approach (Checklist)
- [x] **Step 0: Recon** 기존 동작과 모듈 지침을 확인했다. 공동 구현 계획은 fast/medium/heavy 검토를 거쳤다.
- [x] **Step 1: Implementation** 3개 병렬 수신, 콘텐츠 hash/크기 검증, 환경별 파일 캐시, 제한 재시도, 취소와 열린 UI 갱신, IDBFS 동기화.
- [x] **Step 2: Tests** Unity EditMode 508개, 미리보기 PlayMode 12개 및 WebGL 빌드 통과. 파일 손상/환경 격리/정리 및 기존 재생 회귀 확인. 생성된 framework에 IDBFS 동기화 함수가 연결되었고 player data에는 기존 fixture JSON과 경로가 없다.
- [ ] **Step 3: Rollout / Rollback** 게임 #102 및 로비 #62 후 배포한다. 실패 시 해당 모듈 커밋을 되돌린다.

## Validation
- **Commands to run:** Unity EditMode / MagicPreviewTests PlayMode / WebGL build; git diff --check.
- **Expected output:** Unity EditMode 508개, 미리보기 PlayMode 12개 통과. 파일 손상/환경 격리/정리 및 기존 재생 회귀 확인.
- 실제 DB 및 배포 브라우저 전체 연동 검증은 배포 확인 항목이다. Graphify 실행 파일이 없어 루트 그래프 갱신은 실행하지 못했다.

## Risks & Rollback
- **Risks:** 운영 입력에 따라 상황 재생 길이가 달라질 수 있으며 서버 생성/전송 비용과 브라우저 저장 동작은 실제 환경에서 확인한다.
- **Rollback steps:** 게임 #102 및 로비 #62 후 배포한다. 실패 시 해당 모듈 커밋을 되돌린다.

## Open Questions
- 실제 DB 입력과 WebGL 브라우저 재실행/저장 용량 초과 검증.
