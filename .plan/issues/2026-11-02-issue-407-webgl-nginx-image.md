# 2026-11-02 — WebGL Nginx 이미지 GHCR 게시 추가

- Date: 2026-11-02
- GitHub Issue: #407
- Status: Implemented

## Goal
Unity 빌드 후 별도 Play 저장소 대신 Nginx 이미지를 GHCR에 게시합니다.
## Non-goals
서버/DNS 전환, 다른 track과 itch.io 변경은 포함하지 않습니다.
## Context / Constraints
기존 작업 트리를 보존하고 최신 dev의 별도 worktree에서 수정합니다. 버전은 올리지 않습니다.
## Approach (Checklist)
- [x] **Step 0: Recon** 기존 빌드·압축·PWA 설정 확인
- [x] **Step 1: Implementation** 이미지 빌드/게시, MIME·캐시, Compose 예시 추가
- [x] **Step 2: Tests** fixture 기반 실제 컨테이너 HTTP 검사와 CI 검사 추가
- [x] **Step 3: Rollout / Rollback** SHA 태그 및 서버 실행/롤백 문서 추가
## Validation
- **Commands to run:** build-image.sh, check-image.sh, actionlint
- **Expected output:** 이미지 빌드 성공, MIME/압축/404 검사 통과
## Risks & Rollback
- **Risks:** 실제 Unity 빌드·GHCR 게시에는 GitHub Actions 실행이 필요합니다.
- **Rollback steps:** 이전 SHA 이미지로 Compose를 재실행합니다.
## Open Questions
없음. 실제 monolong 전환은 이번 변경 이후 별도로 적용합니다.

## Plan Review
- FAST REVIEW: NONPASS — manual feature ref의 dev 태그 덮어쓰기 방지와 HTTP 검사 범위 보완.
- MEDIUM REVIEW: PASS — 두 저장소에 독립된 작은 설정 유지.
- HEAVY REVIEW: PASS — dev ref 제한, 실제 HTTP 검사 및 서버 적용 경계 반영.
