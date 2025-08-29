using Axomate.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Axomate.ApplicationLayer.Interfaces.Repositories;

namespace Axomate.Infrastructure.Database.Seeders
{
    public class ServiceItemSeeder
    {
        private readonly IServiceItemRepository _repository;
        private readonly AxomateDbContext _dbContext;

        public ServiceItemSeeder(IServiceItemRepository repository, AxomateDbContext dbContext)
        {
            _repository = repository;
            _dbContext = dbContext;
        }

        public async Task SeedAsync()
        {
            if (!await _dbContext.ServiceItems.AnyAsync())
            {
                var items = new List<ServiceItem>
                {
                    new() { Name = "Tire Changeover", Price = 80 },
                    new() { Name = "Tire Changeover", Price = 100 },
                    new() { Name = "Tire Changeover", Price = 120 },
                    new() { Name = "Tire Changeover (Manual Entry)", Price = 0 },

                    new() { Name = "Tire Swap", Price = 40 },

                    new() { Name = "Wheel Alignment", Price = 100 },
                    new() { Name = "Wheel Alignment", Price = 150 },
                    new() { Name = "Wheel Balancing", Price = 60 },

                    new() { Name = "Brake Pads & Rotors (Manual Entry)", Price = 0 },
                    new() { Name = "Brake Pads (Manual Entry)", Price = 0 },
                    new() { Name = "Brake Rotors (Manual Entry)", Price = 0 },

                    new() { Name = "Brake Installation", Price = 80 },
                    new() { Name = "Brake Installation", Price = 160 },

                    new() { Name = "Flat Tire Repair", Price = 30 },
                    new() { Name = "Flat Tire Patch", Price = 45 },

                    new() { Name = "Oil Change + Filter", Price = 80 },
                    new() { Name = "Oil Change + Filter", Price = 100 },
                    new() { Name = "Oil Change + Filter (Manual Entry)", Price = 0 },

                    new() { Name = "Used Tire Sale (Manual Entry)", Price = 0 },
                    new() { Name = "Shop Supplies (Manual Entry)", Price = 0 },
                    new() { Name = "Labour (Manual Entry)", Price = 0 },
                    new() { Name = "Mobile Charges (Manual Entry)", Price = 0 },

                    new() { Name = "Discount (as Line Item)", Price = 0 },
                    new() { Name = "Battery (Manual Entry)", Price = 0 },

                    new() { Name = "Other", Price = 0 }
                };

                foreach (var item in items)
                {
                    await _repository.AddAsync(item);
                }

                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
