// File: Axomate.Infrastructure/Database/Security/DpapiCrypt.cs
using System;
using System.Security.Cryptography;
using System.Text;

namespace Axomate.Infrastructure.Database.Security
{
    /// <summary>
    /// DPAPI helper with bulletproof tolerant decrypt:
    /// - Returns plaintext unchanged if not Windows or not valid Base64 or not our DPAPI blob.
    /// - Prevents FormatException/CryptographicException from bubbling up during reads.
    /// </summary>
    internal static class DpapiCrypt
    {
        public static string? Encrypt(string? plaintext)
        {
            if (string.IsNullOrWhiteSpace(plaintext)) return plaintext;
            if (!OperatingSystem.IsWindows()) return plaintext; // test/CI fallback

            try
            {
                var data = Encoding.UTF8.GetBytes(plaintext);
                var cipher = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(cipher);
            }
            catch
            {
                // If DPAPI fails for any reason, fall back to plaintext (better than crashing).
                return plaintext;
            }
        }

        public static string? Decrypt(string? ciphertext)
        {
            if (string.IsNullOrWhiteSpace(ciphertext)) return ciphertext;
            if (!OperatingSystem.IsWindows()) return ciphertext; // test/CI fallback

            try
            {
                // If not valid Base64, this throws and we return plaintext.
                var data = Convert.FromBase64String(ciphertext);

                // If Base64 but not our DPAPI blob / wrong profile, Unprotect throws and we return plaintext.
                var plain = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plain);
            }
            catch
            {
                // Handles FormatException (not Base64) and CryptographicException (not DPAPI / wrong user).
                return ciphertext;
            }
        }
    }
}
