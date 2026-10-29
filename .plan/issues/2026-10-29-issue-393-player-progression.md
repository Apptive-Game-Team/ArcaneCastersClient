# 2026-10-29 — 세력 선택과 플레이어 성장 보상 추가

- Date: 2026-10-29
- GitHub Issue: https://github.com/Apptive-Game-Team/ArcaneCastersClient/issues/393
- Status: Implemented; Unity 플레이 검증 대기

## Goal
세력별 시작 카드, 50레벨 성장, 일일 상자, 무작위 카드와 재화 보상을 연결한다.

## Non-goals
재화 소비, 기존 카드 회수, PvE 성장 보상은 추가하지 않는다.

## Context / Constraints
기존 상자 API와 UI를 재사용한다. 서버가 경험치와 지급 여부를 결정한다.
카드 확률 80%, 재화 확률 20%, 재화 10~30을 초기 설정으로 사용한다.
동일 카드 3장 이상이면 추첨에서 제외한다. 추첨할 카드가 없으면 재화만 지급한다.

## Approach (Checklist)
- [x] **Step 0: Recon** 기존 경기 기록, 상자 서비스, ConfirmPage 프리팹 확인
- [x] **Step 1: Implementation** database #132, lobby #105, client #393 연결
- [x] **Step 2: Tests** 경험치 경계, 보상 수량, 중복 지급 방지, 클라이언트 컴파일 확인
- [x] **Step 3: Rollout / Rollback** DB → lobby → client 순서. 배포 전 검증 기록 확인

## Validation
- **Commands to run:** lobby Gradle test, migration 정적 검사, Unity 컴파일 또는 Roslyn 검사
- **Expected output:** 테스트 실제 실행, SQL 버전 중복 없음, 새 스크립트 컴파일 오류 없음

## Risks & Rollback
- **Risks:** 경기 종료 기록이 늦으면 다음 갱신에서 경험치가 지급된다.
- **Rollback steps:** 클라이언트와 lobby 변경을 되돌리고 추가 테이블의 지급 기록은 보존한다.

## Open Questions
현재 경기 기록에서 항복과 일반 패배가 구분되지 않는 경우는 서버 기록 보완이 필요하다.

## 검증 결과
- Unity 생성 참조를 사용한 전체 런타임 C# 컴파일: 오류 0, 기존 경고 3.
- 프리팹 문서 32개: 로컬 fileID 누락과 중복 없음. 새 현지화 키 8개의 한국어·영어 대응 확인.
- 서버 관련 회귀 테스트 51개 통과. 기존 모험 기대값 불일치 3건은 기준 브랜치에서도 재현.
- Unity Editor에서 화면과 입력을 직접 확인하는 검증은 아직 수행하지 않았다.
- 신규 세력 선택 창은 튜토리얼 안내보다 위에 표시하고 선택 후 레벨 표시만 남긴다.
