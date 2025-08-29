using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Axomate.Domain.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Tagline { get; set; }

        [MaxLength(200)]
        public string? AddressLine1 { get; set; }

        [MaxLength(200)]
        public string? AddressLine2 { get; set; }

        [MaxLength(20)]
        public string? Phone1 { get; set; }

        [MaxLength(20)]
        public string? Phone2 { get; set; }

        [EmailAddress, MaxLength(100)]
        public string? Email { get; set; }

        [Url, MaxLength(100)]
        public string? Website { get; set; }

        [MaxLength(255)]
        public string? LogoPath { get; set; }

        [MaxLength(30)]
        public string? GstNumber { get; set; }

        [Required(ErrorMessage = "GST rate is required.")]
        [Range(0, 1, ErrorMessage = "GST rate must be between 0 and 1.")]
        [Column(TypeName = "decimal(5,4)")]
        public decimal GstRate { get; set; }

        [Required(ErrorMessage = "PST rate is required.")]
        [Range(0, 1, ErrorMessage = "PST rate must be between 0 and 1.")]
        [Column(TypeName = "decimal(5,4)")]
        public decimal PstRate { get; set; }

        [MaxLength(500)]
        public string? ReviewQrText { get; set; }
    }
}
