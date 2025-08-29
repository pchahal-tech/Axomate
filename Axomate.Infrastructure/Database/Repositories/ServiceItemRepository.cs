using Axomate.ApplicationLayer.Interfaces.Repositories;
using Axomate.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Axomate.Infrastructure.Database.Repositories
{
    public class ServiceItemRepository : IServiceItemRepository
    {
        private readonly AxomateDbContext _db;
        public ServiceItemRepository(AxomateDbContext db) => _db = db;

        // Read: for dropdowns (alphabetical), no tracking
        public async Task<List<ServiceItem>> GetAllAsync()
        {
            return await _db.ServiceItems
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        // Read: single (tracked)
        public async Task<ServiceItem?> GetByIdAsync(int id)
        {
            return await _db.ServiceItems.FindAsync(id);
        }

        // Create
        public async Task<int> AddAsync(ServiceItem serviceItem)
        {
            if (serviceItem is null) throw new ArgumentNullException(nameof(serviceItem));

            _db.ServiceItems.Add(serviceItem);
            await _db.SaveChangesAsync();
            return serviceItem.Id;
        }

        // Update (returns updated entity)
        public async Task<ServiceItem> UpdateAsync(ServiceItem serviceItem)
        {
            if (serviceItem is null) throw new ArgumentNullException(nameof(serviceItem));
            if (serviceItem.Id <= 0) throw new ArgumentException("ServiceItem must have a valid Id.", nameof(serviceItem));

            _db.ServiceItems.Update(serviceItem);
            await _db.SaveChangesAsync();
            return serviceItem;
        }

        // Delete
        public async Task DeleteAsync(int id)
        {
            var entity = await _db.ServiceItems.FindAsync(id);
            if (entity != null)
            {
                _db.ServiceItems.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }

        // Search helper: case-insensitive "contains" match, ordered by Name
        public async Task<List<ServiceItem>> SearchByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return await GetAllAsync();
            }

            var term = name.Trim();

            return await _db.ServiceItems
                .AsNoTracking()
                .Where(s => EF.Functions.Like(s.Name, $"%{term}%"))
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
    }
}
