using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Axomate.Domain.Models
{
    public class ServiceItem
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 100000)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
    }
}
