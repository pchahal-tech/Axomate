using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Interfaces.Services
{
    public interface IServiceItemService
    {
        Task<IEnumerable<ServiceItem>> GetAllAsync();
        Task<ServiceItem?> GetByIdAsync(int id);
        Task<int> AddAsync(ServiceItem item);
        Task<ServiceItem> UpdateAsync(ServiceItem item);
        Task DeleteAsync(int id);
        Task<IEnumerable<ServiceItem>> SearchByNameAsync(string name);
        Task<ServiceItem> AddOrUpdateAsync(string name, decimal? price = null);
        Task<ServiceItem> AddOrUpdateAsync(ServiceItem item);
    }

}
