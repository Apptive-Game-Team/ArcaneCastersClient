# 2026-11-04 — 돌 가고일 공중 유닛 추가

- Date: 2026-11-04
- GitHub Issue: https://github.com/Apptive-Game-Team/ArcaneCastersClient/issues/415
- Status: 로컬 구현 및 검증 완료

## Goal

중형 돌 속성 공중 유닛 StoneGargoyle을 추가한다. 지상 적에게 근접 강하 공격을 하고 다시 떠오른다. 천둥새보다 공격 주기가 짧다.

## Non-goals

Java 서버와 기존 유닛 스탯, 배포 버전을 바꾸지 않는다.

## Context / Constraints

현재 dev는 magics의 element/cast_kind/prefab 모델이다. 번개 정령은 HP 240, damage 50, speed 2, mass 1, radius 0.5, mana 35다. 천둥새는 attack_interval 3, 복귀 상승속도 1이다. Graphify 그래프와 명령이 이 작업 공간에 없다. Rust 도구 설치 위치를 확인 중이다.

## Approach (Checklist)

- [x] **Step 0: Recon**: Rust 천둥새 강하 행동, DB 번개 정령 스탯, Unity 공중 프리팹과 master-v2 화풍 확인.
- [x] **Step 1: Implementation**: Rust PrefabType/GameObjectKey/registry에 StoneGargoyle 등록. ThunderBirdMob을 재사용하고 지상 마스크·Rock 속성·CommonEffectReceiver 사용. ZPhysics에 기존 기본값을 유지하는 상승속도 지정 생성자를 추가하고 가고일만 3으로 설정. DB는 HP 240/damage 50/speed 2/mass 1/radius 0.5/mana 35/attack_interval 1.5/attack_range 0.6/quantity 1/range 6/spawn_height 3 등록, TYPE_Unit/CAT_Medium/CAT_Melee 태그 및 magic_id 연결. 기본 공격 피해는 천둥새의 기존 낙하거리 계산을 따른다. Unity 프리팹과 6개 번역 테이블 추가. make-game-art/imagegen으로 붉은 눈과 이끼 낀 돌 날개의 후보 생성.
- [x] **Step 2: Tests**: 실제 시뮬레이션에서 지상 타격·공중 제외·상승 복귀·짧은 반복 공격 주기·표적 파괴 시 중단·Rock 속성 검증. 천둥새의 기존 상승속도 유지 확인. Cargo 전체 검사와 DB migration 정적 검사, Unity 참조·이름·번역 검증.
- [ ] **Step 3: Rollout / Rollback**: 아트 승인 전에는 후보를 Assets 밖에 보관한다. DB와 클라이언트가 준비된 뒤 Rust 버전을 사용한다. 2026-11-05 사용자 승인에 따라 이슈 등록·push·초안 PR을 진행한다.

## Validation

- **Commands to run:** cargo fmt --check; cargo clippy --all-targets -- -D warnings; cargo test; scripts/ci/validate-migrations.sh; Unity YAML 참조 및 번역 검사; 아트 축소·알파 검수.
- **Expected output:** 신규 유닛이 지상 표적에만 피해를 주고 hover 높이로 복귀하며, 천둥새보다 더 자주 재공격한다. 강하 중 타깃 판정은 기존 천둥새와 같은 높이 규칙을 따른다. 기존 천둥새 테스트 유지.

## Risks & Rollback

- **Risks:** 공격 간격만 줄이면 복귀 시간이 실제 공격을 늦출 수 있다. 수치 테스트로 실제 주기를 비교한다. Migration 번호는 모든 원격 트랙에서 확인한다.
- **Rollback steps:** 배포 전 세 모듈 변경을 함께 제거한다. DB 적용 후에는 forward-fix를 작성한다.

## Open Questions

- 없음. 사용자가 지옥불 차원 30% 수정안 적용을 승인했다.

## Plan Review

- Fast 검토 반영: 봇 대공 상성에 필요한 CAT_Flying 태그도 등록한다. 공중형 ZPhysics와 높이 기반 피격 분류를 구별한다.
- Medium 검토 PASS: 기존 with_hover 경로를 바꾸지 않는다. 1.5초는 설정 공격 간격이며 실제 타격 간격은 강하·복귀를 포함해 테스트에서 측정한다. damage 50은 낙하거리 보정 전 수치다. 두 enum의 VALUES 배열 크기도 갱신한다.
- Heavy 검토 PASS.
- 2026-11-05 사용자 수정: 돌 부족 70%에 지옥불 차원 영향 30%를 반영한다. 날개 끝·어깨·발톱의 그을린 석재와 붉은 균열만 추가하고 몸통과 이끼, 붉은 눈은 유지한다.
- 강하 중 표적이 공중으로 올라가는 경우 기존 코드가 복귀하지 못하므로, 공격 상태에서도 기존 타깃 유효성 판정을 다시 수행한다.
- 시뮬레이션 타격 시각: 가고일 2.55/5.15/7.65초, 천둥새 4.10/9.60/14.95초. 안정된 반복 간격은 각각 약 2.50초와 5.35초다.

## 검증 결과

- 가고일 시뮬레이션 테스트 4개 통과. 공중 제외, 지상 강하, hover 복귀, 반복 공격 주기, 표적 파괴·공중 이동 시 취소 확인.
- 가고일 커밋만 담긴 독립 worktree에서 `cargo fmt --check`와 전체 `cargo clippy --all-targets -- -D warnings` 통과.
- 전체 `cargo test --workspace`에서 서버의 기존 Windows 관련 테스트 2개 실패: `matches_java_instant_to_string`, `a_closed_port_is_an_io_failure`. 변경 전 dev `2e952b4`에서도 같은 두 실패를 재현했다.
- Unity 2022.3.34f1 EditMode `ObjectSfxTests` 31개 통과. 번역 테이블 9개 신규 행과 프리팹 override 대상도 검사했다.
- DB `scripts/ci/validate-migrations.sh origin/dev` 통과. V122는 모든 원격 트랙의 V121보다 높다. Docker 엔진을 사용할 수 없어 실제 DB 적용은 검사하지 못했다.
- Graphify 명령과 그래프가 설치되어 있지 않아 `graphify update .`는 실행할 수 없었다.
- 2026-11-05 사용자가 지옥불 차원 30% 수정안을 승인하여 리소스에 적용했다.
- 두 기준선 실패를 제외한 서버 테스트 전체와 나머지 모든 workspace 크레이트 테스트도 독립 worktree에서 통과했다.

## GitHub 연결

- 클라이언트: Apptive-Game-Team/ArcaneCastersClient#415
- DB: Apptive-Game-Team/ArcaneCastersDatabase#134
- Rust: Apptive-Game-Team/ArcaneCastersGameRust#27
- 모노레포 포인터: Apptive-Game-Team/ArcaneCasters#31
- PR 외형 검수 자료: `docs/pr-media/415/stone-gargoyle-art-review.png`. 실제 전투 화면 캡처가 아닌 스프라이트 비교 시트다.

## 날갯짓 추가 — 2026-11-05

- 사용자 요청으로 날개 올림·수평·내림 3프레임을 생성했다. `12321` 왕복을 선택하여 `1,2,3,2` 배열을 반복한다.
- `SpriteFrameAnimator`를 재사용하며 간격은 0.16초다. 공통 캔버스·PPU·피벗·Full Rect로 몸통과 체력바 기준을 유지한다.
- 실제 Rust 전투 기록을 Unity에서 재생했다. 137개 샘플의 프레임 전환이 `1,2,3,2` 반복과 일치하고 Sprite bounds가 같다. 13.7초·960x540·10fps 전투 GIF(2,566,610bytes)를 확인했다.
- 저장된 프리팹에 대해 EditMode `ObjectSfxTests.CatalogCoversPrefabsAndHasNoLegacySoundOwnersOrMissingScripts` 1개 통과.
