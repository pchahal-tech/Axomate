// Axomate.ApplicationLayer/Services/Pdf/InvoicePdfOptions.cs
namespace Axomate.ApplicationLayer.Services.Pdf
{
    public class InvoicePdfOptions
    {
        /// <summary>
        /// Directory where PDFs will be saved. 
        /// Default: %USERPROFILE%\Documents\Axomate\Invoices
        /// </summary>
        public string? OutputDirectory { get; set; }

        /// <summary>
        /// File name pattern with tokens:
        /// {InvoiceId}, {Customer}, {Vehicle}, {yyyyMMdd_HHmmss}, {yyyy}, {MM}, {dd}, {HH}, {mm}, {ss}
        /// </summary>
        public string FileNamePatternAAA { get; set; } = "Invoice_{InvoiceId}_{yyyyMMdd_HHmmss}_{Customer}_{Vehicle}.pdf";

        /// <summary>
        /// Optional: Google Review link (or other feedback link) to embed as QR.
        /// If null/empty, the PDF shows a "QR" placeholder box instead.
        /// </summary>
        public string? ReviewUrl { get; set; }
    }
}
