# 천둥 새 무리

- 서버 키: `thunder_bird_swarm`
- 현재 이름: 천둥 새 무리
- 컨셉 이름: 천둥새 편대
- 시전 계열: 소환
- 진영: 세계수 전기 정령
- 상태: game 서버 구현 확인

## 컨셉

세계수 전기 정령의 독립 개체로 전장에 합류하는 천둥새 편대.

## 컨셉 설명

천둥 새 세 마리가 전장 위를 날다가 지상 표적을 향해 내려가 부리로 공격한다. 타격 뒤 다시 고도를 높인다. 세계수에서 갈라진 전기 생명으로, 남색 날개에 노란 번개 무늬가 있다.

## 설명

플레이어가 사용하는 **소환 계열 마법**이다.
게임플레이 사실은 아래 서버 코드에서 가져왔고, 세계관·아트 문장은
`.art/WORLD.md`와 `.art/STYLE.md`를 적용한 해석이다.

## 동작

1. 떠 있거나 수평으로 날 때 양 날개가 조금 다른 박자로 움직인다.
2. 지상 표적을 향해 하강할 때는 몸을 기울이고 날개를 접는다.
3. 개체별 피해 이벤트에서 부리 타격이 시작된다. 부리를 회수하고 날개를 펴며 다시 상승한다.
4. 사망하면 낙하 고도에 맞춰 날개를 접는다. 착지 지점에서 살아 있는 전기 소환수에게 에너지를 넘긴다.

## 전투 프로필

| 항목 | 분류 | 코드 근거 |
|---|---|---|
| 기동 방식 | 공중 부유형 | `ZPhysics(gameObject, hoverY)`로 고도를 유지한다. |
| 전투 역할 | 급강하 타격 | `ThunderBirdMob` |
| 공격 형태 | 단일 표적 | 공격 컴포넌트와 시전 계열 기준 |
| 표적 | 지상 | `TargetMask` 기준 |
| 특수 이동·행동 | 공격 전 하강과 타격 후 재상승 | `ThunderBirdMob` |
| 생명주기 | HP 소진 시 낙하 후 파괴와 에너지 이전 | `ZPhysics`와 `ElectricDeathEnergy` |

## 서버 구조

| 항목 | 값 |
|---|---|
| 구현 클래스 | `thunderBirdSwarmMagic` |
| 상위 클래스 | `AbstractSpawnMagic` |
| 주 프리팹 | `ThunderBird` |
| 부가 프리팹 | 없음 또는 소스에서 직접 확인되지 않음 |
| 부착 컴포넌트 | `RigidBody`, `ZPhysics`, `CircleCollider`, `ThunderBirdMob`, `ElectricDeathEnergy`, `LightningSummonEffectReceiver` |
| 파라미터 키 | `MASS`, `RADIUS`, `HP`, `SPEED`, `DAMAGE`, `ATTACK_INTERVAL`, `ATTACK_RANGE` |
| 오브젝트 파라미터 | `THUNDER_BIRD` |

수치 자체는 런타임 DB 데이터다. 이 문서는 키만 기록하며 값을 추정하지 않는다.

## 아트 방향

세계수에서 갈라진 전기 생명과 각진 전격 파편을 사용한다.

- 공통 스타일: A — 2.5D 컷페이퍼
- 생성 참조: `.art/anchors/master-v2/MasterStyleKey.png`와 가장 가까운 재질 앵커
- 실루엣은 실제 동작이 읽혀야 한다. 공격 방향, 이동 방식, 설치 여부를 장식보다 우선한다.

## 미결 사항

- 현재 확인된 세계관 충돌 없음

## 근거

- Magic: `game/src/main/java/com/wordonline/server/game/domain/magic/implement/spawn/thunderBirdSwarmMagic.java`
- Prefab initializer: `game/src/main/java/com/wordonline/server/game/domain/object/prefab/implement/misc/third/ThunderBirdPrefabInitializer.java`
