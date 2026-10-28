# 전투 결과 재생 검증기

Unity와 테스트 probe 없이 같은 고정 소수점 시뮬레이션 소스를 .NET 8 실행 파일로 빌드한다.

```powershell
dotnet build Tools/LockstepVerifier/LockstepVerifier.csproj -c Release
```

표준 입력으로 서버가 보관한 `{ "start": SessionStart, "frames": ConfirmedFrame[] }` JSON을 받는다. 입력은 3 MiB, 12,000 프레임까지 허용한다. 초기 콘텐츠 해시와 버전, 참가자, 세션, 연속 입력 순서를 확인한 뒤 처음부터 재생한다. 종료 유예 프레임까지 정확히 포함해야 한다. 미완료, 종료 이후 프레임, 실행 중 격리된 시스템 오류도 거부한다.

성공하면 표준 출력에 스키마 버전, 세션 ID, 시뮬레이션 버전, 초기 해시, 프레임 수, 패자와 양측 HP, 성공한 마법 사용 집계(`magics`: `userId`, `magicId`, `count`)를 담은 JSON 한 줄을 기록하고 0으로 종료한다. 마법 집계는 사람과 봇 모두 공통 `MagicUsed` 이벤트에서 수집하며 실패한 시전은 포함하지 않는다. 화면용 이벤트는 매 프레임 비워 재생 내내 누적하지 않는다. 실패하면 표준 출력 없이 1로 종료한다. `AC_LOCKSTEP_VERIFIER_DIAGNOSTICS=1`은 표준 오류에 예외 타입과 스택을 추가한다.

이 실행 파일 자체는 입력의 출처를 인증하거나 결과를 저장하지 않는다. 호출 서버가 자기 세션의 초기 상태와 확정 로그를 전달하고 프로세스 시간·동시 실행 수를 제한한다. 클라이언트가 제출한 로그로 보상을 승인하면 안 된다.

서버 실행 시 `LOCKSTEP_VERIFIER_DLL`에 빌드된 DLL의 절대 경로를 지정한다. 참가자의 `POST /api/lockstep/{id}/complete` 요청은 서버가 보관한 입력을 재생하고 통계·MMR·보상·완료 응답을 같은 transaction으로 저장한다. 동시 요청은 같은 응답을 받는다. Unity는 이 응답의 승패를 로컬 결과와 대조하고 기존 `ResultHandler`로 전달한다.

`game-rust`의 `lockstep_client_network` 통합 테스트는 `AC_LOCKSTEP_VERIFIER_DLL`에 빌드된 DLL을 지정한다. `AC_LOCKSTEP_RESULT_MIGRATION`에는 database worktree의 `V120_20261027__add_lockstep_completed_results.sql` 경로를 지정한다. 실제 네트워크 경기의 서버 입력 이력을 별도 프로세스에서 재생하고 플레이어 프로세스 결과와 비교한다. 완료 HTTP 요청의 참가자 제한, 미완료 거부, 동시 재시도, 결과·보상 저장과 lobby 종료 통지도 일회용 DB와 로컬 HTTP 서버에서 검사한다. 서버 재시작 후 결과 복구와 렌더링된 GameScene 전체 검증은 아직 남아 있다.
