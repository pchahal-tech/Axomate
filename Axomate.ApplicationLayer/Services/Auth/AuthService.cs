using System;
using System.Threading.Tasks;
using Axomate.ApplicationLayer.Interfaces.Repositories;
using Axomate.ApplicationLayer.Interfaces.Services;
using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IAdminCredentialRepository _repo;
        public AuthService(IAdminCredentialRepository repo) => _repo = repo;

        public async Task EnsureInitializedAsync()
        {
            var cred = await _repo.GetByUsernameAsync("admin");
            if (cred != null) return;

            const string defaultPassword = "admin123";
            var (hash, salt, iter) = PasswordHasher.Hash(defaultPassword);
            cred = new AdminCredential
            {
                Username = "admin",
                PasswordHash = hash,
                Salt = salt,
                Iterations = iter,
                CreatedAtUtc = DateTime.UtcNow
            };
            await _repo.AddAsync(cred);
        }

        public async Task<bool> VerifyAdminPasswordAsync(string password)
        {
            var cred = await _repo.GetByUsernameAsync("admin");
            if (cred == null) return false;
            return PasswordHasher.Verify(password, cred.PasswordHash, cred.Salt, cred.Iterations);
        }

        public async Task SetAdminPasswordAsync(string newPassword)
        {
            var cred = await _repo.GetByUsernameAsync("admin", asTracking: true)
                       ?? new AdminCredential { Username = "admin", CreatedAtUtc = DateTime.UtcNow };

            var (hash, salt, iter) = PasswordHasher.Hash(newPassword);
            cred.PasswordHash = hash;
            cred.Salt = salt;
            cred.Iterations = iter;
            cred.ChangedAtUtc = DateTime.UtcNow;

            if (cred.Id == 0) await _repo.AddAsync(cred);
            else await _repo.UpdateAsync(cred);
        }
    }
}
