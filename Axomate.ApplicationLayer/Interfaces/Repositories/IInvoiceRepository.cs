using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Interfaces.Repositories
{
    public interface IInvoiceRepository
    {
        // Create
        Task<int> AddAsync(Invoice invoice);

        // Update
        Task<Invoice> UpdateAsync(Invoice invoice);

        // Delete
        Task DeleteAsync(int id);

        // Read
        Task<Invoice?> GetByIdAsync(int id, bool includeDetails = true);
        Task<List<Invoice>> GetByCustomerAsync(int customerId);
        Task<List<Invoice>> GetByVehicleAsync(int vehicleId);
        Task<List<Invoice>> GetByDateRangeAsync(DateTime from, DateTime to);
    }
}
