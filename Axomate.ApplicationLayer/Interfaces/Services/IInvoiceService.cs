using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Interfaces.Services
{
    public interface IInvoiceService
    {
        Task<int> AddAsync(Invoice invoice);
        Task<Invoice> UpdateAsync(Invoice invoice);
        Task<Invoice?> GetByIdAsync(int id);
        Task<List<Invoice>> GetByCustomerAsync(int customerId);
        Task<List<Invoice>> GetByVehicleAsync(int vehicleId, bool includeDetails);
        Task<List<Invoice>> GetByDateRangeAsync(DateTime from, DateTime to);
        Task<Invoice> GenerateInvoiceAsync(Customer customer, Vehicle vehicle, List<InvoiceLineItem> lineItems, DateTime serviceDate);
        Task<List<Invoice>> GetVehicleHistoryAsync(int vehicleId, DateTime? from = null, DateTime? to = null, bool includeDetails = true);
    }

}
