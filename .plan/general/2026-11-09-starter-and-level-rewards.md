# 2026-11-09 — 진영 시작 카드와 레벨별 고정 보상표 적용

- Date: 2026-11-09
- GitHub Issue: Client #397 / Database #141 / Lobby #120
- Status: 검증 완료, 기존 PR 반영

## Goal

시작 카드와 시작 덱은 실습과 같은 5종을 각 3장 지급한다.
HELL: fire_shot, rock_golem, rock_mage, wall_golem, stone_gargoyle.
NATURE: chain_lightning, electric_spearman, aqua_archer, tree_golem, thunder_spirit.
사용자가 마지막으로 보낸 레벨업 보상표를 그대로 적용하며, HELL 15는 rock_golem,
40은 crater_carrier다. rock_golem은 1·15레벨에 함께 등록한다.

## Non-goals

기존 계정의 카드·덱·이미 발급된 수령 원장·지급 상한·유닛 성능은 변경하지 않는다.
튜토리얼 단계·손 안내·마법 체험 규칙도 유지한다. 공유 원본 DB를 변경하지 않는다.

## Context / Constraints

V121의 magic_id 단독 PK와 faction/magic 유일성은 동일 카드의 다중 레벨 등록을 막는다.
모든 적용된 migration은 보존하고, 열린 Database #140(feature/139) 위 V125를 추가한다.
모든 원격 이력과 main/magic-card 존재 여부에서 번호 충돌을 확인한다.
Lobby는 #119(feature/118) 위 fix/120, Client는 기존 #398에 반영한다.
실행 중인 로컬 로비를 중지한 후 같은 전용 체크아웃을 새 브랜치로 전환한다.

## Approach (Checklist)

- [x] **Step 0: Recon** 지급·덱 생성이 progression_magic_rewards를 공통으로 읽는 것을 확인했다.
- [x] **Step 1: Implementation** V125에서 PK를 (faction,unlock_level,magic_id)로 바꾸고
  기존 faction/magic 유일성을 제거한다. 정책 30행을 승인 표로 교체하고 모든 대상 마법의
  PLAYER 등록을 STRICT 조회로 검증한다. 다른 사용자 데이터는 만지지 않는다.
  HELL 레벨 5/10/15/20/25/30/35/40/45/50은 ember_spirit_swarm, meteor_shower,
  rock_golem, magma_spirit, frenzy_totem, dragon_tower, earth_call, crater_carrier,
  evil_ent, fire_lord_spirit다. NATURE는 water_slime_swarm, tornado_strike, life_tree,
  vine_spirit, grass_generator, tide_call, overgrowth, storm_rider, vine_world, sea_serpent다.
  Lobby eligibleMagics는 SELECT DISTINCT로 조인 중복을 제거한다. 가장 이른 해금 레벨,
  카드 소유 상한, 레벨별 조회는 그대로 유지한다. Test-only Postgres fixture는 새 정책과 맞춘다.
  Client 보상 배열을 새 5종으로 교체하고 실습 배열과의 일치를 검증한다.
  기존 완료 팝업은 이 배열을 그대로 써서 새 카드의 간단 설명도 표시한다.
- [x] **Step 2: Tests** 로컬 복제 DB에 V124·V125를 적용해 SQL 정책 30행, 시작 5종씩,
  각 레벨 1종씩, rock_golem의 1·15행, FK·보유·덱 데이터 보존을 확인한다.
  Lobby Postgres 테스트에서 다중 행 상자 후보가 한 번만 나오는지와 시작 지급 멱등성을 검증한다.
  새 로컬 게스트 두 진영의 소유 카드·시작 덱 각 3장, 보상 API 1~50레벨을 승인 표와 비교한다.
  HELL 계정을 실제 15레벨까지 올려 고정 보상 지급 후에도 rock_golem 3장 상한을 확인한다.
  해당 카드가 3장 미만일 때 1·14·15레벨 상자 후보에 정확히 한 번씩 포함되는지 확인한다.
  마이그레이션 전후 이미 발급된 미수령 원장·보유·덱 내용을 비교한다.
  Unity MCP로 EditMode/PlayMode 보상·팝업 테스트와 새 완료 화면 렌더를 확인한다.
- [x] **Step 3: Rollout / Rollback** 기존 DB#140와 Lobby#119 선행 체인을 보존한다.
  호환 가능한 Lobby 중복 제거를 먼저 적용하고 V125, Client #398 순으로 적용한다.
  새 DB·로비 draft PR을 열고 #398에 의존성을 연결한다. root pointer는 병합 후 갱신 대상이다.
  Database #142, Lobby #121로 게시한다.

## Validation

- **Commands to run:** scripts/ci/validate-migrations.sh origin/feature/139,
  test-env migrate(기존 로컬 복제 DB만), ./gradlew.bat test,
  Postgres integration tests, Unity FactionTutorialTests/FactionTutorialPlayModeTests,
  git diff --check, graphify update .
- **Expected output:** 보상표 30행, 신규 진영별 5종×3장과 덱 일치, 두 진영 레벨 5~50 표 일치,
  후보 중복·중복 지급 없음, 기존 계정 데이터 유지, 카드 설명 정상 표시.

## Risks & Rollback

- **Risks:** 새 시작 카드와 15레벨 rock_golem이 중복되지만 사용자 승인 표를 따른다.
  기존 최대 3장 정책에 따라 이미 3장인 카드의 추가 보상은 보유량을 늘리지 않는다.
  미수령 보상 원장에 이미 저장된 옛 보상은 그대로 둔다.
- **Rollback steps:** 적용 후 DB는 기존 지급표를 복구하는 정방향 migration으로 처리한다.
  신규 복합 PK를 예전 단독 PK로 되돌리려면 다중 레벨 행을 먼저 정리해야 한다.
  Client/Lobby 코드는 각 기능 커밋을 revert할 수 있다.

## Open Questions

없음. 사용자가 최종 표의 분화구 운반수까지 그대로 적용하도록 지시했다.

## Plan Review

- FAST REVIEW: PASS. 15레벨 실제 지급·3장 상한, 1·14·15 후보 1회, 미수령 원장 보존 확인 추가.
- MEDIUM REVIEW: PASS. 필요한 복합 PK와 DISTINCT 외 새 정책 계층은 추가하지 않는다.
- Rejected feedback: 없음.
- HEAVY REVIEW: PASS. 구현 차단사항 없음.

## Verification Results

- DB 정적 검사 통과. 원격 main/magic-card 브랜치는 없으며 전체 원격 이력에 V125 충돌 없음.
- 기존 로컬 DB에 V124·V125 적용 완료. 기존 user_magics/decks/deck_cards 내용의 해시 동일.
- Postgres 신규 보상표 통합 5개 통과. 신규 지급·덱·승인 표·최초 해금·추첨 중복·15레벨 상한과
  V125 재실행 전후 기존 보유·덱·미수령 원장 보존을 검증했다.
- 로비 전체 454개 중 450개 통과, 기존 무시 1개, 실패 3개. 성장 관련 41개는 모두 통과.
  AppearanceChestPostgresIntegrationTest 1개와 QuestClaimPostgresIntegrationTest 2개 실패는
  변경 전 55c9656와 V124까지만의 migration에서도 같은 결과를 재현했다.
- 실제 로컬 게스트 두 진영에서 시작 5종×3장, 15장 시작 덱, 1~50레벨 보상 API와 재선택 멱등성 통과.
- Unity MCP EditMode 9개·PlayMode 8개 통과. 새 10종의 보상 카드 설명과 완료 화면 렌더 2장을 확인했다.
- WebGL 빌드와 공유 DB 적용은 수행하지 않았다. Graphify CLI는 없어 갱신하지 못했다.
