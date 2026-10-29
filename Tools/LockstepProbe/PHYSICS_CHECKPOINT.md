# 물리 checkpoint의 상태 범위

`PhysicsWorld.WriteCheckpoint(BinaryWriter)`는 복원 파일이 아닌 결정론 비교용
정규화 state를 기록한다. 중간 snapshot 복원은 지원하지 않고, 재접속은 초기
state와 확정 입력을 처음부터 재생한다.

## 포함하는 상태

- adapter의 `lastId`, `contacts`, 전투 body binding·edge 순서·`nextBody`, 실패 callback의 객체 ID 집합.
- body의 위치·회전·속도·질량·관성·중력·damping·PositionUpdateMode, collidable의 world transform과 local offset, shape의 실제 크기와 derived radius/volume/inertia cache, material과 충돌 rule.
- sleep candidacy의 이전 속도·누적 저속 시간·flag, simulation island의 부모 관계·활성 flag·멤버 수·candidate 수, 멤버/connection 순서와 split 대기열.
- DynamicHierarchy의 tree 구조·leaf의 stable body ID·각 node bounds·현재/최대 volume, broadphase overlap 순서, narrowphase pair 순서와 양쪽 collidable list index.
- persistent contact의 ID·위치·법선·깊이와 제거 대기 목록, general convex manifold의 local offset·기준 깊이, GJK의 state·simplex·분리축 cache.
- solver permutation과 constraint 순서·설정·활성 상태, contact별 누적 impulse, 활성 contact의 실제 처리 순서와 미사용 pool에서 향후 take하는 순서의 설정값.

`PhysicsStateEngineAccessors.cs`는 private numeric/value state를 각 소유 타입의
partial method에서 읽는다. 실행 중 reflection이나 native 주소를 사용하지 않는다.
이 파일과 engine reference writer는 `ArcaneCasters.FixedPhysics` assembly에 있으며,
상위 Simulation assembly를 참조하지 않는다.

## 제외하는 상태와 이유

- `Entity.InstanceId`와 `hashCode`, Material/BroadPhaseEntry의 hashcode는 process-local dictionary identity다. 참조는 명시적 body ID와 결정론 순회 순번으로 기록하며 dictionary bucket은 기록하지 않는다. lookup mapping의 의미는 body/pair/rule 목록으로 기록한다.
- lock·thread delegate·event dispatcher와 engine 소유의 상수 callback은 단일 thread world에서 전투 규칙 state가 아니다. PhysicsWorld는 engine event subscriber를 외부에 노출하지 않는다. 게임의 collision callback state는 상위 Simulation checkpoint가 담당한다.
- Solver Jacobian·현재 mass/방향·friction 계수·lever arm 등은 다음 tick의 `Update`에서 실제 입력으로 덮어쓴 뒤만 읽는 scratch다. `accumulatedImpulse`는 warm start에 쓰이므로 기록한다. 비활성 pool constraint의 scratch 역시 기록하지 않는다. 원본 solver가 실제 사용하는 contact 추가 순서를 보존하고, allocation slot identity 대신 향후 take 순서의 persistent SolverSettings를 기록한다. 필드 inventory는 scratch 항목도 나열하여 신규 필드 추가를 감지한다.
- 사용하지 않는 contact/node pool의 raw storage는 다음 take/setup에서 덮어쓰는 allocation cache다. 실제 다음 tree 재구성에 사용되는 `InternalNode.maximumVolume`은 제외하지 않는다. pooled node 삽입 때 이를 0으로 초기화하여 새 allocation과 같은 첫 Refit 동작을 보장한다.
- interpolation/buffered rendering state와 profiling 시간, collection capacity는 다음 고정 tick 계산에 들어가지 않는다.
- 사용자 정의 shape·constraint·collision group은 이 adapter가 제공하지 않는다. 알 수 없는 유형은 checkpoint에서 예외로 실패한다.

## 검증

`PhysicsCheckpointChecks.Run()`은 엔진 필드 inventory 변경, 독립 world 300tick,
앞선 경기 pool을 사용하는 추가 world, 눈에 보이는 위치/속도를 바꾸지 않은 body
cache mutation, contact 깊이·GJK 분리축·solver impulse mutation을 확인한다.
`ReplayDigest()`는 별도 process 및 Unity/IL2CPP 간 비교할 300tick digest를 제공한다.

`physics-state-fields.csv`와 generated manifest의 type 이름은 generic argument의
assembly 버전을 포함하지 않는다. `.NET System.Private.CoreLib`와 Unity `mscorlib`
차이만으로 audit가 실패하지 않게 한다. 필드를 추가하면 기존 manifest를 맹목적으로
재생성하지 말고, 실제 다음 tick에서 읽는지 확인해 typed/reference/scratch 정책과
mutation 검사를 함께 갱신한다.

## 2026-10-29 검증 기록

전체 원본·모델·core probe를 선행한 서로 다른 process 두 번에서 모두 통과했다.
이전 검사에서 남은 pool state도 사용하므로 전용 검사만 단독으로 실행한 결과보다
넓은 회귀 검사다.

- 원본 C# 대응 893개, model-original38개, core와 물리 checkpoint 검사 통과.
- 물리 checkpoint300tick digest: `824e21f06de89a1939124d9bc0bed6ecc279cbe64db48acfe52e69432c08d963`.
- 기존 물리10000tick digest: `BF7A3B87B1681C31BF98A630E4746B9CE2ED3C95C9ADF1F00AE4159F6B150C7D` 유지.
- `GeneralConvexPairTester.CleanUp`은 pool 잔여 localDirection을0으로 정리한다.
- solver 미등록 constraint의 옛 solverIndex는 다음 Add에서 덮어쓰므로-1로 정규화한다.
- `currentIterations`/`iterationsAtZeroImpulse`는 다음 Solver/Group.Update에서0으로 덮어쓰는 scratch다.
- 실제 solver 순서는 Convex/NonConvex의 Update/ExclusiveUpdate/SolveIteration override를 기준으로 기록한다. raw slot 순서는 allocation identity이므로 state로 사용하지 않는다.

증거는 `.generated/physics-review-full.log`와
`.generated/physics-review-full-second-process.log`다. Unity/IL2CPP 실행 결과는
부모 통합 검증에 별도로 기록한다.