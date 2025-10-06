using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Order : AuditableEntity
    {
        public string CustomerId { get; set; } = null!; // FK -> User.Id
        public Guid AddressId { get; set; } // FK -> Address.AddressId

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [ForeignKey(nameof(CustomerId))]
        public User? Customer { get; set; }

        [ForeignKey(nameof(AddressId))]
        public Address? ShippingAddress { get; set; }

        public ICollection<OrderItem>? Items { get; set; }
        public Payment? Payment { get; set; }
    }
}
