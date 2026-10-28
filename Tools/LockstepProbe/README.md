# 고정 소수점 물리엔진 재생 검증

이 도구는 엔진 후보의 충돌 재생을 확인한다. 실제 게임의 전투 로직이나 네트워크 lockstep 구현은 아니다. 게임 프로젝트의 Assets와 패키지 설정을 변경하지 않는다.

## 엔진과 패치

- 소스: https://github.com/sam-vdp/bepuphysics1int
- 고정 커밋: `9237daa68c3014fd7c2e93c6a99326ba5248d60b`
- 수치: FixedMath.NET `Fix64`, Q32.32
- 라이선스: upstream `LICENSE.md`에 기록된 Apache-2.0 및 포함 코드의 MIT 고지를 보존한다. 실행 소스는 `LocalPackages/com.arcanecasters.lockstep/Runtime/Physics`에 고정되어 있다.
- 단일 스레드 `Space`, 고정 `1 / 20` tick, 정수 또는 decimal에서만 생성한 시뮬레이션 입력을 사용한다.
- `WINDOWS`는 구형 라이브러리의 BCL HashSet 선택을 위한 심볼이다. WebGL에서도 같은 선택이 필요하다. 심볼이 없으면 자체 HashSet과 BCL 타입이 충돌한다.
- `CHECKMATH`를 모든 검증 빌드에 동일하게 적용한다. 이 심볼은 산술 동작을 바꾸므로 플랫폼별로 다르게 설정하면 안 된다. 특히 더하기/빼기는 포화 연산이며 곱하기는 일부 overflow에서 예외를 낸다.
- `Patch-Engine.ps1`은 CHECKMATH 경로의 옛 필드명 6개와 BoxBoxCollider의 잔존 float 리터럴을 수정한다. 패치 없는 upstream은 이 검증 구성이 컴파일되지 않는다. 엔진 채택 시 별도 버전으로 관리해야 한다.

## 실행

이 폴더에서 PowerShell로 실행한다. vendored 소스를 사용하므로 검증에 엔진 다운로드는 필요하지 않다. `Prepare-Engine.ps1`은 upstream 패치 재현용이다.

```powershell
dotnet run -c Release
dotnet run -c Release --no-build
```

구 32개, 바닥과 벽 5개를 생성하고 정해진 tick마다 속도를 바꾼다. 각 10,000 tick 재생을 같은 프로세스 안에서 두 번 수행해 모든 tick을 비교한다. 별도 프로세스의 출력도 비교한다. 바닥 아래로 떨어지거나 충돌이 한 번도 없으면 실패한다.

Unity 2022.3.34f1에서 별도 프로젝트를 `.generated/UnityProbe`에 생성한 뒤 실행한다. 아래 `Unity.exe`는 설치된 Editor 절대 경로로 바꾼다.

```powershell
& $unityEditor -batchmode -nographics -quit -createProject "$PWD/.generated/UnityProbe" -logFile "$PWD/.generated/unity-create.log"
./Prepare-UnityProbe.ps1
& $unityEditor -batchmode -nographics -quit -projectPath "$PWD/.generated/UnityProbe" -executeMethod UnityProbeRunner.Run -logFile "$PWD/.generated/unity-replay.log"
& $unityEditor -batchmode -nographics -quit -projectPath "$PWD/.generated/UnityProbe" -executeMethod UnityProbeRunner.BuildWebGL -logFile "$PWD/.generated/unity-webgl.log"
```

PowerShell에서 GUI 프로세스 종료를 기다리지 않는 경우 `Start-Process -Wait -WindowStyle Hidden`을 사용한다. WebGL 결과는 `UnityProbe/WebGL`을 로컬 HTTP 서버로 제공해서 브라우저 콘솔의 해시까지 비교한다. 빌드 성공만으로 재생 검증을 통과했다고 판단하지 않는다. 반복 검증에서 옛 data/wasm 캐시가 섞이지 않게 data caching을 끄고 clean development 빌드한다.

## 현재 결과와 한계

2026-10-26 Windows .NET 8, Unity 2022.3.34f1 Editor, WebGL IL2CPP 빌드의 브라우저 실행에서 모두 다음 결과를 확인했다. WebGL은 로컬 HTTP로 제공하고 브라우저 콘솔의 출력도 확인했다.

```text
PASS ticks=10000 traceSha256=BF7A3B87B1681C31BF98A630E4746B9CE2ED3C95C9ADF1F00AE4159F6B150C7D
```

해시는 ID 순서로 위치, 속도, 각속도, 회전, 활성 상태를 기록한 관측 해시다. 엔진의 내부 접촉 캐시까지 직렬화하지 않으므로 중간 상태 복원용 해시가 아니다. 모든 재생은 새 world와 초기 상태에서 시작한다. 다른 CPU 아키텍처·게임 규모 성능·전체 게임 규칙은 별도로 검증해야 한다. 이 검증만으로 전체 이관 완료를 선언하지 않는다.

추가한 `CoreTests`는 같은 프로덕션 소스로 덱/마나/fever, 입력 순서와 중복, 피해/면역/방어막 순서, 속성 상성, 소환 보정, 사거리, trigger enter/stay/exit, raycast, 경기 시계, 예약 이벤트를 검사한다. .NET, Unity Editor, WebGL 브라우저에서 통과했다. 실제 client 프로젝트 전체 스크립트 컴파일도 통과했다.

전송 테스트는 client 루트에서 실행한다.

```powershell
dotnet run --project Tools/LockstepTransportTests -c Release
node Tools/LockstepProbe/StompPluginTests.cjs
```

Native 테스트는 실제 `NativeStompTransport.cs`를 Unity 타입 stub과 함께 컴파일하고 로컬 WebSocket 서버에 연결한다. Unity GameScene 실행이나 Rust와 Unity의 전체 경기 통합 검증을 대신하지 않는다.

## 전체 이관 테스트의 WebGL 실행

2026-10-27에는 원본 이관 테스트 881개를 .NET과 Unity Editor에서 실행했다.
현재 소스로 다시 만든 WebGL/IL2CPP 빌드를 Chrome에서 실행한 결과는
878개 통과, 데스크톱 스레드 전용 3개 제외다. core 검사와 10000틱 물리
해시도 위 결과와 일치한다. 로그는 `.generated/webgl-bot-prefab-contract.log`다.

`RequiresThreads`는 동시 제출과 스레드 사이 snapshot·봇 인스턴스 공유 검사에만 붙는다.
WebGL에서 제외된 항목은 `PARITY SKIP`으로 남고 통과 수에 포함되지 않는다.
큐의 지연 실행과 봇 재진입 검사는 WebGL의 동기 실행 방식으로도 수행한다.
실제 lockstep 봇은 `SynchronousBotThinkExecutor`를 기본으로 사용한다.

`Run-WebGLProbe.cjs`는 Playwright가 Node의 모듈 경로에 있을 때 사용할 수 있다.
빌드 폴더를 로컬 HTTP로 제공한 뒤, 별도 headless 브라우저에서 실행하고
콘솔 전체를 저장한다. 오류·페이지 crash·5분 제한·원본 테스트 요약 누락은
실패로 처리한다. 이미 설치된 Chromium 계열 브라우저 경로를 지정한다.

```powershell
node Tools/LockstepProbe/Run-WebGLProbe.cjs http://127.0.0.1:18764 $browserPath Tools/LockstepProbe/.generated/webgl-parity.log
```

이 probe는 실제 시뮬레이션 코드를 실행하지만 GameScene의 렌더링·사용자 입력·
네트워크를 통합한 화면 검증을 대신하지 않는다.

## 실제 Unity PVE 장면 검사

`LockstepPlayableSceneTests`는 실제 GameScene에서 PVE를 진행하고 ResultScene의
승리 표시까지 확인한다. 일회용 PostgreSQL과 Rust `lockstep_client_network` 검증
서버가 필요하다. 기존 network probe 환경 변수에 `AC_LOCKSTEP_UNITY_CONFIG`를
추가하면 서버가 그 경로에 Unity용 설정을 쓰고 완료 보고를 기다린다.
설정에는 테스트 전용 JWT만 들어간다. 매 실행마다 새 설정 경로와 빈 DB를 사용한다.

Unity에도 같은 환경 변수를 전달하고 `-runTests -testPlatform EditMode
-testFilter LockstepPlayableSceneTests`로 실행한다. 캡처를 위해 `-nographics`는
사용하지 않는다. 설정이 없으면 일반 EditMode 검사에서는 이 테스트를 제외한다.
설정 옆의 `.png`는 전장 카메라 캡처이며 overlay UI 전체 캡처는 아니다.
`.result.json`은 Unity 완료 보고다. 서버는 보고 성공 이후 저장된 승리 결과,
PVE FINISHED, lobby 종료 통지 1회를 추가 검증한다. Unity XML과 서버 결과가
모두 통과해야 성공이다. 실제 lobby 매칭 화면부터 시작하는 검사는 아니다.

서버 실행 시 `AC_LOCKSTEP_UNITY_MODE=bot`를 지정하면 규칙 봇 Practice 경기를
검증한다. 봇이 lightning_explosion을 시전해 인간 플레이어를 쓰러뜨리고
ResultScene의 Lose 제목이 표시되는지 검사한다. 기본값 `pve`는 Survive PVE의
Win 제목과 FINISHED 진행 상태를 검사한다. 두 모드 모두 테스트용 짧은 경기와
빈 인간 손패를 사용하므로 카드 조작이나 로비 매칭 UI 검증을 대신하지 않는다.

`AC_LOCKSTEP_UNITY_MODE=cast`는 Unity와 별도 .NET 상대 프로세스의 PVP 검사다.
Unity가 경기를 만든 뒤 `.ready` 파일을 쓰면 서버 fixture가 상대 클라이언트를
연결한다. 테스트는 로비가 준비하는 마법 정보 캐시를 fixture 값으로 채우고,
실제 CardUI.OnCardClicked와 CardInputSender.TrySendInput으로 카드를 사용한다.
Unity의 시전 성공 응답 1회와 Win 제목, 상대의 acceptedCasts 1회와 RightPlayer
패배, DB 저장 결과를 검사한다. 마우스 좌표 클릭이나 실제 로비 API 검사는 아니다.
서버의 `AC_LOCKSTEP_UNITY_MAP=RIVER`로 River 전장을 검사할 수 있다. Unity는 `/api/server/lockstep-matches` 준비 응답의 mapType과 lockstepSimulationVersion을 읽어 SceneContext를 설정한다.
