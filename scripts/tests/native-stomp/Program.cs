using System;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Global.Stomp;

// 네트워크 응답과 메인스레드 큐 실행 사이에 재연결이 끼는 순서를 직접 재현한다.
var transport = new NativeStompTransport();
int connected = 0, disconnected = 0, errors = 0, messages = 0;
transport.Connected += () => connected++;
transport.Disconnected += _ => disconnected++;
transport.Errored += _ => errors++;
transport.MessageReceived += (_, body) => { Check(body == "current", "message body"); messages++; };

using var oldSocket = new ClientWebSocket();
using var newSocket = new ClientWebSocket();
Set("_generation", 1);
Set("_ws", oldSocket);
Call("ParseAndDispatch", "CONNECTED\n\n", oldSocket, 1);
Call("ParseAndDispatch", "ERROR\n\nstale", oldSocket, 1);
Call("ParseAndDispatch", "MESSAGE\nsubscription:frame-sub\n\nstale", oldSocket, 1);
var oldReceive = (Task)Call("ReceiveLoop", oldSocket, CancellationToken.None, 1);
await oldReceive;

transport.Disconnect();
Check(!transport.IsConnected, "Disconnect must clear state synchronously");
Set("_generation", 3);
Set("_ws", newSocket);
using var currentCancellation = new CancellationTokenSource();
Set("_cts", currentCancellation);
Call("ParseAndDispatch", "CONNECTED\n\n", newSocket, 3);
Call("Update");
Check(connected == 1 && transport.IsConnected, "stale queued callbacks must not mutate the new connection");
Check(disconnected == 0 && errors == 0 && messages == 0, "stale events must not escape");

await (Task)Call("ReceiveLoop", oldSocket, CancellationToken.None, 1);
Call("Update");
Check(transport.IsConnected && !currentCancellation.IsCancellationRequested, "old receive exit must preserve current state and CTS");
Call("ParseAndDispatch", "MESSAGE\nsubscription:frame-sub\n\ncurrent", newSocket, 3);
Call("Update");
Check(messages == 1, "current message must be delivered");

// 현재 소켓이 Open이 아닌 상태로 수신 루프를 빠지는 경로도 종료를 통보해야 한다.
await (Task)Call("ReceiveLoop", newSocket, CancellationToken.None, 3);
Call("Update");
Check(disconnected == 1 && !transport.IsConnected, "current unexpected close must be reported");
Call("ParseAndDispatch", "CONNECTED\n\n", newSocket, 3);
transport.Disconnect();
Call("Update");
Check(connected == 1 && disconnected == 1 && !transport.IsConnected, "intentional shutdown must discard pending callbacks and stay silent");
Console.WriteLine("Native STOMP generation, dispatch, close and teardown checks passed.");

void Set(string name, object value) => typeof(NativeStompTransport)
    .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(transport, value);
object Call(string name, params object[] arguments) => typeof(NativeStompTransport)
    .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(transport, arguments);
void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

namespace UnityEngine { public class MonoBehaviour { } }
