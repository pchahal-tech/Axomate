using System.Threading.Tasks;
using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Interfaces.Repositories
{
    public interface IAdminCredentialRepository
    {
        Task<AdminCredential?> GetByUsernameAsync(string username, bool asTracking = false);
        Task<int> AddAsync(AdminCredential credential);
        Task<AdminCredential> UpdateAsync(AdminCredential credential);
    }
}
