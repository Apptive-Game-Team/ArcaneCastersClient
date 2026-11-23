# 2026-11-16 — 밸런스 시나리오 실행과 Unity 직접 플레이 추가

- Date: 2026-11-16
- GitHub Issue: Apptive-Game-Team/ArcaneCastersClient#463
- Status: Implemented

## Goal
동일한 Rust 실행기로 자동 비교와 Unity 직접 플레이를 지원하고 수치 변경안 5개의 전후 결과를 저장한다.

## Non-goals
운영 DB 변경, 배포, 자동 밸런스 확정, 덱 전체 봇 학습은 포함하지 않는다.

## Context / Constraints
기존 미커밋 변경과 분리한 worktree를 사용한다. Rust preview의 PreviewParameters, 고정 tick과 프레임 DTO를 활용하되 passive target, death field 억제, 무제한 시전은 사용하지 않는다. 튜토리얼의 배치·사거리·마나 검사 패턴을 적용한다. 전적·보상·계정·DB 연결이 없는 별도 loopback 전용 binary다.

## Approach (Checklist)
- [x] **Step 0: Recon** preview, tutorial, Editor playground를 확인했다.
- [x] **Step 1: Implementation** server crate의 balance 모듈과 ac-balance binary에 snapshot, scenario, patch, seeded runner를 추가한다. 실제 prefab, 양측 mana, 시간별 command를 사용한다. 단일 live battle을 별도 OS thread에서 실행하며 HTTP로 catalog/create/state/cast/pause/speed를 제공한다. CLI batch는 같은 runner를 빠르게 실행한다. JSON report에는 snapshot hash, code revision, seed, 조건, 실패한 시전, 생존·잔여 HP·마나 지표와 재생 frames를 기록한다. 지원하지 않는 파라미터 변경과 미사용 변경은 명시적으로 실패/경고한다.
- [x] Unity는 Editor playground generator와 prefab UI를 재사용한 balance scene을 만든다. Editor menu로 로그인 없이 loopback 실행기에 접속한다. 시나리오 선택, baseline/candidate 전환, 재시작, 일시정지, 배속, magic icon 선택·field click, mana/status 표시를 지원한다. 기존 GameScene renderer를 사용한다. 생성 scene과 host는 player build에서 제외한다.
- [x] **Step 2: Tests** seed 재현성, 패치 격리, 잘못된 입력·마나·사거리 거부, 양측 전투, 실시간/자동 동일 tick 경로, HTTP 실패/복구를 검증한다. 2026-11-04 콘텐츠 snapshot을 명시해 5개 후보를 복수 거리·배치·seed에서 대조하고 관련 조합을 검사한다. 수치는 제안이며 운영에 적용하지 않는다. Unity compile 및 가능한 Editor 실행 검증을 수행한다.
- [x] **Step 3: Rollout / Rollback** local binary와 Editor menu로만 진입한다. 독립 기능 제거로 원복 가능하다.

## Validation
- **Commands to run:** cargo fmt --check; cargo clippy --all-targets -- -D warnings; cargo test; ac-balance batch; Unity batch validation.
- **Expected output:** 재현 가능한 paired JSON 결과, 5개 후보별 측정·한계 보고서, 게임 renderer에서 직접 시전 가능한 Editor scene.

## Risks & Rollback
- **Risks:** snapshot은 현재 운영값과 다를 수 있다. 단위 실험은 덱 승률·재미를 증명하지 않는다. mana와 range를 지켜도 덱·hand draw는 v1 범위 밖이며 report에 명시한다. 프레임 HP 차이는 누적 피해·회복과 같지 않으므로 정확한 수치가 없는 항목을 추정값으로 표시하지 않는다. 서버 tick이 멈춰야 pause이며 client Time.timeScale로 대체하지 않는다.
- **Rollback steps:** 별도 binary와 Editor 진입을 제거한다.

## Open Questions
- 없음. 5개 후보의 구체적인 값은 snapshot 확인 후 고정한다.

## Review changes
- 초기 배치 prefab은 무료 fixture로 명시하고 마나 효율의 근거로 쓰지 않는다. 비용 비교 시나리오는 모두 실제 시전 명령으로 소모한다.
- live는 LeftPlayer의 예약 시전만 제외하고 RightPlayer 예약 시전과 초기 배치는 유지한다. 자동 실행과 live는 동일한 step/cast 함수를 사용한다.
- 후보마다 사전에 질문·대상 시나리오·판단 지표를 suite에 기록한다. 전체 baseline/candidate 쌍과 시전 실패도 보고한다.
- live는 한 전투만 유지하고 새 create에서 이전 전투와 버퍼를 폐기한다. idle 20분에 정리하고 프레임 기록 상한을 둔다. 클라이언트 generation과 battle ID로 늦은 응답을 무시한다.
- 관측되지 않은 누적 피해·회복은 v1에서 미측정으로 명시하고 HP 시계열·제거 시각·시전 결과·마나를 제공한다. 광범위한 전투 observer 추가는 하지 않는다.
- 기존 preview 전체 리팩터링은 하지 않는다. 시나리오 정의와 실행·시간 제어만 분리한다.
- FAST REVIEW: 위 must-fix 4개를 반영했다. MEDIUM REVIEW: PASS, 위 should-fix를 반영했다.

## Final validation
- HEAVY REVIEW: PASS. Rust balance 테스트 5개 포함 1,400개 통과(기존 실패 2개 제외), fmt/clippy 통과. 수정 전 commit에서도 동일 실패 확인.
- Unity 실제 Play Mode에서 직접 시전·일시정지·수정안 전환·기록 저장·재생 통과.
- 후보 5개 × 시나리오 18개 × seed 3개 = 270쌍 비교 완료. 전체 비대상 대조군도 검사했으며 나무 골렘 회복 조정의 추가 조합 영향 9쌍을 보고했다.
- 상세 결과와 미확인 범위는 balance-lab-results-2026-11-16.md를 따른다.
