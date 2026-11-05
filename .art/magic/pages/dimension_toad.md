# 차원 두꺼비

- 서버 키: `dimension_toad`
- 현재 이름: 차원 두꺼비
- 컨셉 이름: 균열두꺼비
- 시전 계열: 소환
- 진영: 차원 유랑종
- 상태: game 서버 구현 확인

## 컨셉

차원유랑종의 통로 운반자인 경계 운반자. 코드 식별자는 `DimensionToad`다.

## 컨셉 설명

경계 운반자는 무너진 세계 조각을 몸 안에 품고 차원 사이를 이동한다.
차원유랑종은 불지옥 차원의 악마들을 워드 대륙으로 보내는 배후이며, 악마들에게
붙잡혀 이용당한 종족이라는 종전 설정은 사용하지 않는다. 경계 운반자 개체와
배후의 정확한 관계는 아직 정하지 않았다.

화산편(`FireTadpole`)과 폭풍편(`LightningTadpole`)은 통로를 통해 전장에 합류하는
별개의 독립 종이다. 경계 운반자의 알이나 새끼로 설명하지 않는다.
아래의 교대 소환은 기존 게임 동작이며, 부모·자식 관계를 뜻하지 않는다.

## 설명

플레이어가 사용하는 **소환 계열 마법**이다.
게임플레이 사실은 아래 서버 코드에서 가져왔고, 세계관·아트 문장은
`.art/WORLD.md`와 `.art/STYLE.md`를 적용한 해석이다.

## 동작

1. 직접 공격하지 않고 지상 위협에서 거리를 벌린다.
2. `FireTadpole`과 `LightningTadpole`을 10초마다 번갈아가며 제한 없이 소환한다.

## 전투 프로필

| 항목 | 분류 | 코드 근거 |
|---|---|---|
| 기동 방식 | 지상 이동형 | 이동 AI가 지면 경로와 `RigidBody` 속도를 사용한다. |
| 전투 역할 | 원소 올챙이 소환 | 부착 AI·마법 컴포넌트 기준 |
| 공격 형태 | 10초 간격 교대 소환 | 공격 컴포넌트와 시전 계열 기준 |
| 표적 | 지상 위협 감지 | `TargetMask` 기준 |
| 특수 이동·행동 | 위협에서 도주·두 종류 하위 개체 무제한 생성 | 부착 컴포넌트 기준 |
| 생명주기 | HP 소진 시 파괴 | 파괴·시간제한 컴포넌트 기준 |

## 서버 구조

| 항목 | 값 |
|---|---|
| 구현 클래스 | `DimensionToadMagic` |
| 상위 클래스 | `AbstractSpawnMagic` |
| 주 프리팹 | `DimensionToad` |
| 부가 프리팹 | `FireTadpole`, `LightningTadpole` |
| 부착 컴포넌트 | `RigidBody`, `ZPhysics`, `CircleCollider`, `NonAttackingCowardMob`, `LimitedSequenceSpawner`, `CommonEffectReceiver` |
| 파라미터 키 | `MASS`, `RADIUS`, `HP`, `SPEED`, `ATTACK_INTERVAL`, `DETECTION_RANGE`, `PANIC_DURATION`, `SPAWN_INTERVAL` |
| 오브젝트 파라미터 | `DIMENSION_TOAD` |

수치 자체는 런타임 DB 데이터다. 이 문서는 키만 기록하며 값을 추정하지 않는다.

## 아트 방향

차원 틈새의 이질적인 생물 조직과 흡수한 원소 마력이 함께 보이도록 표현한다. 특정 원소 진영의 재질을 그대로 사용하지 않는다.

- 공통 스타일: A — 2.5D 컷페이퍼
- 생성 참조: `.art/anchors/master-v2/MasterStyleKey.png`와 가장 가까운 재질 앵커
- 실루엣은 실제 동작이 읽혀야 한다. 공격 방향, 이동 방식, 설치 여부를 장식보다 우선한다.

## 미결 사항

- 배후 개체와 경계 운반자의 정확한 관계 및 개별 가담 범위는 미정이다.
- 현재 이름과 컨셉 이름은 기존 표기를 기록한 것이다. 이 문서 변경으로
  서버 키, 리소스 이름이나 게임 내 표시 이름을 바꾸지 않는다.

## 근거

- Magic: `game/src/main/java/com/wordonline/server/game/domain/magic/implement/spawn/DimensionToadMagic.java`
- Prefab initializer: `game/src/main/java/com/wordonline/server/game/domain/object/prefab/implement/misc/DimensionToadPrefabInitializer.java`
