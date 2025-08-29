using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Interfaces.Repositories
{
    public interface IServiceItemRepository
    {
        Task<List<ServiceItem>> GetAllAsync();
        Task<ServiceItem?> GetByIdAsync(int id);
        Task<int> AddAsync(ServiceItem serviceItem);
        Task<ServiceItem> UpdateAsync(ServiceItem serviceItem);
        Task DeleteAsync(int id);

        // Optional: Search helper
        Task<List<ServiceItem>> SearchByNameAsync(string name);
    }
}
