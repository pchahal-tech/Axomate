// Axomate.Infrastructure/Security/HashLookup.cs
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Axomate.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Axomate.Infrastructure.Security
{
    public static class HashLookup
    {
        public static IQueryable<Customer> WhereEmailEquals(this IQueryable<Customer> query, string email)
        {
            var hash = Sha256Hex(NormEmail(email));
            return query.Where(c => EF.Property<string>(c, "EmailHash") == hash);
        }

        public static IQueryable<Customer> WherePhoneEquals(this IQueryable<Customer> query, string phone)
        {
            var hash = Sha256Hex(NormPhone(phone));
            return query.Where(c => EF.Property<string>(c, "PhoneHash") == hash);
        }

        public static IQueryable<Vehicle> WherePlateEquals(this IQueryable<Vehicle> query, string plate)
        {
            var hash = Sha256Hex(NormUpper(plate));
            return query.Where(v => EF.Property<string>(v, "LicensePlateHash") == hash);
        }

        public static IQueryable<Vehicle> WhereVinEquals(this IQueryable<Vehicle> query, string vin)
        {
            var hash = Sha256Hex(NormUpper(vin));
            return query.Where(v => EF.Property<string>(v, "VinHash") == hash);
        }

        private static string NormEmail(string s) => s.Trim().ToUpperInvariant();
        private static string NormPhone(string s)
        {
            var digits = new string(s.Where(char.IsDigit).ToArray());
            return digits;
        }
        private static string NormUpper(string s) => s.Trim().ToUpperInvariant();

        private static string Sha256Hex(string s)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
