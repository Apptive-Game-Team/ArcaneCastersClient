using System.IO;

namespace LocalServer
{
    /// <summary>ArcaneCastersGameRust의 local-server 배포 묶음 규격.</summary>
    public static class LocalServerBundle
    {
        // Rust local launcher가 Java와 같은 profile 인자를 받는다. JVM 인자는 넘기지 않는다.
        public const string LaunchArguments = "--spring.profiles.active=local";

        public static string ExecutableName(bool windows) => windows ? "ac-game.exe" : "ac-game";

        /// <summary>직접 풀린 묶음과 local-server/ 아래 묶음을 지원한다. Java 묶음은 선택하지 않는다.</summary>
        public static string ResolveRoot(string directory, bool windows)
        {
            string executable = ExecutableName(windows);
            if (File.Exists(Path.Combine(directory, executable))) return directory;
            string nested = Path.Combine(directory, "local-server");
            return File.Exists(Path.Combine(nested, executable)) ? nested : null;
        }
    }
}
