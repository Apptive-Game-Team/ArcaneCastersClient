// 전송 코드만 .NET에서 검사한다. Unity 생명주기와 GameScene 검증을 대신하지 않는다.
namespace UnityEngine { public class MonoBehaviour { } }
public static class WDebug {
    public static void Log(string message) { }
    public static void LogError(string message) { }
}
