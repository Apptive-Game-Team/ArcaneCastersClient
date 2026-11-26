# 바람 정령

- 서버 키: `wind_spirit`
- 현재 이름: 바람 정령
- 컨셉 이름: 질풍 정령
- 시전 계열: 소환
- 진영: 세계수 바람 정령
- 상태: game 서버 구현 확인

## 컨셉

세계수 바람 정령의 독립 개체로 전장에 합류하는 질풍 정령.

## 컨셉 설명

질풍 정령은 세계수 바람 정령의 소환 마법이다. `ZPhysics(gameObject, hoverY)`로 고도를 유지하고 공중 표적을 향해 활공한다. 몸통 주위를 두르는 넓은 곡선 종이 띠의 두꺼운 부분이 옮겨가며 바람의 흐름을 표현한다.

## 설명

플레이어가 사용하는 **소환 계열 마법**이다.
게임플레이 사실은 아래 서버 코드에서 가져왔고, 세계관·아트 문장은
`.art/WORLD.md`와 `.art/STYLE.md`를 적용한 해석이다.

## 동작

1. 떠 있을 때는 몸통 뒤의 바람이 말리며, 수평 이동할 때는 두꺼운 띠가 꼬리로 이어진다.
2. 낮은 공중 표적으로 하강할 때 띠와 꼬리가 몸통 쪽으로 모인다.
3. 대상에 닿으면 마지막 자세에서 기존 폭발과 제거 표현으로 이어진다.

## 전투 프로필

| 항목 | 분류 | 코드 근거 |
|---|---|---|
| 기동 방식 | 공중 부유형 | `ZPhysics(gameObject, hoverY)`로 고도를 유지한다. |
| 전투 역할 | 자폭 공격 | 부착 AI·마법 컴포넌트 기준 |
| 공격 형태 | 접근 후 자폭 | 공격 컴포넌트와 시전 계열 기준 |
| 표적 | 공중 | `TargetMask` 기준 |
| 특수 이동·행동 | 공중 표적 접근과 하강 | `SelfDestructMob` |
| 생명주기 | 접근 후 자폭 또는 HP 소진 시 파괴 | `SelfDestructMob` |

## 서버 구조

| 항목 | 값 |
|---|---|
| 구현 클래스 | `WindSpiritMagic` |
| 상위 클래스 | `AbstractSpawnMagic` |
| 주 프리팹 | `WindSpirit` |
| 부가 프리팹 | 없음 또는 소스에서 직접 확인되지 않음 |
| 부착 컴포넌트 | `RigidBody`, `ZPhysics`, `CircleCollider`, `SelfDestructMob`, `CommonEffectReceiver` |
| 파라미터 키 | `MASS`, `RADIUS`, `HP`, `SPEED`, `DAMAGE`, `ATTACK_INTERVAL`, `ATTACK_RANGE` |
| 오브젝트 파라미터 | `WIND_SPIRIT` |

수치 자체는 런타임 DB 데이터다. 이 문서는 키만 기록하며 값을 추정하지 않는다.

## 아트 방향

넓고 읽기 쉬운 곡선 종이 띠로 흐름과 회전을 표현한다.

- 공통 스타일: A — 2.5D 컷페이퍼
- 생성 참조: `.art/anchors/master-v2/MasterStyleKey.png`와 가장 가까운 재질 앵커
- 실루엣은 실제 동작이 읽혀야 한다. 공격 방향, 이동 방식, 설치 여부를 장식보다 우선한다.

## 미결 사항

- 현재 확인된 세계관 충돌 없음

## 근거

- Magic: `game/src/main/java/com/wordonline/server/game/domain/magic/implement/spawn/WindSpiritMagic.java`
- Prefab initializer: `game/src/main/java/com/wordonline/server/game/domain/object/prefab/implement/wind/WindSpiritPrefabInitializer.java`
