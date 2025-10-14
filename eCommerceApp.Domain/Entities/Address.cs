using eCommerceApp.Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Address : AuditableEntity
    {
        public string UserId { get; set; } = null!; // FK -> User.Id

        [Required]
        public string RecipientName { get; set; } = null!; // Tên người nhận

        [Required]
        public string PhoneNumber { get; set; } = null!; // Số điện thoại
        // ---------------------------------

        [Required]
        public string Street { get; set; } = null!; // Bao gồm cả Phường/Xã

        [Required]
        public string City { get; set; } = null!; // Tỉnh/Thành

        // Giả sử Country luôn là Việt Nam, có thể thay bằng District/Ward nếu cần chi tiết hơn
        [Required]
        public string Country { get; set; } = null!;

        public bool IsDefault { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}