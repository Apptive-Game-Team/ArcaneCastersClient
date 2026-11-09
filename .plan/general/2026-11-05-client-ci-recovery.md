# 2026-11-05 — 클라이언트 테스트와 macOS 빌드 CI 복구

- Date: 2026-11-05
- GitHub Issue: None
- Status: In progress

## Goal
실패한 PR 테스트를 복구하고 macOS 릴리스 빌드 실패를 게시 없이 재현해 수정합니다.

## Non-goals
새 버전 발행과 기존 릴리스 파일 교체는 포함하지 않습니다.

## Context / Constraints
PR #398·#406은 언어 초기화 누락으로 실패했습니다. dev 테스트는 Playground 자동 생성 중 중단됐습니다. v1.6.0의 macOS 빌드 로그는 HTTP 410으로 조회할 수 없습니다. 기존 작업 파일은 별도 worktree로 보존합니다.

게시 없는 재현 빌드에서 Unity는 성공했지만 `Contents/MacOS/Arcane Casters`를 생성해 updater가 요구하는 `ArcaneCasters` 경로 검사에 실패했습니다. 제품 이름은 유지하고 빌드된 실행 파일과 `CFBundleExecutable`만 정규화합니다. 패키징의 기존 재서명 순서는 유지합니다.

## Approach (Checklist)
- [x] **Step 0: Recon** 실패 로그와 최신 PR check 확인
- [x] **Step 1: Implementation** 언어 초기화 수정 전파, batch Playground 자동 생성 제외, macOS 실행 파일과 plist 정규화
- [ ] **Step 2: Tests** macOS 두 아키텍처의 게시 없는 빌드 검사 추가, 실제 CI 확인 후 오류 수정
- [ ] **Step 3: Rollout / Rollback** 수정 PR과 검증 결과 제공

## Validation
- **Commands to run:** git diff --check, GitHub Actions Unity Tests 및 Desktop build check
- **Expected output:** 최신 PR 테스트 통과, macOS 두 빌드에서 updater가 요구하는 실행 파일과 plist 일치, 실제 바이너리 아키텍처 확인

## Risks & Rollback
- **Risks:** macOS 빌드 재현에서 추가 실패가 드러날 수 있습니다.
- **Rollback steps:** 이 PR의 수정 커밋을 revert합니다.

## Open Questions
- 만료된 macOS 로그의 원인은 실제 빌드로 확인합니다.
