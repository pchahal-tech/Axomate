using Axomate.ApplicationLayer.Interfaces.Repositories;
using Axomate.ApplicationLayer.Interfaces.Services;
using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public InvoiceService(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public async Task<int> AddAsync(Invoice invoice)
        {
            return await _invoiceRepository.AddAsync(invoice);
        }

        public async Task<Invoice> UpdateAsync(Invoice invoice)
        {
            return await _invoiceRepository.UpdateAsync(invoice);
        }

        public async Task<Invoice?> GetByIdAsync(int id)
        {
            return await _invoiceRepository.GetByIdAsync(id);
        }

        public async Task<List<Invoice>> GetByCustomerAsync(int customerId)
        {
            return await _invoiceRepository.GetByCustomerAsync(customerId);
        }

        public async Task<List<Invoice>> GetByVehicleAsync(int vehicleId, bool includeDetails)
        {
            var list = await _invoiceRepository.GetByVehicleAsync(vehicleId);
            if (!includeDetails) return list.OrderByDescending(i => i.ServiceDate).ToList();

            var withDetails = new List<Invoice>(list.Count);
            foreach (var inv in list)
                withDetails.Add(await _invoiceRepository.GetByIdAsync(inv.Id, includeDetails: true) ?? inv);

            return withDetails.OrderByDescending(i => i.ServiceDate).ToList();
        }

        public async Task<List<Invoice>> GetByDateRangeAsync(DateTime from, DateTime to)
        {
            return await _invoiceRepository.GetByDateRangeAsync(from, to);
        }

        public async Task<List<Invoice>> GetVehicleHistoryAsync(
            int vehicleId,
            DateTime? from = null,
            DateTime? to = null,
            bool includeDetails = true)
        {
            var list = await _invoiceRepository.GetByVehicleAsync(vehicleId);

            if (from.HasValue) list = list.Where(i => i.ServiceDate.Date >= from.Value.Date).ToList();
            if (to.HasValue) list = list.Where(i => i.ServiceDate.Date <= to.Value.Date).ToList();

            if (includeDetails)
            {
                // Ensure LineItems are populated (uses your repo's includeDetails)
                var withDetails = new List<Invoice>(list.Count);
                foreach (var inv in list)
                {
                    var full = await _invoiceRepository.GetByIdAsync(inv.Id, includeDetails: true) ?? inv;
                    withDetails.Add(full);
                }
                list = withDetails;
            }

            return list.OrderByDescending(i => i.ServiceDate).ToList();
        }

        public async Task<Invoice> GenerateInvoiceAsync(Customer customer, Vehicle vehicle, List<InvoiceLineItem> lineItems, DateTime serviceDate)
        {
            var invoice = new Invoice
            {
                CustomerId = customer.Id,
                VehicleId = vehicle.Id,
                Customer = customer,
                Vehicle = vehicle,
                ServiceDate = serviceDate,
                LineItems = lineItems
            };

            var id = await _invoiceRepository.AddAsync(invoice);
            invoice.Id = id;
            return invoice;
        }
    }
}
