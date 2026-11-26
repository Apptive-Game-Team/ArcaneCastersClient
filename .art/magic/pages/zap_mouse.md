# 찌릿쥐

- 서버 키: `zap_mouse`
- 현재 이름: 찌릿쥐
- 컨셉 이름: 찌릿꼬리쥐
- 시전 계열: 소환
- 진영: 세계수 전기 정령
- 상태: game 서버 구현 확인

## 컨셉

세계수 전기 정령의 독립 개체로 전장에 합류하는 찌릿꼬리쥐.

## 컨셉 설명

금색 등가시와 둥근 귀가 달린 찌릿쥐 두 마리가 네 발로 지상 표적을 좇는다. 가까운 앞발과 먼 뒷발이 체중을 받다가 반대편에 넘기고, 타격할 때 머리와 앞쪽 가시를 낮게 내민다.

## 설명

플레이어가 사용하는 **소환 계열 마법**이다.
게임플레이 사실은 아래 서버 코드에서 가져왔고, 세계관·아트 문장은
`.art/WORLD.md`와 `.art/STYLE.md`를 적용한 해석이다.

## 동작

1. 목표 지점 주변에 두 마리를 소환한다.
2. Rust 서버는 건물을 우선하는 지상 추적과 근접 피해를 처리한다.
3. 지상 녹화에서 두 마리가 차례로 공격한다. 피해 이벤트에 해당하는 찌릿쥐만 타격 자세를 시작한다.

## 전투 프로필

| 항목 | 분류 | 코드 근거 |
|---|---|---|
| 기동 방식 | 지상 질주 | Rust `BuildingPrioMob`과 `zap_mouse/ground` 녹화 |
| 전투 역할 | 전투 개체 | 부착 AI·마법 컴포넌트 기준 |
| 공격 형태 | 지상 근접 타격 | 피해를 준 개체의 `HitEvent`에서 가시 자세 표시 |
| 표적 | 지상 | `TargetMask` 기준 |
| 특수 이동·행동 | 추가 특수 이동 없음 | 부착 컴포넌트 기준 |
| 생명주기 | 소스에서 시간 제한을 직접 확인하지 못함 | 파괴·시간제한 컴포넌트 기준 |

## 서버 구조

| 항목 | 값 |
|---|---|
| 구현 클래스 | `ZapMouseMagic` |
| 상위 클래스 | `AbstractSpawnMagic` |
| 주 프리팹 | `ZapMouse` |
| 부가 프리팹 | 없음 또는 소스에서 직접 확인되지 않음 |
| 부착 컴포넌트 | Rust `RigidBody`, `ZPhysics`, `CircleCollider`, `BuildingPrioMob`, `CommonEffectReceiver` |
| 파라미터 키 | `MASS`, `RADIUS`, `HP`, `SPEED`, `DAMAGE`, `ATTACK_INTERVAL` |
| 오브젝트 파라미터 | `ZAP_MOUSE` |

수치 자체는 런타임 DB 데이터다. 이 문서는 키만 기록하며 값을 추정하지 않는다.

## 아트 방향

세계수에서 갈라진 전기 생명과 각진 전격 파편을 사용한다.

- 공통 스타일: A — 2.5D 컷페이퍼
- 생성 참조: `.art/anchors/master-v2/MasterStyleKey.png`와 가장 가까운 재질 앵커
- 실루엣은 실제 동작이 읽혀야 한다. 공격 방향, 이동 방식, 설치 여부를 장식보다 우선한다.

## 미결 사항

- 현재 확인된 세계관 충돌 없음

## 근거

- Magic: `game/src/main/java/com/wordonline/server/game/domain/magic/implement/spawn/ZapMouseMagic.java`
- Prefab initializer: `game/src/main/java/com/wordonline/server/game/domain/object/prefab/implement/lightning/ZapMousePrefabInitializer.java`
