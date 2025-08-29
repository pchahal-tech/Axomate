// File: Axomate.Infrastructure.Database.Repositories/CustomerRepository.cs
using Axomate.ApplicationLayer.Interfaces.Repositories;
using Axomate.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Axomate.Infrastructure.Database.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly AxomateDbContext _dbContext;

        public CustomerRepository(AxomateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _dbContext.Customers
                    .AsNoTracking()
                    .Include(c => c.Vehicles)
                    .ToListAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _dbContext.Customers
                .Include(c => c.Vehicles)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<int> AddAsync(Customer customer)
        {
            _dbContext.Customers.Add(customer);
            await _dbContext.SaveChangesAsync();
            return customer.Id; // EF populates after SaveChanges
        }

        public async Task<Customer> UpdateAsync(Customer customer)
        {
            var existing = await _dbContext.Customers
                .Include(c => c.Vehicles)
                .FirstOrDefaultAsync(c => c.Id == customer.Id);

            if (existing == null)
                throw new InvalidOperationException("Customer not found.");

            // Safe update
            existing.Name = customer.Name.Trim();
            existing.Phone = customer.Phone?.Trim();
            existing.AddressLine1 = customer.AddressLine1;
            existing.Email = customer.Email;

            await _dbContext.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> ExistsAsync(string name, string phone)
        {
            var n = (name ?? string.Empty).Trim().ToUpper();
            var p = (phone ?? string.Empty).Trim();

            return await _dbContext.Customers.AnyAsync(c =>
                    c.Name.ToUpper().Trim() == n &&
                    ((c.Phone ?? string.Empty).Trim() == p));
        }

        public async Task<Customer> AddOrUpdateAsync(Customer customer)
        {
            var n = (customer.Name ?? string.Empty).Trim().ToUpper();
            var p = (customer.Phone ?? string.Empty).Trim();
            var existing = await _dbContext.Customers.FirstOrDefaultAsync(c =>
            c.Name.ToUpper().Trim() == n &&
            ((c.Phone ?? string.Empty).Trim() == p));

            if (existing != null)
            {
                existing.AddressLine1 = customer.AddressLine1;
                existing.Email = customer.Email;
                await _dbContext.SaveChangesAsync();
                return existing;
            }

            _dbContext.Customers.Add(customer);
            await _dbContext.SaveChangesAsync();
            return customer;
        }
    }
}
