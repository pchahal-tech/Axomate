using System.Threading.Tasks;

namespace Axomate.ApplicationLayer.Interfaces.Services
{
    public interface IAuthService
    {
        Task EnsureInitializedAsync();           
        Task<bool> VerifyAdminPasswordAsync(string password);
        Task SetAdminPasswordAsync(string newPassword);
    }
}
