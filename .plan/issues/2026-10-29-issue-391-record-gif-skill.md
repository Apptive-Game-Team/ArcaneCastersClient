# 2026-10-29 — 실제 재생 녹화와 용량 제한 GIF 제작 스킬 추가

- Date: 2026-10-29
- GitHub Issue: #391 / PR #392
- Status: Implemented / validated

## Goal
실제 앱/Unity 재생을 녹화하고 지정 용량 이하 GIF로 전달하는 record-gif 스킬을 작성한다. 3MB를 변경 가능한 기본값으로 삼고 개인 스킬에도 설치한다.

## Non-goals
게임 코드 변경, 새로운 이미지 생성, 기존 녹화 재촬영, 무단 업로드.

## Context / Constraints
전용 스킬은 없고 magick은 이미지 변환 지침이다. 시작 시 Jua 폰트만 dirty이므로 보존한다. 녹화에서 발견한 EOF/gamma/실제 프레임 시간/추가 폰트 캐시 문제를 기록한다.

## Approach (Checklist)
- [x] **Step 0: Recon** 기존 스킬, skill-creator, 실제 녹화 결과와 관련 PR을 확인했다.
- [x] **Step 1: Implementation** 스킬·Unity 녹화 reference·FFmpeg helper·AGENTS 검색 항목을 작성했다.
- [x] **Step 2: Tests** 실제 녹화의 용량·타임라인·디코딩과 기존 출력 보호를 검증했다. 프로젝트/개인 스킬 형식과 5개 파일의 해시 일치를 확인했다.
- [x] **Step 3: Rollout / Rollback** 개인 스킬 설치와 기존 PR #392 갱신으로 전달한다. 신규 스킬 패키지와 이 커밋만으로 되돌릴 수 있다.

## Validation
- **Commands to run:** quick_validate.py, py_compile, 실제 MP4/ffconcat 변환, GIF 디코딩·byte 검사, 기존 출력 보호 검사, 패키지 해시 대조.
- **Expected output:** GIF가 3,000,000바이트 이하이고 약 32초를 유지한다. 실패 시 용량 초과를 성공으로 보고하지 않는다.
- **Results:** 실제 MP4→GIF는 3회 설정 후 2,373,982바이트/32.25초, 실제 ffconcat→GIF는 2,738,691바이트/32.25초였다. FFmpeg 전체 디코딩 통과. Pillow로 프레임 129개·시간·치수를 독립 대조했다. 명시 구간(2초부터 1초) 결과는 정확히 1초/4프레임이었다. 1바이트 제한은 종료 코드 2와 실패 report만 남기고 GIF를 생성하지 않았다. 기존 결과는 종료 코드 1로 보호했고 SHA256이 유지됐다. Python 컴파일과 프로젝트 스킬 quick_validate 통과.
- **Limits:** 이번에는 기존 실제 녹화로 helper를 검증했고 Unity 재촬영은 하지 않았다. 앞선 녹화의 직접 ScreenCapture 방식과 EOF/gamma/font-cache 교훈을 reference에 기록했다. Graphify 갱신은 CLI 미설치로 실행하지 못했다.

## Risks & Rollback
- **Risks:** 용량은 콘텐츠마다 다르므로 고정 설정으로 보장하지 않는다. 무제한 재시도와 임의 대사 삭제를 피한다.
- **Rollback steps:** 신규 커밋과 개인 record-gif 패키지만 되돌린다. 사용자 파일과 녹화 원본은 유지한다.

## Open Questions
없음.
