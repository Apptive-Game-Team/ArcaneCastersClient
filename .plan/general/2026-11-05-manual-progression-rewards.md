# 2026-11-05 — 기존 보상 페이지 수동 수령 추가

- Date: 2026-11-05
- GitHub Issue: [Client #431](https://github.com/Apptive-Game-Team/ArcaneCastersClient/issues/431), [Lobby #114](https://github.com/Apptive-Game-Team/ArcaneCastersLobby/issues/114), [Database #139](https://github.com/Apptive-Game-Team/ArcaneCastersDatabase/issues/139)
- Status: Implemented and verified locally; rollout pending

## Goal

로비의 기존 레벨 보상 페이지에서 상자와 상자 개봉 이외의 마법 보상을 직접 수령한다. 로비 버튼에 미수령 빨간 점을 표시하고 수령 후 실제 지급된 보상으로 획득 연출을 보여준다.

## Non-goals

별도 보상함 화면을 만들지 않는다. 상자 개봉 보상을 다시 수령하게 만들거나, 기존 지급분을 회수·재지급하지 않는다. 플레이에 필요한 세력 선택의 시작 카드·시작 덱은 즉시 지급을 유지한다.

## Context / Constraints

- XP와 레벨은 즉시 반영한다. 레벨 보상과 일일 상자의 소유권만 수령 시 반영한다.
- 기존 progression 보상은 sync 안에서 자동 지급된다. 기존 퀘스트는 MANUAL과 원자적 claim API를 이미 지원한다.
- 새로운 Flyway 버전은 database의 dev와 magic-card 양쪽에서 중복되지 않아야 한다.
- 중복 클릭, 동시 요청, 응답 유실 후 재시도, 계정/서버 전환, 창 닫기를 검증한다.
- 개발 서버 배포와 운영 서버 배포는 코드·마이그레이션 검증 결과와 구분한다.
- `rewards.manual-claims-enabled`는 기본 false다. migration과 새 서버를 모든 인스턴스에 먼저 반영하고 새 클라이언트 검증 후 활성화한다. 비활성 상태의 새 서버는 해당 사용자의 기존 pending을 안전하게 정산한 뒤 자동 지급 경로를 유지한다. 미수령 행이 남은 상태에서는 pending을 모르는 과거 서버 바이너리로 롤백하지 않는다.
- 신형 클라이언트는 progression sync와 quest 목록/claim 요청에 `X-Reward-Claims: manual-v1`을 보낸다. flag=true여도 이 capability가 없는 구버전 sync/check는 기존 자동 정산 호환 경로를 사용하며 해당 사용자의 progression pending도 같은 원자적 claim 경로로 정산한다. 신형/구형의 혼합 접근은 같은 원장·source 키로 조정하고 새 클라이언트는 다음 조회에서 자동 수령된 상태를 반영한다. `flag=true + capability 없음`, `flag=true + capability 있음`, 두 클라이언트의 동시 정산을 실제 PostgreSQL에서 검사한다.
- capability는 직접 sync뿐 아니라 모든 정산 진입점으로 전달한다. 새 클라이언트의 `/quests/check`, quest 목록/claim, progression 조회/claim, 상자 개봉 요청에도 적용하고, `ChestService.openChest()`가 내부에서 호출하는 progression sync에도 같은 manual 모드를 명시적으로 넘긴다. 신형 클라이언트의 로비 check·상자 개봉이 pending을 자동 수령하지 않는 회귀 검사를 필수로 둔다.

### 합의할 API 계약

- `GET /api/users/mine/progression/rewards/pending`: `{ enabled, baselineLevel, claimedLevels: int[], rewards: [{ id, source, level: int?, createdAt, rewards: [{ rewardType, rewardId, rewardKey, amount, rewardSlots }] }] }`.
- `POST /api/users/mine/progression/rewards/{id}/claim`: `{ id, alreadyClaimed, rewards: [{ rewardType, rewardId, rewardKey, amount, rewardSlots }] }`. 다른 사용자의 ID는 404. 같은 사용자 재시도는 저장된 실제 지급 영수증을 200으로 재반환한다.
- 기존 `GET /api/users/mine/quests`와 quest claim API는 유지한다. 409는 재조회·수령 상태 확인으로 처리하고 신규 획득 연출을 반복하지 않는다.
- 첫 수동 전환 시 기존 level을 지급 완료 기준선으로 저장한다. 그 이하 레벨을 새 pending으로 재구성하지 않는다. 수령 시 상자 source와 마법의 실제 증가량을 다시 확인하여 snapshot의 예정 수량을 실제 지급으로 오인하지 않는다.

## Approach (Checklist)

- [x] **Step 0: Recon** 기존 progression·quest claim·상자 지급 코드와 클라이언트 보상 페이지 조사.
- [x] **Step 1: Implementation** 서버에 사용자별 pending progression 보상 그룹과 마법/상자 스냅샷을 저장한다. source key의 사용자별 유일성을 유지하고 sync에서는 새 레벨/오늘 일일 보상을 enqueue한다. 과거 소유 상자에 같은 source가 있으면 다시 enqueue하지 않는다.
- [x] **Step 1: Implementation** GET pending 보상과 사용자 소유 보상 ID의 POST claim을 추가한다. 수령은 progression 사용자 행과 보상 행을 일정 순서로 잠그고 재고 증가·상자 생성·claimed 상태를 한 트랜잭션으로 처리한다. 응답 유실 재시도는 같은 영수증을 반환하되 다시 지급하지 않는다.
- [x] **Step 1: Implementation** 기존 퀘스트의 MAGIC/CARD/CHEST 보상은 기존 목록·claim API를 재사용하고 새 progression 원장으로 복제하지 않는다. 기능 활성 중에만 기존 자동 check가 이 보상들을 지급하지 않으며 목록에는 유효 claimMode=MANUAL을 반환한다. 과거 COMPLETED 상태는 유지하고 기존 모험 수령 API와 경합해도 중복 지급하지 않는다. 온보딩 필수 지급과 상자 개봉은 변경하지 않는다.
- [x] **Step 1: Implementation** 기존 페이지 안에 받을 보상 목록을 추가하고 레벨 행의 미도달/수령 가능/수령 완료를 구분한다. 별도 씬 없이 같은 상세 패널·보상 타일·수령 버튼을 사용한다. 빨간 점은 서버 미수령 결과에 기반하며 조회 실패를 빈 목록으로 취급하지 않는다.
- [x] **Step 1: Implementation** 새 progression claim은 실제 재고 증가량을 영수증에 저장하고 재시도 시 같은 결과를 반환한다. 기존 quest claim은 성공 응답만 연출하고 409에서는 목록·재고 재조회와 수령 상태 확인 안내로 복구한다. quest 영수증 테이블은 추가하지 않는다. 수령 완료 영수증만 획득 연출로 표시하고 사용자/서버별 중복 표시를 방지한다. 확정 전 재고를 낙관적으로 올리지 않는다.
- [x] **Step 1: Implementation** 요청 시작의 사용자·서버를 고정한다. 페이지를 닫으면 연출만 취소하고 이미 전송한 수령 요청의 결과는 다음 조회로 조정한다. 계정·서버가 바뀌면 이전 응답을 현재 재고·표시 상태에 적용하지 않는다.
- [x] **Step 2: Tests** 실제 격리 PostgreSQL에서 migration, 이전 자동 지급분 보존, 신규 pending, 동시·재시도 claim, 다른 계정 접근, 지급 실패 rollback을 검증한다. Unity에서는 빨간 점·같은 페이지 진입·수령 성공/실패/복구·취소와 XP 연출을 검사한다.
- [ ] **Step 3: Rollout / Rollback** migration → 서버 → 클라이언트 순서로 배포 준비한다. 구버전 서버에서는 수령 불가 안내와 재시도를 제공하고 다른 로비 조작은 유지한다.

## Validation

- **Commands to run:** lobby의 관련 Gradle 단위/실제 PostgreSQL 통합 테스트, migration 검증, Unity MCP EditMode/PlayMode 및 실제 프리팹 캡처.
- **Expected output:** 같은 보상은 한 번만 재고에 반영된다. 레벨 보상 페이지 밖에 새 수령 화면이 생기지 않으며, 로비의 미수령 표시와 서버 목록이 일치한다.

## Risks & Rollback

- **Risks:** 자동 지급에서 수동 수령으로 바뀌므로 과거 지급 source의 재발급, 기존 quest check와의 경합, 구버전 클라이언트가 새 pending을 표시하지 못하는 경우를 막아야 한다.
- **Rollback steps:** 새 서버를 유지한 채 기능 설정을 false로 전환해 해당 사용자의 다음 sync/claim에서 pending을 같은 원자적 정산 경로로 처리한다. 이미 지급된 source는 재지급하지 않고 완료 처리한다. pending과 영수증을 삭제하거나 재고를 되돌리지 않는다. pending이 남아 있으면 이전 서버 바이너리로 되돌리지 않으며, 클라이언트만 이전 버전으로 되돌린 경우에도 새 서버의 자동 정산 호환 경로를 유지한다.

## Review feedback

- Fast/medium의 must-fix를 반영해 progression 영수증과 기존 quest 409 복구를 구분했다.
- 수령 전 예정량과 실제 카드 증가량을 구분하고 과거 자동 지급 기준선·상자 source 재검사를 추가했다.
- 구버전 혼재는 비활성 feature 설정으로 보호하고 새 서버 유지 상태의 구체적인 롤백 절차를 확정했다.
- Rejected feedback: 모든 퀘스트에도 새 영수증 원장을 추가하는 방안은 기존 완료 상태·409 재조회로 해결 가능하므로 제외했다.

## Open Questions

- 없음. 사용자는 기존 보상 페이지에서 직접 수령하는 방식을 선택했다. 미수령 보상 목록은 그 페이지 내부 영역으로 추가한다.

## Verified results

- Unity MCP: 관련 EditMode 17개와 PlayMode 8개 통과. Editor 프리뷰에서 보상 페이지, 상자 목록과 닫힘·열림 프레임, 전적 스크롤을 검수했다. 연속 상자 연출과 배포된 API를 함께 쓰는 E2E는 미검증이다.
- 서버: 집중 대상 88개 최종 통과. 격리 PostgreSQL에서 V123→V124 업그레이드, 동시 수령·재시도·rollback을 확인했다. 전체 447개 중 443개 통과, 기존 데이터 기대값 3개 실패, 1개 건너뜀.
- 서버 PR #116, DB PR #140. 기능 설정은 기본 false이며 merge·배포·활성화는 아직 하지 않았다.
