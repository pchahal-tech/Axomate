using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Axomate.Domain.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Name is required.")]
        [RegularExpression(
            @"^[\p{L}\p{M}0-9][\p{L}\p{M}0-9\s\.'-]{0,99}$",
            ErrorMessage = "Name can contain letters, numbers, spaces, apostrophes, hyphens, and periods only."
        )]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? AddressLine1 { get; set; }
     
        [Phone, MaxLength(20)]
        public string? Phone { get; set; }

        [EmailAddress, MaxLength(100)]
        public string? Email { get; set; }

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }
}
