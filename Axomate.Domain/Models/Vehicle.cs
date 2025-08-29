using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Axomate.Domain.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Make { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Model { get; set; } = string.Empty;

        [Range(1900, 2100)]
        public int? Year { get; set; }

        [Required, MaxLength(20)]
        public string LicensePlate { get; set; } = string.Empty;

        [RegularExpression(
            @"^[A-HJ-NPR-Z0-9]{11,17}$",
            ErrorMessage = "VIN must be 11–17 characters, using only letters (A–Z, excluding I, O, Q) and digits (0–9)."
        )]
        public string? VIN { get; set; }

        [MaxLength(30)]
        public string? Color { get; set; }

        [MaxLength(50)]
        public string? Engine { get; set; }

        [MaxLength(50)]
        public string? Transmission { get; set; }

        [MaxLength(20)]
        public string? FuelType { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        [NotMapped]
        public string DisplayName => Year.HasValue
            ? $"{Make} {Model} ({Year}) {LicensePlate}"
            : $"{Make} {Model} {LicensePlate}";

        public ICollection<MileageHistory> MileageHistories { get; set; } = new List<MileageHistory>();
    }
}
