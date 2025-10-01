using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Promotion
    {
        [Key]
        public Guid PromoId { get; set; }

        public Guid? ShopId { get; set; }
        public Guid? ProductId { get; set; }

        [Required]
        public string Code { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(ShopId))]
        public Shop? Shop { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }
    }
}
