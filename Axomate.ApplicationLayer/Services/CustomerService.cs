using Axomate.ApplicationLayer.Interfaces.Repositories;
using Axomate.ApplicationLayer.Interfaces.Services;
using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _customerRepository.GetAllAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _customerRepository.GetByIdAsync(id);
        }

        public async Task<Customer> AddAsync(Customer customer)
        {
            if (await ExistsAsync(customer.Name, customer.Phone))
                throw new InvalidOperationException("Customer with the same name and phone already exists.");

            var id = await _customerRepository.AddAsync(customer);
            customer.Id = id;
            return customer;
        }

        public async Task<Customer> UpdateAsync(Customer customer)
        {
            var existing = await _customerRepository.GetByIdAsync(customer.Id);
            if (existing == null)
                throw new InvalidOperationException("Customer not found.");

            if (string.IsNullOrWhiteSpace(customer.Name) || string.IsNullOrWhiteSpace(customer.Phone))
                throw new InvalidOperationException("Customer must have a valid name and phone number.");

            // Prevent duplicate (delegated to repo)
            if (await _customerRepository.ExistsAsync(customer.Name, customer.Phone) &&
                !string.Equals(existing.Phone, customer.Phone, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Another customer with the same name and phone already exists.");
            }

            return await _customerRepository.UpdateAsync(customer);
        }

        public async Task<bool> ExistsAsync(string name, string phone)
        {
            return await _customerRepository.ExistsAsync(name, phone);
        }

        public async Task<Customer> AddOrUpdateAsync(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (customer.Id > 0)
            {
                return await UpdateAsync(customer);
            }

            return await AddAsync(customer);
        }
    }
}
