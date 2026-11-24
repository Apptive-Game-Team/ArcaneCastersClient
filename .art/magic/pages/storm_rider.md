# 폭풍 기수

- 서버 키: `storm_rider`
- 현재 이름: 폭풍 기수
- 컨셉 이름: 폭풍갈기 기수
- 시전 계열: 소환
- 진영: 세계수 바람 정령
- 상태: game 서버 구현 확인

## 컨셉

세계수 바람 정령의 독립 개체로 전장에 합류하는 폭풍갈기 기수.

## 컨셉 설명

구름에 탄 기수가 지상 표적을 추격한다. 구름의 앞뒤가 부풀고 눌리는 사이에 청록 망토와 투구 장식이 뒤따른다. 표적에 닿으면 기수가 몸을 낮추고 창을 내지른다.

## 설명

플레이어가 사용하는 **소환 계열 마법**이다.
게임플레이 사실은 아래 서버 코드에서 가져왔고, 세계관·아트 문장은
`.art/WORLD.md`와 `.art/STYLE.md`를 적용한 해석이다.

## 동작

1. 목표 지점에 아군 개체를 소환한다.
2. `PlayerPrioMob`이 지상 표적을 추적한다.
3. 클라이언트는 `HitEvent`가 도착하면 피해 방향으로 낮게 창을 내밀고 다섯 자세에 걸쳐 회수한다.

## 전투 프로필

| 항목 | 분류 | 코드 근거 |
|---|---|---|
| 기동 방식 | 지상 이동 | `PlayerPrioMob` 위치 갱신과 `storm_rider/ground` 녹화 |
| 전투 역할 | 전투 개체 | 부착 AI·마법 컴포넌트 기준 |
| 공격 형태 | 지상 근접 타격 | `PlayerPrioMob`의 피해 이벤트 |
| 표적 | 지상 | `TargetMask` 기준 |
| 특수 이동·행동 | 추가 특수 이동 없음 | 부착 컴포넌트 기준 |
| 생명주기 | 소스에서 시간 제한을 직접 확인하지 못함 | 파괴·시간제한 컴포넌트 기준 |

## 서버 구조

| 항목 | 값 |
|---|---|
| 구현 클래스 | `StormRiderMagic` |
| 상위 클래스 | `AbstractSpawnMagic` |
| 주 프리팹 | `StormRider` |
| 부가 프리팹 | 없음 또는 소스에서 직접 확인되지 않음 |
| 부착 컴포넌트 | `RigidBody`, `ZPhysics`, `CircleCollider`, `PlayerPrioMob`, `ElectricDeathEnergy`, `CommonEffectReceiver` |
| 파라미터 키 | `MASS`, `RADIUS`, `HP`, `SPEED`, `DAMAGE`, `ATTACK_INTERVAL` |
| 오브젝트 파라미터 | `STORM_RIDER` |

수치 자체는 런타임 DB 데이터다. 이 문서는 키만 기록하며 값을 추정하지 않는다.

## 아트 방향

넓고 읽기 쉬운 곡선 종이 띠로 흐름과 회전을 표현한다.

- 공통 스타일: A — 2.5D 컷페이퍼
- 생성 참조: `.art/anchors/master-v2/MasterStyleKey.png`와 가장 가까운 재질 앵커
- 실루엣은 실제 동작이 읽혀야 한다. 공격 방향, 이동 방식, 설치 여부를 장식보다 우선한다.

## 미결 사항

- 현재 확인된 세계관 충돌 없음

## 근거

- Magic: `game/src/main/java/com/wordonline/server/game/domain/magic/implement/spawn/StormRiderMagic.java`
- Prefab initializer: `game/src/main/java/com/wordonline/server/game/domain/object/prefab/implement/misc/StormRiderPrefabInitializer.java`
