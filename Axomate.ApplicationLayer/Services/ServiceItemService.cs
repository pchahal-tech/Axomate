using Axomate.ApplicationLayer.Interfaces.Repositories;
using Axomate.ApplicationLayer.Interfaces.Services;
using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Services
{
    public class ServiceItemService : IServiceItemService
    {
        private readonly IServiceItemRepository _repository;

        public ServiceItemService(IServiceItemRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ServiceItem>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<ServiceItem?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<int> AddAsync(ServiceItem item)
        {
            return await _repository.AddAsync(item);
        }

        public async Task<ServiceItem> UpdateAsync(ServiceItem item)
        {
            return await _repository.UpdateAsync(item);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task<IEnumerable<ServiceItem>> SearchByNameAsync(string name)
        {
            return await _repository.SearchByNameAsync(name);
        }

        public async Task<ServiceItem> AddOrUpdateAsync(string name, decimal? price = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
            name = name.Trim();

            var matches = await _repository.SearchByNameAsync(name);
            var existing = matches
                .Where(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.Id)
                .FirstOrDefault();

            if (existing is null)
            {
                var created = new ServiceItem { Name = name, Price = price ?? 0m };
                var id = await _repository.AddAsync(created);
                created.Id = id;
                return created;
            }
            else
            {
                if (price.HasValue) existing.Price = price.Value;
                return await _repository.UpdateAsync(existing);
            }
        }

        public Task<ServiceItem> AddOrUpdateAsync(ServiceItem item)
            => AddOrUpdateAsync(item?.Name ?? string.Empty, item?.Price);
    }
}
