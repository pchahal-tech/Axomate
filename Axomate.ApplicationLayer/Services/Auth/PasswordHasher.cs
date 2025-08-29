using System;
using System.Security.Cryptography;

namespace Axomate.ApplicationLayer.Services.Auth
{
    public static class PasswordHasher   // <- was internal
    {
        public static (string HashBase64, byte[] Salt, int Iterations) Hash(
            string password, int iterations = 60_000, int saltSize = 16, int keySize = 32)
        {
            var salt = RandomNumberGenerator.GetBytes(saltSize);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(keySize);
            return (Convert.ToBase64String(key), salt, iterations);
        }

        public static bool Verify(string password, string storedHashBase64, byte[] salt, int iterations, int keySize = 32)
        {
            var stored = Convert.FromBase64String(storedHashBase64);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var computed = pbkdf2.GetBytes(keySize);
            return CryptographicOperations.FixedTimeEquals(stored, computed);
        }
    }
}
