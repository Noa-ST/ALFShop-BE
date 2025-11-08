using System.ComponentModel.DataAnnotations.Schema;
using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Enums;

namespace eCommerceApp.Domain.Entities
{
    public class Order : AuditableEntity
    {
        public string CustomerId { get; set; } = null!; // FK -> User.Id
        public Guid AddressId { get; set; } // FK -> Address.Id

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } // Tổng tiền cuối cùng
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public Guid ShopId { get; set; } 
        public PaymentMethod PaymentMethod { get; set; } // Phương thức thanh toán
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending; // Trạng thái thanh toán

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0; // Phí vận chuyển

        public string? PromotionCodeUsed { get; set; } // Mã khuyến mãi áp dụng

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0; // Số tiền giảm giá

        public string? TrackingNumber { get; set; } // Mã vận chuyển
                                                    // -------------------------------------

        // Navigation
        [ForeignKey(nameof(CustomerId))]
        public User? Customer { get; set; }

        [ForeignKey(nameof(AddressId))]
        public Address? ShippingAddress { get; set; }

        [ForeignKey(nameof(ShopId))]
        public Shop? Shop { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        // Bỏ Navigation Payment vì dùng OneToOne Key
        // public Payment? Payment { get; set; }
    }
}