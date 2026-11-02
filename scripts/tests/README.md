# STOMP 전송 회귀 테스트

Node.js와 .NET 8 SDK로 실행한다. Unity Editor와 외부 서버는 필요하지 않다.

```sh
node --test scripts/tests/stomp-plugin.test.cjs
dotnet run --project scripts/tests/native-stomp/native-stomp.csproj
```

JS 테스트는 WebGL template과 같은 stomp.js 2.3.3을 가짜 WebSocket 위에서 실행해
이전 연결의 callback, 현재 구독, 서버 종료와 의도한 종료를 확인한다.
`fixtures/stomp-2.3.3.js`는 npm `stompjs@2.3.3/lib/stomp.js`의 원본이며
파일 상단의 Apache 2.0 고지를 유지한다.

Native 테스트는 실제 `NativeStompTransport.cs`를 최소 `MonoBehaviour` stub과 함께
컴파일한다. 응답을 메인스레드 큐에 넣은 뒤 연결을 교체해, 이전 응답과 종료가 새 연결의
상태와 CTS를 바꾸지 않는지 확인한다. 실제 WebSocket 통신과 Unity 씬 전환은 별도 검증이
필요하다.
