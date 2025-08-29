using System.Text.RegularExpressions;
using Axomate.ApplicationLayer.Interfaces.Services;
using Axomate.Domain.Models;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace Axomate.ApplicationLayer.Services.Pdf
{
    public class InvoicePdfService : IInvoicePdfService
    {
        private readonly InvoicePdfOptions _opts;
        private readonly ICompanyService _companyService;
        private readonly IMileageHistoryService _mileageHistoryService;

        public InvoicePdfService(IOptions<InvoicePdfOptions> opts,
                                 ICompanyService companyService,
                                 IMileageHistoryService mileageHistoryService)
        {
            _opts = opts.Value ?? new InvoicePdfOptions();
            _companyService = companyService;
            _mileageHistoryService = mileageHistoryService;
        }

        public async Task<byte[]> GenerateAsync(Invoice invoice, CancellationToken ct = default)
        {

            QuestPDF.Settings.License = LicenseType.Community;

            // 1) Resolve output directory
            var rawDir = _opts.OutputDirectory;
            if (string.IsNullOrWhiteSpace(rawDir))
            {
                rawDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Axomate", "Invoices"
                );
            }


            var expanded = Environment.ExpandEnvironmentVariables(rawDir);
            var outputDir = Path.IsPathRooted(expanded)
                ? expanded
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expanded));

            Directory.CreateDirectory(outputDir);

            // 2) Build file name
            var pattern = string.IsNullOrWhiteSpace(_opts.FileNamePatternAAA)
                ? "Invoice_{InvoiceId}_{yyyyMMdd_HHmmss}.pdf"
                : _opts.FileNamePatternAAA;

            string ReplaceTokens(string p)
            {
                var now = DateTime.Now;
                var safeCustomer = SanitizeForFileName(invoice.Customer?.Name ?? "Customer");
                var safeVehicle = SanitizeForFileName(invoice.Vehicle?.LicensePlate ?? "Vehicle");

                return p
                    .Replace("{InvoiceId}", invoice.Id > 0 ? invoice.Id.ToString() : "NEW")
                    .Replace("{Customer}", safeCustomer)
                    .Replace("{Vehicle}", safeVehicle)
                    .Replace("{yyyyMMdd_HHmmss}", now.ToString("yyyyMMdd_HHmmss"))
                    .Replace("{yyyy}", now.ToString("yyyy"))
                    .Replace("{MM}", now.ToString("MM"))
                    .Replace("{dd}", now.ToString("dd"))
                    .Replace("{HH}", now.ToString("HH"))
                    .Replace("{mm}", now.ToString("mm"))
                    .Replace("{ss}", now.ToString("ss"))
                    .Replace("{Customer}", invoice?.Customer?.Name ?? string.Empty)
                    .Replace("{Vehicle}", invoice?.Vehicle?.DisplayName ?? string.Empty);
            }

            var fileName = Path.ChangeExtension(
                SanitizeForFileName(ReplaceTokens(pattern)),
                ".pdf"
            );
            
            var fullPath = Path.Combine(outputDir, fileName);

            // 3) Get company + mileage
            var company = await _companyService.GetAsync();


            int? displayMileage = invoice.Mileage;
            if (!displayMileage.HasValue && (invoice.Vehicle?.Id ?? invoice.VehicleId) > 0)
            {
                var vid = invoice.Vehicle?.Id ?? invoice.VehicleId;
                var byTime = await _mileageHistoryService.GetLatestOnOrBeforeAsync(vid, invoice.ServiceDate);
                if (!byTime.HasValue)
                {







                    var eod = invoice.ServiceDate.Date.AddDays(1).AddTicks(-1);
                    byTime = await _mileageHistoryService.GetLatestOnOrBeforeAsync(vid, eod);




                }
                displayMileage = byTime;
            }


            var reviewUrl = string.IsNullOrWhiteSpace(_opts.ReviewUrl)
                ? "https://search.google.com/local/writereview?placeid=ChIJ42gp40TeiEYRqM4OhEBzGE8"
                : _opts.ReviewUrl;

            var reviewQrPng = GenerateQrPng(reviewUrl, 10);

            // 4) Generate PDF bytes
            byte[] bytes;
            // Assign inside Task.Run and return out
            bytes = await Task.Run(() =>
            {
                using var ms = new MemoryStream();
                var doc = BuildInvoiceDocument(invoice, company, displayMileage, reviewQrPng);
                doc.GeneratePdf(ms);
                return ms.ToArray();
            }, ct);

            // Save to disk
            await File.WriteAllBytesAsync(fullPath, bytes, ct);

            // Auto-open
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fullPath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Could not open PDF: {ex.Message}");
            }

            return bytes;
        }

        private static byte[] GenerateQrPng(string text, int pixelsPerModule = 8)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q); // good for print
            var png = new PngByteQRCode(data);
            return png.GetGraphic(pixelsPerModule); // PNG bytes
        }

        private static string SanitizeForFileName(string value)
        {
            // remove invalid chars and trim length
            var invalid = new string(Path.GetInvalidFileNameChars());
            var cleaned = Regex.Replace(value, $"[{Regex.Escape(invalid)}]", "_");
            return cleaned.Length > 120 ? cleaned[..120] : cleaned;
        }

        // NOTE: changed signature to receive computed mileage
        private IDocument BuildInvoiceDocument(Invoice invoice, Company? company, int? displayMileage, byte[]? reviewQrPng)
        {
            var companyName = company?.Name ?? "To be Filled";
            var addr1 = company?.AddressLine1 ?? "To be Filled";
            var addr2 = company?.AddressLine2 ?? "To be Filled";
            var website = string.IsNullOrWhiteSpace(company?.Website) ? string.Empty : company!.Website!;
            var phone1 = company?.Phone1 ?? "000-000-0000";
            var phone2 = company?.Phone2 ?? "000-000-0000";
            var gstNo = company?.GstNumber ?? "XXXXX";

            var lineItems = invoice.LineItems?.ToList() ?? new List<InvoiceLineItem>();
            var subtotal = lineItems.Sum(x => x.Price * (x.Quantity == 0 ? 1 : x.Quantity));
            var gstRate = company?.GstRate ?? 0.05m;
            var pstRate = company?.PstRate ?? 0.00m;
            var gst = Math.Round(subtotal * gstRate, 2);
            var pst = Math.Round(subtotal * pstRate, 2);
            var other = 0m;
            var total = subtotal + gst + pst + other;




            return Document.Create(c =>
            {
                c.Page(p =>
                {
                    p.Size(PageSizes.A4);
                    p.Margin(20);

                    // HEADER
                    p.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(companyName).FontSize(15).Bold();
                            col.Item().Text(addr1);
                            col.Item().Text(addr2);
                            col.Item().Text(website);
                        });

                        row.ConstantItem(200).AlignRight().Column(col =>
                        {
                            col.Item().Text("INVOICE").FontSize(18).Bold();
                            col.Item().PaddingTop(1).Text(phone1);
                            col.Item().Text(phone2);
                        });
                    });

                    p.Content().Column(col =>
                    {
                        col.Item().PaddingTop(35);

                        // BILL TO + VEHICLE DETAILS
                        col.Item().Row(row =>
                        {
                            // Bill To
                            row.RelativeItem().Column(c2 =>
                            {
                                c2.Item().PaddingBottom(7).Text("Bill To").Bold().FontSize(12);

                                LabeledLine(c2, "Name", invoice.Customer?.Name, 10);
                                LabeledLine(c2, "Address", invoice.Customer?.AddressLine1, 10);
                                LabeledLine(c2, "Phone", invoice.Customer?.Phone, 10);
                            });

                            // Vehicle details
                            row.RelativeItem().Column(c2 =>
                            {
                                c2.Item().PaddingBottom(7).Text("Vehicle Details").Bold().FontSize(12);

                                LabeledLine(c2, "Year", invoice.Vehicle?.Year?.ToString());
                                LabeledLine(c2, "Make", invoice.Vehicle?.Make);
                                LabeledLine(c2, "Model", invoice.Vehicle?.Model);
                                LabeledLine(c2, "Lic#", invoice.Vehicle?.LicensePlate);
                                LabeledLine(c2, "VIN", invoice.Vehicle?.VIN);
                                LabeledLine(c2, "Mi/Km", displayMileage?.ToString() ?? "");
                            });
                        });

                        col.Item().PaddingTop(8);

                        // SERVICE TABLE
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(40);   // Qty
                                cols.RelativeColumn(4);    // Description
                                cols.ConstantColumn(70);   // Qty/hour
                                cols.ConstantColumn(70);   // Price
                                cols.ConstantColumn(80);   // Total
                            });

                            // header
                            table.Header(h =>
                            {
                                HeaderCell(h.Cell(), "Item#");
                                HeaderCell(h.Cell(), "Description");
                                HeaderCell(h.Cell(), "Qty/hour");
                                HeaderCell(h.Cell(), "Price");
                                HeaderCell(h.Cell(), "Total");
                            });

                            // data rows
                            int index = 1;
                            foreach (var li in lineItems)
                            {
                                var qty = (li.Quantity == 0 ? 1 : li.Quantity);
                                var unit = li.Price;
                                var ext = unit * qty;

                                BodyCell(table.Cell(), (index++).ToString());
                                BodyCell(table.Cell(), li.Description ?? li.ServiceItem?.Name ?? "Other");
                                BodyCell(table.Cell(), qty.ToString());
                                BodyCell(table.Cell(), unit.ToString("C"));
                                BodyCell(table.Cell(), ext.ToString("C"));
                            }

                            // blank rows
                            int desiredRows = 12;
                            for (int i = lineItems.Count; i < desiredRows; i++)
                            {
                                BodyCell(table.Cell(), "");
                                BodyCell(table.Cell(), "");
                                BodyCell(table.Cell(), "");
                                BodyCell(table.Cell(), "");
                                BodyCell(table.Cell(), "");
                            }
                        });

                        col.Item().PaddingTop(5);

                        // BOTTOM: GST note + QR + TOTALS box
                        col.Item().Row(r =>
                        {
                            // left notes + QR
                            r.RelativeItem().Column(leftCol =>
                            {

                                leftCol.Item().Text($"GST # {gstNo}");


                                leftCol.Item().PaddingTop(15)
                                    .Text("We value your feedback! Please take a moment to leave us a Google review by scanning the QR code. Thank you for choosing HP Car Fix.");


                                leftCol.Item().PaddingTop(8).AlignLeft().Element(e =>
                                {
                                    var box = e.Border(1)
                                               .Padding(2)
                                               .Width(80)
                                               .Height(80)
                                               .AlignCenter()
                                               .AlignMiddle();

                                    if (reviewQrPng is not null && reviewQrPng.Length > 0)
                                        box.Image(reviewQrPng);
                                    else
                                        box.Text("QR");
                                });
                            });

                            // totals
                            r.ConstantItem(220).Column(rc =>
                            {
                                rc.Item().Element(Box).Column(box =>
                                {
                                    LineTotal(box, "Subtotal", subtotal);
                                    LineTotal(box, "GST", gst);
                                    LineTotal(box, "PST", pst);
                                    LineTotal(box, "Other", other);
                                    LineTotal(box, "TOTAL", total, true);
                                });
                            });
                        });
                    });
                });
            });

            // ===== helpers =====

            static void LabeledLine(ColumnDescriptor col, string label, string? value, float fontSize = 8)
            {
                col.Item().Row(r =>
                {
                    if (!string.IsNullOrWhiteSpace(label))
                        r.ConstantItem(55).Text(label).FontSize(fontSize);

                    r.RelativeItem()
                        .PaddingRight(40)
                        .BorderBottom(0.5f)
                        .PaddingBottom(2)
                        .Text(string.IsNullOrWhiteSpace(value) ? "" : value)
                        .FontSize(fontSize);
                });
            }

            static void HeaderCell(IContainer c, string text)
            {
                c.Border(1).Background(Colors.Grey.Lighten3).Padding(3).Text(text).Bold();
            }

            static void BodyCell(IContainer c, string text)
            {
                c.Border(1).Padding(3).Text(string.IsNullOrEmpty(text) ? " " : text);
            }

            static IContainer Box(IContainer c)
            {
                return c.Border(1).Padding(6);
            }

            static void LineTotal(ColumnDescriptor box, string label, decimal amount, bool bold = false)
            {
                box.Item().Row(r =>
                {
                    var left = r.RelativeItem().Text(label);
                    if (bold) left.Bold();

                    var right = r.ConstantItem(80).AlignRight().Text(amount.ToString("C"));
                    if (bold) right.Bold();
                });
            }
        }

    }
}
