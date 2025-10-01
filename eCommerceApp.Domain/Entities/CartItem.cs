using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    // Composite key (CartId, ProductId) - configure in DbContext ModelBuilder
    public class CartItem
    {
        public Guid CartId { get; set; }
        public Guid ProductId { get; set; }

        public int Quantity { get; set; }

        [ForeignKey(nameof(CartId))]
        public Cart? Cart { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }
    }
}
