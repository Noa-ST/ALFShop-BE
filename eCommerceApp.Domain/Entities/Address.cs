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

        // Đường + Số nhà (Ví dụ: "358/14/15 Nguyễn Thái Học")
        [Required]
        public string FullStreet { get; set; } = null!;

        // Phường/Xã/Thị trấn
        [Required]
        public string Ward { get; set; } = null!;

        // Quận/Huyện/Thị xã/Thành phố (cấp huyện)
        [Required]
        public string District { get; set; } = null!;

        // Tỉnh/Thành phố (cấp tỉnh)
        [Required]
        public string Province { get; set; } = null!;

        // Quốc gia (mặc định Việt Nam)
        [Required]
        public string Country { get; set; } = "Việt Nam";

        public bool IsDefault { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}