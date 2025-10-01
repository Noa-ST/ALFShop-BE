using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Order
    {
        [Key]
        public Guid OrderId { get; set; }

        public string CustomerId { get; set; } = null!; // FK -> User.Id
        public Guid AddressId { get; set; } // FK -> Address.AddressId

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public User? Customer { get; set; }

        [ForeignKey(nameof(AddressId))]
        public Address? ShippingAddress { get; set; }

        public ICollection<OrderItem>? Items { get; set; }
        public Payment? Payment { get; set; }
    }
}
