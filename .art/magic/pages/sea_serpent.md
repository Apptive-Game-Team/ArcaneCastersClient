# 바다뱀

- 서버 키: `sea_serpent`
- 현재 이름: 바다뱀
- 컨셉 이름: 심해 뱀
- 시전 계열: 소환
- 진영: 귀속 미정
- 상태: game 서버 구현 확인

## 컨셉

귀속 미정의 독립 개체로 전장에 합류하는 심해 뱀.

## 컨셉 설명

심해 뱀은 소환된 뒤 표적을 따라가며 지나간 길에 물 장판을 남긴다. 입에서 물줄기를 쏘면 줄기에 닿는 지상·공중 적이 피해를 받는다.

## 설명

플레이어가 사용하는 **소환 계열 마법**이다.
게임플레이 사실은 아래 서버 코드에서 가져왔고, 세계관·아트 문장은
`.art/WORLD.md`와 `.art/STYLE.md`를 적용한 해석이다.

## 동작

1. 표적의 사거리까지 지면을 따라 이동하며 물 장판을 남긴다.
2. 물줄기를 뿜고 물줄기에 닿는 적에게 0.35초 뒤 피해를 준다.

## 전투 프로필

| 항목 | 분류 | 코드 근거 |
|---|---|---|
| 기동 방식 | 지상 이동 | `SeaSerpentMob`이 `SPEED`로 표적을 추적한다. |
| 전투 역할 | 원거리 공격 | 물줄기로 지상과 공중을 공격한다. |
| 공격 형태 | 직선 범위 피해 | `fire_hydro_pump`가 물줄기에 닿는 적을 찾는다. |
| 표적 | 지상·공중 | `TargetMask` 기준 |
| 특수 이동·행동 | 이동 경로에 원소 장판 생성 | 부착 컴포넌트 기준 |
| 생명주기 | 소스에서 시간 제한을 직접 확인하지 못함 | 파괴·시간제한 컴포넌트 기준 |

## 서버 구조

| 항목 | 값 |
|---|---|
| 구현 클래스 | `SeaSerpentMagic` |
| 상위 클래스 | `AbstractSpawnMagic` |
| 주 프리팹 | `SeaSerpent` |
| 부가 프리팹 | `WaterField` |
| 부착 컴포넌트 | `RigidBody`, `ZPhysics`, `CircleCollider`, `SeaSerpentMob`, `PathSpawner`, `CommonEffectReceiver` |
| 파라미터 키 | `MASS`, `RADIUS`, `HP`, `SPEED`, `DAMAGE`, `ATTACK_INTERVAL`, `ATTACK_RANGE`, `BEAM_WIDTH` |
| 오브젝트 파라미터 | `SEA_SERPENT` |

수치 자체는 런타임 DB 데이터다. 이 문서는 키만 기록하며 값을 추정하지 않는다.

## 아트 방향

기존 세계관만으로 귀속을 확정할 수 없다. 시각 제작 전 설정 결정이 필요하다.

- 공통 스타일: A — 2.5D 컷페이퍼
- 생성 참조: `.art/anchors/master-v2/MasterStyleKey.png`와 가장 가까운 재질 앵커
- 실루엣은 실제 동작이 읽혀야 한다. 공격 방향, 이동 방식, 설치 여부를 장식보다 우선한다.

## 미결 사항

- 세계관 진영 귀속 필요

## 근거

- Magic: `game/src/main/java/com/wordonline/server/game/domain/magic/implement/spawn/SeaSerpentMagic.java`
- Prefab initializer: `game/src/main/java/com/wordonline/server/game/domain/object/prefab/implement/water/SeaSerpentPrefabInitializer.java`
- 이동과 발사: `game-rust/crates/sim/src/game/domain/object/component/mob/statemachine/attacker/sea_serpent_mob.rs`
