using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class ProductImage
    {
        [Key]
        public Guid ImageId { get; set; }

        public Guid ProductId { get; set; }

        [Required]
        public string Url { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }
    }
}
