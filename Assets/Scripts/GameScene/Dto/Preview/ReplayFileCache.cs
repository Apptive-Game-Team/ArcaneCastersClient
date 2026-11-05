using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Data.Preview
{
    /// <summary>Content-addressed files are usable only after today's server catalog validates their hash.</summary>
    public sealed class ReplayFileCache
    {
        public const int MaxBytes = 4 * 1024 * 1024;
        private readonly string directory;

        public ReplayFileCache(string root, string source)
        {
            directory = Path.Combine(root, "magic-previews", Hash(Encoding.UTF8.GetBytes(source)));
        }

        public static bool Valid(PreviewEntry entry) => entry != null && entry.available &&
            entry.bytes > 0 && entry.bytes <= MaxBytes &&
            Regex.IsMatch(entry.name ?? "", "^[a-z0-9_]{1,100}$") && ValidHash(entry.hash);

        public static bool ValidHash(string hash) => Regex.IsMatch(hash ?? "", "^[a-f0-9]{64}$");

        public static string Hash(byte[] bytes)
        {
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        public static bool Verify(PreviewEntry entry, byte[] bytes) => Valid(entry) && bytes != null &&
            bytes.Length == entry.bytes && Hash(bytes) == entry.hash;

        public bool TryLoad(PreviewEntry entry, out byte[] bytes)
        {
            bytes = null;
            if (!Valid(entry)) return false;
            try {
                string path = Path.Combine(directory, entry.hash + ".json");
                if (!File.Exists(path) || new FileInfo(path).Length != entry.bytes) return false;
                byte[] loaded = File.ReadAllBytes(path);
                if (!Verify(entry, loaded)) return false;
                bytes = loaded;
                return true;
            } catch (Exception e) when (StorageFailure(e)) { return false; }
        }

        public bool TrySave(PreviewEntry entry, byte[] bytes)
        {
            if (!Verify(entry, bytes)) return false;
            try {
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, entry.hash + ".json");
                string temporary = path + ".tmp";
                File.WriteAllBytes(temporary, bytes);
                if (File.Exists(path)) File.Delete(path);
                File.Move(temporary, path);
                return true;
            } catch (Exception e) when (StorageFailure(e)) { return false; }
        }

        public void Prune(System.Collections.Generic.IEnumerable<string> hashes)
        {
            var retained = new System.Collections.Generic.HashSet<string>(hashes);
            try {
                if (!Directory.Exists(directory)) return;
                foreach (string path in Directory.EnumerateFiles(directory, "*.json"))
                    if (ValidHash(Path.GetFileNameWithoutExtension(path)) &&
                        !retained.Contains(Path.GetFileNameWithoutExtension(path))) File.Delete(path);
            } catch (Exception e) when (StorageFailure(e)) { }
        }

        private static bool StorageFailure(Exception e) => e is IOException || e is UnauthorizedAccessException ||
            e is NotSupportedException || e is System.Security.SecurityException;
    }
}
