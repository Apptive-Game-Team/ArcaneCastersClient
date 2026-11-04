# 2026-11-04 — 돌 가고일 공중 유닛 추가

- Date: 2026-11-04
- GitHub Issue: None (자동 승인 검토에서 원격 이슈 생성 거부)
- Status: Draft

## Goal

중형 돌 속성 공중 유닛 StoneGargoyle을 추가한다. 지상 적에게 근접 강하 공격을 하고 다시 떠오른다. 천둥새보다 공격 주기가 짧다.

## Non-goals

Java 서버와 기존 유닛 스탯, 배포 버전을 바꾸지 않는다.

## Context / Constraints

현재 dev는 magics의 element/cast_kind/prefab 모델이다. 번개 정령은 HP 240, damage 50, speed 2, mass 1, radius 0.5, mana 35다. 천둥새는 attack_interval 3, 복귀 상승속도 1이다. Graphify 그래프와 명령이 이 작업 공간에 없다. Rust 도구 설치 위치를 확인 중이다.

## Approach (Checklist)

- [x] **Step 0: Recon**: Rust 천둥새 강하 행동, DB 번개 정령 스탯, Unity 공중 프리팹과 master-v2 화풍 확인.
- [ ] **Step 1: Implementation**: Rust PrefabType/GameObjectKey/registry에 StoneGargoyle 등록. ThunderBirdMob을 재사용하고 지상 마스크·Rock 속성·CommonEffectReceiver 사용. ZPhysics에 기존 기본값을 유지하는 상승속도 지정 생성자를 추가하고 가고일만 3으로 설정. DB는 HP 240/damage 50/speed 2/mass 1/radius 0.5/mana 35/attack_interval 1.5/attack_range 0.6/quantity 1/range 6/spawn_height 3 등록, TYPE_Unit/CAT_Medium/CAT_Melee 태그 및 magic_id 연결. 기본 공격 피해는 천둥새의 기존 낙하거리 계산을 따른다. Unity 프리팹과 6개 번역 테이블 추가. make-game-art/imagegen으로 붉은 눈과 이끼 낀 돌 날개의 후보 생성.
- [ ] **Step 2: Tests**: 실제 시뮬레이션에서 지상 타격·공중 제외·상승 복귀·짧은 반복 공격 주기·표적 파괴 시 중단·Rock 속성 검증. 천둥새의 기존 상승속도 유지 확인. Cargo 전체 검사와 DB migration 정적 검사, Unity 참조·이름·번역 검증.
- [ ] **Step 3: Rollout / Rollback**: 아트 승인 전에는 후보를 Assets 밖에 보관한다. DB와 클라이언트가 준비된 뒤 Rust 버전을 사용한다. 원격 issue/PR 생성은 자동 검토 거부로 보류.

## Validation

- **Commands to run:** cargo fmt --check; cargo clippy --all-targets -- -D warnings; cargo test; scripts/ci/validate-migrations.sh; Unity YAML 참조 및 번역 검사; 아트 축소·알파 검수.
- **Expected output:** 신규 유닛이 지상에만 피해를 주고 공중 상태를 유지하며, 천둥새보다 더 자주 재공격한다. 기존 천둥새 테스트 유지.

## Risks & Rollback

- **Risks:** 공격 간격만 줄이면 복귀 시간이 실제 공격을 늦출 수 있다. 수치 테스트로 실제 주기를 비교한다. Migration 번호는 모든 원격 트랙에서 확인한다.
- **Rollback steps:** 배포 전 세 모듈 변경을 함께 제거한다. DB 적용 후에는 forward-fix를 작성한다.

## Open Questions

- 아트 후보를 생성한 뒤 사용자의 외형 승인을 받는다.
