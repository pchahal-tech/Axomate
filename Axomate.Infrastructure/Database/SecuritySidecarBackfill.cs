// File: Axomate.Infrastructure/Database/SecuritySidecarBackfill.cs
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Axomate.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Axomate.Infrastructure.Database
{
    /// <summary>
    /// One-time/idempotent backfill:
    /// - Re-saves encrypted columns so legacy plaintext gets encrypted via the EF converter.
    /// - Populates shadow hash columns for duplicate checks and indexes.
    /// Safe to run at startup after migrations.
    /// </summary>
    public static class SecuritySidecarBackfill
    {
        public static async Task RunAsync(AxomateDbContext db)
        {
            // --- Customers ---
            var customers = await db.Customers.ToListAsync();
            foreach (var c in customers)
            {
                // Sidecar hashes
                db.Entry(c).Property("EmailHash").CurrentValue = HashOrNull(NormEmail(c.Email));
                db.Entry(c).Property("PhoneHash").CurrentValue = HashOrNull(NormPhone(c.Phone));

                // Force EF to write through the converter so legacy plaintext gets encrypted
                db.Entry(c).Property(nameof(c.Email)).IsModified = true;
                db.Entry(c).Property(nameof(c.Phone)).IsModified = true;
            }

            // --- Vehicles ---
            var vehicles = await db.Vehicles.ToListAsync();
            foreach (var v in vehicles)
            {
                db.Entry(v).Property("LicensePlateHash").CurrentValue = HashOrNull(NormUpper(v.LicensePlate));
                db.Entry(v).Property("VinHash").CurrentValue = HashOrNull(NormUpper(v.VIN));

                // Force re-save through converter
                db.Entry(v).Property(nameof(v.LicensePlate)).IsModified = true;
                db.Entry(v).Property(nameof(v.VIN)).IsModified = true;
            }

            await db.SaveChangesAsync();
        }

        // --- helpers (must mirror AxomateDbContext normalization) ---
        private static string? NormEmail(string? email)
            => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToUpperInvariant();

        private static string? NormPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            return string.IsNullOrWhiteSpace(digits) ? null : digits;
        }

        private static string? NormUpper(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

        private static string? HashOrNull(string? normalized)
        {
            if (string.IsNullOrWhiteSpace(normalized)) return null;
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString(); // 64 hex chars
        }
    }
}
