using Axomate.ApplicationLayer.Interfaces.Repositories;
using Axomate.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Axomate.Infrastructure.Database.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly AxomateDbContext _context;

        public InvoiceRepository(AxomateDbContext context)
        {
            _context = context;
        }

        // Create
        public async Task<int> AddAsync(Invoice invoice)
        {
            // Ensure FK values are set directly
            invoice.CustomerId = invoice.Customer?.Id ?? invoice.CustomerId;
            invoice.VehicleId = invoice.Vehicle?.Id ?? invoice.VehicleId;

            // Prevent EF from trying to re-insert existing Customer and Vehicle
            if (invoice.Customer != null)
                _context.Entry(invoice.Customer).State = EntityState.Unchanged;

            if (invoice.Vehicle != null)
                _context.Entry(invoice.Vehicle).State = EntityState.Unchanged;

            // Line Items
            if (invoice.LineItems != null)
            {
                foreach (var line in invoice.LineItems)
                {
                    line.Invoice = invoice; // Set navigation property

                    // Handle ServiceItem FK if present
                    line.ServiceItemId = line.ServiceItem?.Id ?? line.ServiceItemId;

                    if (line.ServiceItem != null)
                        _context.Entry(line.ServiceItem).State = EntityState.Unchanged;
                }
            }

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            return invoice.Id;
        }

        // Update
        public async Task<Invoice> UpdateAsync(Invoice invoice)
        {
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        // Delete
        public async Task DeleteAsync(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
            }
        }

        // Read single invoice (with optional eager-loading)
        public async Task<Invoice?> GetByIdAsync(int id, bool includeDetails = true)
        {
            var query = _context.Invoices.AsQueryable();

            if (includeDetails)
            {
                query = query
                    .Include(i => i.Customer)
                    .Include(i => i.Vehicle)
                    .Include(i => i.LineItems)
                        .ThenInclude(li => li.ServiceItem);
            }

            return await query.FirstOrDefaultAsync(i => i.Id == id);
        }

        // Read all invoices for a customer
        public async Task<List<Invoice>> GetByCustomerAsync(int customerId)
        {
            return await _context.Invoices
                .AsNoTracking()
                .Where(i => i.CustomerId == customerId)
                .OrderByDescending(i => i.ServiceDate)
                .ToListAsync();
        }

        // Read all invoices for a vehicle
        public async Task<List<Invoice>> GetByVehicleAsync(int vehicleId)
        {
            return await _context.Invoices
                .AsNoTracking()
                .Where(i => i.VehicleId == vehicleId)
                .OrderByDescending(i => i.ServiceDate)
                .ToListAsync();
        }

        // Read invoices by date range
        public async Task<List<Invoice>> GetByDateRangeAsync(DateTime from, DateTime to)
        {
            var toExclusive = to.Date.AddDays(1); // include entire 'to' day
            return await _context.Invoices
                 .AsNoTracking()
                .Where(i => i.ServiceDate >= from && i.ServiceDate < toExclusive)
                .OrderByDescending(i => i.ServiceDate)
                .ToListAsync();
        }
    }
}
