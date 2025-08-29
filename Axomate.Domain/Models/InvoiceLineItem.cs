using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Axomate.Domain.Models
{
    public class InvoiceLineItem
    {
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        public Invoice? Invoice { get; set; }

        [Required, MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, 1000000)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required, Range(1, 1000)]
        public int Quantity { get; set; } = 1;

        public int? ServiceItemId { get; set; }

        [ForeignKey(nameof(ServiceItemId))]
        public ServiceItem? ServiceItem { get; set; }
    }
}
