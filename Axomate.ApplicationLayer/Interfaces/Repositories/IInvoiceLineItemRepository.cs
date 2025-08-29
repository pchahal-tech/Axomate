using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Interfaces.Repositories
{
    public interface IInvoiceLineItemRepository
    {
        // Create
        Task<int> AddAsync(InvoiceLineItem lineItem);

        // Read        
        Task<List<InvoiceLineItem>> GetByInvoiceIdAsync(int invoiceId, bool includeServiceItem);

        // Update
        Task<InvoiceLineItem> UpdateAsync(InvoiceLineItem lineItem);

        // Delete
        Task DeleteAsync(int lineItemId);
    }
}
