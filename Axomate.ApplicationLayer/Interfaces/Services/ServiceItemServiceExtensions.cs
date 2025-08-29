using System;
using System.Linq;
using System.Threading.Tasks;
using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Interfaces.Services
{
    public static class ServiceItemServiceExtensions
    {
        public static async Task<ServiceItem> AddOrUpdateAsync(
            this IServiceItemService service, string name, decimal? price = null)
        {
            if (service is null) throw new ArgumentNullException(nameof(service));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));

            name = name.Trim();

            var matches = await service.SearchByNameAsync(name);
            var existing = matches
                .Where(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.Id)
                .FirstOrDefault();

            if (existing is null)
            {
                var created = new ServiceItem { Name = name, Price = price ?? 0m };
                var id = await service.AddAsync(created);
                created.Id = id;
                return created;
            }
            else
            {
                if (price.HasValue) existing.Price = price.Value;
                return await service.UpdateAsync(existing);
            }
        }

        public static Task<ServiceItem> AddOrUpdateAsync(
            this IServiceItemService service, ServiceItem item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));
            return service.AddOrUpdateAsync(item.Name, item.Price);
        }
    }
}
