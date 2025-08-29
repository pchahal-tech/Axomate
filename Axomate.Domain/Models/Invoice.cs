using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Axomate.Domain.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        // Foreign keys (required at DB level)
        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int VehicleId { get; set; }

        // Core fields
        [Required]
        public DateTime ServiceDate { get; set; } = DateTime.Now;

        // Snapshot mileage at service time; full history in MileageHistory
        [Range(0, 2_000_000)]
        public int? Mileage { get; set; }

        // Navigation properties (optional in memory; FKs above enforce required relationship)
        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public Vehicle? Vehicle { get; set; }

        public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();

        [NotMapped]
        public decimal TotalAmount => LineItems?.Sum(li => (li?.Price ?? 0m) * (li?.Quantity ?? 0)) ?? 0m;
    }
}
