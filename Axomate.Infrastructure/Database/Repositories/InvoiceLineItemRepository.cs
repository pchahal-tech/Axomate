using Axomate.ApplicationLayer.Interfaces.Repositories;
using Axomate.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Axomate.Infrastructure.Database.Repositories
{
    public class InvoiceLineItemRepository : IInvoiceLineItemRepository
    {
        private readonly AxomateDbContext _db;

        public InvoiceLineItemRepository(AxomateDbContext db)
        {
            _db = db;
        }

        // Create
        public async Task<int> AddAsync(InvoiceLineItem lineItem)
        {
            _db.InvoiceLineItems.Add(lineItem);
            await _db.SaveChangesAsync();
            return lineItem.Id;
        }

        // Read (with optional ServiceItem include)
        public async Task<List<InvoiceLineItem>> GetByInvoiceIdAsync(int invoiceId, bool includeServiceItem)
        {
            var query = _db.InvoiceLineItems.AsQueryable();

            if (includeServiceItem)
                query = query.Include(li => li.ServiceItem);

            return await query
                .Where(li => li.InvoiceId == invoiceId)
                .ToListAsync();
        }

        // Update
        public async Task<InvoiceLineItem> UpdateAsync(InvoiceLineItem lineItem)
        {
            _db.InvoiceLineItems.Update(lineItem);
            await _db.SaveChangesAsync();
            return lineItem;
        }

        // Delete
        public async Task DeleteAsync(int lineItemId)
        {
            var item = await _db.InvoiceLineItems.FindAsync(lineItemId);
            if (item != null)
            {
                _db.InvoiceLineItems.Remove(item);
                await _db.SaveChangesAsync();
            }
        }
    }
}
