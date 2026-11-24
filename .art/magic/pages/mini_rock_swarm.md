# 꼬마 돌 무리

- 서버 키: `mini_rock_swarm`
- 현재 이름: 꼬마 돌 무리
- 컨셉 이름: 꼬마돌 돌격대
- 시전 계열: 소환
- 진영: 돌 골렘 부족
- 상태: game 서버 구현 확인

## 컨셉

돌 골렘 부족의 독립 개체로 전장에 합류하는 꼬마돌 돌격대.

## 컨셉 설명

꼬마 돌 무리는 돌 골렘 부족의 꼬마돌을 소환한다. 꼬마돌은 지상 표적까지 걸어가 근접 공격을 한다. 따뜻한 돌몸과 어깨의 이끼를 유지하며 주먹을 짧게 내민다.

## 설명

플레이어가 사용하는 **소환 계열 마법**이다.
게임플레이 사실은 아래 서버 코드에서 가져왔고, 세계관·아트 문장은
`.art/WORLD.md`와 `.art/STYLE.md`를 적용한 해석이다.

## 동작

1. 목표 지점에 아군 개체를 소환한다.
2. 꼬마돌이 지상 표적의 사거리까지 걸어가 근접 공격을 한다.

## 전투 프로필

| 항목 | 분류 | 코드 근거 |
|---|---|---|
| 기동 방식 | 지상 보행 | `Slime`이 `SPEED`를 사용한다. |
| 전투 역할 | 근접 소환수 | 지상 표적을 추적하고 사거리에서 멈춘다. |
| 공격 형태 | 단일 타격 | 지상 표적에 피해를 준다. |
| 표적 | 지상 | `TargetMask` 기준 |
| 특수 이동·행동 | 추가 특수 이동 없음 | 부착 컴포넌트 기준 |
| 생명주기 | 소스에서 시간 제한을 직접 확인하지 못함 | 파괴·시간제한 컴포넌트 기준 |

## 서버 구조

| 항목 | 값 |
|---|---|
| 구현 클래스 | `MiniRockSwarmMagic` |
| 상위 클래스 | `AbstractSpawnMagic` |
| 주 프리팹 | `MiniRock` |
| 부가 프리팹 | 없음 또는 소스에서 직접 확인되지 않음 |
| 부착 컴포넌트 | `RigidBody`, `ZPhysics`, `CircleCollider`, `Slime`, `RockDeathRemnant`, `CommonEffectReceiver` |
| 파라미터 키 | `MASS`, `RADIUS`, `HP`, `SPEED`, `DAMAGE`, `ATTACK_INTERVAL` |
| 오브젝트 파라미터 | `MINI_ROCK` |

수치 자체는 런타임 DB 데이터다. 이 문서는 키만 기록하며 값을 추정하지 않는다.

## 아트 방향

따뜻한 석재 조각과 이끼. 인간제 회색 기계와 구분한다.

- 공통 스타일: A — 2.5D 컷페이퍼
- 생성 참조: `.art/anchors/master-v2/MasterStyleKey.png`와 가장 가까운 재질 앵커
- 실루엣은 실제 동작이 읽혀야 한다. 공격 방향, 이동 방식, 설치 여부를 장식보다 우선한다.

## 미결 사항

- 현재 확인된 세계관 충돌 없음

## 근거

- Magic: `game/src/main/java/com/wordonline/server/game/domain/magic/implement/spawn/MiniRockSwarmMagic.java`
- Prefab initializer: `game/src/main/java/com/wordonline/server/game/domain/object/prefab/implement/rock/MiniRockPrefabInitializer.java`
- 이동과 근접 공격: `game-rust/crates/sim/src/game/domain/object/prefab/implement/rock/mini_rock_prefab_initializer.rs`
