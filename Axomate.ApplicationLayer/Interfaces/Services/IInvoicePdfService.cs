using Axomate.Domain.Models;
using System.Threading;

namespace Axomate.ApplicationLayer.Interfaces.Services
{
    public interface IInvoicePdfService
    {
        /// <summary>
        /// Generates a PDF for the given invoice, saves it, 
        /// and returns the PDF as a raw byte array.
        /// </summary>
        Task<byte[]> GenerateAsync(Invoice invoice, CancellationToken ct = default);
    }
}
