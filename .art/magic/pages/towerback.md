# 포탑등이

- 서버 키: `towerback`
- 현재 이름: 포탑등이
- 컨셉 이름: 포탑등이
- 시전 계열: 설치
- 진영: 인간 마법 문명
- 상태: game 서버 구현 확인

## 컨셉

인간 마법 문명의 전장에 남아 지속적으로 영향력을 만드는 포탑등이.

## 컨셉 설명

포탑등이는 인간 마법 문명이 납치한 꼬마돌의 등에 지대공 타워를 강제로 결속한 병기다. 꼬마돌의 두 발로 다니며 가까운 지상 적을 주먹으로 치고 공중 적에게는 포탄을 쏜다.

## 설명

플레이어가 사용하는 **설치 계열 마법**이다.
게임플레이 사실은 아래 서버 코드에서 가져왔고, 세계관·아트 문장은
`.art/WORLD.md`와 `.art/STYLE.md`를 적용한 해석이다.

## 동작

1. 탐지 범위에서 공중 표적을 우선 선택한다.
2. 표적에 맞는 무기의 사거리까지 걸어가 멈춘다.
3. 지상 표적은 주먹으로 타격하고 공중 표적은 포신으로 조준해 돌탄을 쏜다.

## 전투 프로필

| 항목 | 분류 | 코드 근거 |
|---|---|---|
| 기동 방식 | 지상 보행 | `TowerbackMob`이 `SUB_SPEED`로 표적을 추적한다. |
| 전투 역할 | 지상 근접, 대공 원거리 | 각 표적의 무기 사거리를 사용한다. |
| 공격 형태 | 지상 단일 타격, 대공 범위 피해 | `strike_ground`는 표적에 명중시키고 `shoot_air`는 표적 주변에도 피해를 준다. |
| 표적 | 지상·공중 | `TargetMask` 기준 |
| 특수 이동·행동 | 추가 특수 이동 없음 | 부착 컴포넌트 기준 |
| 생명주기 | HP 소진 시 파괴 | 파괴·시간제한 컴포넌트 기준 |

## 서버 구조

| 항목 | 값 |
|---|---|
| 구현 클래스 | `TowerbackMagic` |
| 상위 클래스 | `AbstractSummonMagic` |
| 주 프리팹 | `Towerback` |
| 부가 프리팹 | 없음 또는 소스에서 직접 확인되지 않음 |
| 부착 컴포넌트 | `RigidBody`, `ZPhysics`, `CircleCollider`, `AttackMob`, `Tower`, `RockDeathRemnant`, `CommonEffectReceiver` |
| 파라미터 키 | `MASS`, `RADIUS`, `HP`, `SUB_SPEED`, `SUB_DAMAGE`, `ATTACK_INTERVAL`, `SUB_ATTACK_RANGE`, `DAMAGE`, `ATTACK_RANGE` |
| 오브젝트 파라미터 | `TOWERBACK` |

수치 자체는 런타임 DB 데이터다. 이 문서는 키만 기록하며 값을 추정하지 않는다.

## 아트 방향

따뜻한 꼬마돌 몸체와 차가운 인간제 지대공 타워를 명확히 분리한다. 가죽끈·금속 고정구로 강제 결속된 흔적을 보여준다.

- 공통 스타일: A — 2.5D 컷페이퍼
- 생성 참조: `.art/anchors/master-v2/MasterStyleKey.png`와 가장 가까운 재질 앵커
- 실루엣은 실제 동작이 읽혀야 한다. 공격 방향, 이동 방식, 설치 여부를 장식보다 우선한다.

## 미결 사항

- 현재 확인된 세계관 충돌 없음

## 근거

- Magic: `game/src/main/java/com/wordonline/server/game/domain/magic/implement/build/TowerbackMagic.java`
- Prefab initializer: `game/src/main/java/com/wordonline/server/game/domain/object/prefab/implement/misc/TowerbackPrefabInitializer.java`
- 동작: `game-rust/crates/sim/src/game/domain/object/component/mob/statemachine/attacker/towerback_mob.rs`
