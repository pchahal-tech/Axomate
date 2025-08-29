using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Interfaces.Services
{
    public interface ICustomerService
    {
        Task<List<Customer>> GetAllAsync();
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer> AddAsync(Customer customer);
        Task<Customer> UpdateAsync(Customer customer);
        Task<bool> ExistsAsync(string name, string phone);
        Task<Customer> AddOrUpdateAsync(Customer customer);
    }

}
