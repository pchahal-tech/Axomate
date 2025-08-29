using System.Threading.Tasks;
using Axomate.ApplicationLayer.Interfaces.Repositories;
using Axomate.Domain.Models;
using Axomate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Axomate.Infrastructure.Database.Repositories
{
    public class AdminCredentialRepository : IAdminCredentialRepository
    {
        private readonly AxomateDbContext _db;
        public AdminCredentialRepository(AxomateDbContext db) => _db = db;

        public Task<AdminCredential?> GetByUsernameAsync(string username, bool asTracking = false)
        {
            var set = asTracking ? _db.AdminCredentials : _db.AdminCredentials.AsNoTracking();
            return set.FirstOrDefaultAsync(x => x.Username == username);
        }

        public async Task<int> AddAsync(AdminCredential credential)
        {
            _db.AdminCredentials.Add(credential);
            await _db.SaveChangesAsync();
            return credential.Id;
        }

        public async Task<AdminCredential> UpdateAsync(AdminCredential credential)
        {
            _db.AdminCredentials.Update(credential);
            await _db.SaveChangesAsync();
            return credential;
        }
    }
}
