using System;
using System.Linq;
using System.Threading.Tasks;
using Axomate.ApplicationLayer.Interfaces.Services;
using Axomate.ApplicationLayer.Services.Auth;
using Axomate.Domain.Models;
using Microsoft.EntityFrameworkCore;


namespace Axomate.Infrastructure.Database.Services
{ 
    public class AuthService : IAuthService
    {
        private readonly AxomateDbContext _db;

        public AuthService(AxomateDbContext db) => _db = db;

        public async Task EnsureInitializedAsync()
        {
            if (!await _db.AdminCredentials.AnyAsync())
            {
                // First-run default; user should change it ASAP
                const string defaultPassword = "admin123";
                var (hash, salt, iter) = PasswordHasher.Hash(defaultPassword);
                _db.AdminCredentials.Add(new AdminCredential
                {
                    Username = "admin",
                    PasswordHash = hash,
                    Salt = salt,
                    Iterations = iter,
                    CreatedAtUtc = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
        }

        public async Task<bool> VerifyAdminPasswordAsync(string password)
        {
            var cred = await _db.AdminCredentials.AsNoTracking().FirstOrDefaultAsync(x => x.Username == "admin");
            if (cred == null) return false;

            return PasswordHasher.Verify(password, cred.PasswordHash, cred.Salt, cred.Iterations);
        }

        public async Task SetAdminPasswordAsync(string newPassword)
        {
            var cred = await _db.AdminCredentials.FirstOrDefaultAsync(x => x.Username == "admin");
            if (cred == null)
            {
                cred = new AdminCredential { Username = "admin" };
                _db.AdminCredentials.Add(cred);
            }

            var (hash, salt, iter) = PasswordHasher.Hash(newPassword);
            cred.PasswordHash = hash;
            cred.Salt = salt;
            cred.Iterations = iter;
            cred.ChangedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }
    }
}
