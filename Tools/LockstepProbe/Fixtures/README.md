# Java 파라미터 테스트 fixture

`ArcaneCastersGame` 커밋 `80eef7659367ccf2960fabdda89c4bddf094e134`의 `src/test/resources/sql/`에서 가져왔다. 복사 전에 두 파일의 작업 트리 변경이 없는 것을 확인했다. 운영 DB 값이 아니라 원본 테스트 입력이며, 파일 내용을 변경하지 않았다.

복사 시 SHA-256(원본 줄바꿈 포함):

- `component_parameters.sql`: `90A2ED86DF3D7E002ACA4747925478AF09477981377B0DD37BDFCD01B78D150F`
- `component_prefab_parameters.sql`: `7EBE1F4737B239A7A2934665EEE8ABCF03C36F9BB7523DB25F4695A03D56132A`

.NET probe에는 embedded resource로 포함하고, `Prepare-UnityProbe.ps1`은 같은 파일을 Unity의 `Resources/LockstepFixtures/*.txt`로 복사한다. `ParameterSqlFixture`는 이 테스트 SQL의 두 INSERT 형식만 해석하며 수치를 decimal에서 Q32.32로 바꾼다. 일반 SQL 실행기나 런타임 콘텐츠 로더가 아니다.

`ContextParityTests`는 두 파일 모두 읽고 파라미터를 검증한다. fixture가 없으면 실패하며, 외부 Java checkout이 없어도 검사를 생략하지 않는다.
