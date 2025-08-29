using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Axomate.Domain.Models
{
    /// <summary>
    /// A record of a service performed on a vehicle. Can exist with or without an Invoice.
    /// </summary>
    public class ServiceHistory
    {
        public int Id { get; set; }

        // Links
        [Required]
        public int VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        public int? InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        // Core facts
        [Required, DataType(DataType.Date)]
        public DateTime ServiceDate { get; set; } = DateTime.Today;

        [Range(0, 2000000, ErrorMessage = "Odometer must be 0–2,000,000.")]
        public int? Odometer { get; set; }

        [MaxLength(200)]
        public string? Summary { get; set; }  // e.g., "Oil change + tire rotation"

        [MaxLength(2000)]
        public string? Details { get; set; }  // freeform notes

        // Money (nullable to allow records without pricing)
        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "0", "9999999")]
        public decimal? TotalCost { get; set; }

        // Provenance
        [MaxLength(50)]
        public string? Source { get; set; }   // e.g., "Manual", "Invoice", "Imported"
    }
}
