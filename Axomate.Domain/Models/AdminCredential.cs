using System;
using System.ComponentModel.DataAnnotations;

namespace Axomate.Domain.Models
{
    public class AdminCredential
    {
        public int Id { get; set; }

        [Required, MaxLength(64)]
        public string Username { get; set; } = "admin";

        [Required, MaxLength(512)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public byte[] Salt { get; set; } = Array.Empty<byte>();

        [Range(10_000, 500_000)]
        public int Iterations { get; set; } = 60_000;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ChangedAtUtc { get; set; }
    }
}
