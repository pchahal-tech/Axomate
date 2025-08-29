// File: Axomate.ApplicationLayer.Interfaces.Repositories/ICustomerRepository.cs
using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Interfaces.Repositories
{
    public interface ICustomerRepository
    {
        Task<List<Customer>> GetAllAsync();
        Task<Customer?> GetByIdAsync(int id);
        Task<int> AddAsync(Customer customer);
        Task<Customer> UpdateAsync(Customer customer);
        Task<bool> ExistsAsync(string name, string phone);
        Task<Customer> AddOrUpdateAsync(Customer customer);
    }

}
