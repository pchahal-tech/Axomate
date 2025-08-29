using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Axomate.Domain.Models
{
    public class MileageHistory
    {
        public int Id { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public Vehicle? Vehicle { get; set; } 

        [Required, Range(0, 2_000_000, ErrorMessage = "Mileage must be between 0 and 2,000,000.")]
        public int Mileage { get; set; }
               
        [Required]
        public DateTime RecordedDate { get; set; }

        [MaxLength(100)]
        public string? Source { get; set; } // e.g., "Invoice", "Manual", "Import"

        [MaxLength(200)]
        public string? Notes { get; set; }
    }
}
