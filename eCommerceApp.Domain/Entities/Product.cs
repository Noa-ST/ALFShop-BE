using eCommerceApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Product : AuditableEntity
    {
        public Guid ShopId { get; set; }
        public Guid CategoryId { get; set; }

        [Required]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int StockQuantity { get; set; }
        public ProductStatus Status { get; set; } = ProductStatus.Pending;
        // Navigation
        [ForeignKey(nameof(ShopId))]
        public Shop? Shop { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        public ICollection<ProductImage>? Images { get; set; }
        public ICollection<OrderItem>? OrderItems { get; set; }
        public ICollection<CartItem>? CartItems { get; set; }
        public ICollection<Promotion>? Promotions { get; set; }
        public ICollection<Review>? Reviews { get; set; }
        public ICollection<ViolationReport>? ViolationReports { get; set; }
    }
}

