using System;
using System.Security.Cryptography;
using System.Text;

namespace MyMonitorHub.Domain.Util
{
    public static class Base32
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static string Encode(byte[] data)
        {
            if (data == null || data.Length == 0) return string.Empty;
            var output = new StringBuilder((data.Length + 7) * 8 / 5);
            int buffer = data[0];
            int next = 1;
            int bitsLeft = 8;
            while (bitsLeft > 0 || next < data.Length)
            {
                if (bitsLeft < 5)
                {
                    if (next < data.Length)
                    {
                        buffer <<= 8;
                        buffer |= data[next++] & 0xff;
                        bitsLeft += 8;
                    }
                    else
                    {
                        int pad = 5 - bitsLeft;
                        buffer <<= pad;
                        bitsLeft += pad;
                    }
                }
                int index = 0x1f & (buffer >> (bitsLeft - 5));
                bitsLeft -= 5;
                output.Append(Alphabet[index]);
            }
            return output.ToString();
        }

        public static byte[] Decode(string base32)
        {
            if (string.IsNullOrWhiteSpace(base32)) return new byte[0];
            string clean = base32.Trim().Replace(" ", string.Empty).ToUpperInvariant();
            int buffer = 0;
            int bitsLeft = 0;
            var output = new System.IO.MemoryStream();
            foreach (char c in clean)
            {
                int val = Alphabet.IndexOf(c);
                if (val < 0) continue; // ignore non-base32 chars
                buffer <<= 5;
                buffer |= val & 0x1f;
                bitsLeft += 5;
                if (bitsLeft >= 8)
                {
                    output.WriteByte((byte)((buffer >> (bitsLeft - 8)) & 0xff));
                    bitsLeft -= 8;
                }
            }
            return output.ToArray();
        }
    }

    public static class Totp
    {
        public static string GenerateCode(byte[] secret, DateTime utcNow, int digits = 6, int periodSeconds = 30)
        {
            if (secret == null || secret.Length == 0) throw new ArgumentException("secret");
            long counter = (long)Math.Floor((utcNow - new DateTime(1970, 1, 1)).TotalSeconds / periodSeconds);
            var counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(counterBytes);
            }
            using (var hmac = new HMACSHA1(secret))
            {
                var hash = hmac.ComputeHash(counterBytes);
                int offset = hash[hash.Length - 1] & 0x0f;
                int binary = ((hash[offset] & 0x7f) << 24)
                             | ((hash[offset + 1] & 0xff) << 16)
                             | ((hash[offset + 2] & 0xff) << 8)
                             | (hash[offset + 3] & 0xff);
                int otp = binary % (int)Math.Pow(10, digits);
                return otp.ToString(new string('0', digits));
            }
        }

        public static bool VerifyCode(string base32Secret, string code, DateTime utcNow, int allowedDriftSteps = 1)
        {
            if (string.IsNullOrWhiteSpace(base32Secret)) return false;
            if (string.IsNullOrWhiteSpace(code)) return false;
            code = code.Trim();
            var secret = Base32.Decode(base32Secret);
            for (int step = -allowedDriftSteps; step <= allowedDriftSteps; step++)
            {
                var candidate = GenerateCode(secret, utcNow.AddSeconds(step * 30));
                if (SlowEquals(candidate, code)) return true;
            }
            return false;
        }

        private static bool SlowEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            int diff = a.Length ^ b.Length;
            int len = Math.Min(a.Length, b.Length);
            for (int i = 0; i < len; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}


