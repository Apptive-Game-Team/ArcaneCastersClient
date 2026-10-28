using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using Global.Stomp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

var sockets = new ConcurrentQueue<WebSocket>();
var incoming = new ConcurrentQueue<string>();
var builder = WebApplication.CreateBuilder();
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
await using var server = builder.Build();
server.UseWebSockets();
server.Map("/ws", async context => {
    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    sockets.Enqueue(socket);
    var buffer = new byte[32768];
    try {
        while (socket.State == WebSocketState.Open) {
            using var data = new MemoryStream();
            WebSocketReceiveResult result;
            do {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
                if (result.MessageType == WebSocketMessageType.Close) {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None); return;
                }
                data.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);
            var frame = Encoding.UTF8.GetString(data.ToArray()); incoming.Enqueue(frame);
            if (frame.StartsWith("CONNECT\n", StringComparison.Ordinal))
                await Send(socket, "CONNECTED\nversion:1.2\n\n\0");
        }
    } catch (WebSocketException) { } catch (OperationCanceledException) { }
});
await server.StartAsync();
var url = server.Urls.Single().Replace("http:", "ws:") + "/ws";
var transport = new NativeStompTransport();
var update = typeof(NativeStompTransport).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)!;
var received = new List<string>();
int connections = 0, rejected = 0;
transport.Connected += () => connections++;
transport.Rejected += _ => rejected++;
transport.MessageReceived += (id, body) => { if (id != "lockstep") throw new Exception("Subscription mismatch"); received.Add(body); };
try {
    transport.Connect(url, "fixture");
    await Until(() => connections == 1);
    Check(sockets.TryDequeue(out var first), "First socket");
    transport.Subscribe("/game/lockstep/test", "lockstep");
    await Until(() => incoming.Any(frame => frame.StartsWith("SUBSCRIBE\n", StringComparison.Ordinal)));

    // 멀티바이트 UTF-8 문자가 WebSocket fragment 경계에 걸린다.
    byte[] split = Encoding.UTF8.GetBytes("\nMESSAGE\nsubscription:lockstep\n\n전투\0");
    int cut = Array.IndexOf(split, (byte)0xEC) + 1;
    await first!.SendAsync(new ArraySegment<byte>(split, 0, cut), WebSocketMessageType.Text, false, CancellationToken.None);
    await first.SendAsync(new ArraySegment<byte>(split, cut, split.Length - cut), WebSocketMessageType.Text, true, CancellationToken.None);
    await Until(() => received.Count == 1);
    Check(received[0] == "전투", "Fragmented UTF-8 preserved");
    await Send(first, "MESSAGE\nsubscription:lockstep\n\npar");
    await Send(first, "tial\0MESSAGE\nsubscription:lockstep\n\nnext\0");
    await Until(() => received.Count == 3);
    Check(received[1] == "partial" && received[2] == "next", "STOMP fragments survive WebSocket message boundaries");

    await Task.WhenAll(Enumerable.Range(0, 100).Select(i => Task.Run(() => transport.Send("/app/lockstep/test/input", i.ToString()))));
    await Until(() => incoming.Count(frame => frame.StartsWith("SEND\n", StringComparison.Ordinal)) == 100);

    // 메인 스레드에 대기 중인 옛 소켓 이벤트도 연결 교체 뒤에는 전달하지 않는다.
    var parse = typeof(NativeStompTransport).GetMethod("ProcessIncomingData", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var oldSocket = typeof(NativeStompTransport).GetField("_ws", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(transport);
    parse.Invoke(transport, new[] { "ERROR\n\nstale\0", oldSocket });
    transport.Connect(url, "fixture");
    await Until(() => connections == 2);
    Check(rejected == 0 && transport.IsConnected, "Stale rejection cannot terminate new socket");
    Check(sockets.TryDequeue(out var second), "Second socket");
    await Send(second!, "ERROR\n\ninvalid input\0");
    await Until(() => rejected == 1);
    Check(!transport.IsConnected, "Protocol rejection closes logical connection");
    transport.Disconnect();
    Console.WriteLine("PASS native transport: handshake, UTF-8 fragments, split STOMP, serialized sends, stale callbacks, rejection");
} finally {
    typeof(NativeStompTransport).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(transport, null);
    await server.StopAsync();
}

async Task Until(Func<bool> condition) {
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    while (!condition()) { update.Invoke(transport, null); await Task.Delay(5, timeout.Token); }
}
static Task Send(WebSocket socket, string frame) => socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(frame)), WebSocketMessageType.Text, true, CancellationToken.None);
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
