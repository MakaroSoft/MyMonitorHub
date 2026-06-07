using System;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace MyMonitorHub.Domain.Util
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;             // 128 bits
        private const int HashSize = 32;             // 256 bits
        private const int Iterations = 4;
        private const int MemorySize = 65536;        // 64 MB
        private const int DegreeOfParallelism = 2;

        /// <summary>
        /// Returns a self-describing PHC string:
        ///   $argon2id$v=19$m=65536,t=4,p=2$&lt;Base64-salt&gt;$&lt;Base64-hash&gt;
        /// All parameters are embedded in the string so Verify can always
        /// reconstruct the hash even if the defaults are changed later.
        /// </summary>
        public static string Hash(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = ComputeHash(password, salt, Iterations, MemorySize, DegreeOfParallelism);

            return $"$argon2id$v=19$m={MemorySize},t={Iterations},p={DegreeOfParallelism}" +
                   $"${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Parses the PHC string to extract the original parameters and salt,
        /// re-hashes the candidate password, and does a timing-safe comparison.
        /// </summary>
        public static bool Verify(string stored, string password)
        {
            if (string.IsNullOrEmpty(stored) || password == null) return false;

            // Expected parts after splitting on '$':
            //   [0] ""  [1] "argon2id"  [2] "v=19"  [3] "m=...,t=...,p=..."  [4] salt  [5] hash
            var parts = stored.Split('$');
            if (parts.Length != 6 || parts[1] != "argon2id") return false;

            var paramParts = parts[3].Split(',');
            if (paramParts.Length != 3) return false;

            if (!TryParseParam(paramParts[0], "m", out int memory)) return false;
            if (!TryParseParam(paramParts[1], "t", out int iterations)) return false;
            if (!TryParseParam(paramParts[2], "p", out int parallelism)) return false;

            byte[] salt       = Convert.FromBase64String(parts[4]);
            byte[] storedHash = Convert.FromBase64String(parts[5]);
            byte[] testHash   = ComputeHash(password, salt, iterations, memory, parallelism);

            return CryptographicOperations.FixedTimeEquals(testHash, storedHash);
        }

        private static byte[] ComputeHash(string password, byte[] salt, int iterations, int memory, int parallelism)
        {
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));
            argon2.Salt = salt;
            argon2.Iterations = iterations;
            argon2.MemorySize = memory;
            argon2.DegreeOfParallelism = parallelism;
            return argon2.GetBytes(HashSize);
        }

        private static bool TryParseParam(string segment, string key, out int value)
        {
            value = 0;
            var kv = segment.Split('=');
            return kv.Length == 2 && kv[0] == key && int.TryParse(kv[1], out value);
        }
    }
}
