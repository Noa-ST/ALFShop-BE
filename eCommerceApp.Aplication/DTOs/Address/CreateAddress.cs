using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Address
{
    public class CreateAddress
    {
        [Required(ErrorMessage = "Tên người nhận là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên người nhận không được vượt quá 100 ký tự.")]
        public string RecipientName { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [RegularExpression(@"^(0|\+84)[1-9][0-9]{8,9}$", ErrorMessage = "Số điện thoại không hợp lệ. Vui lòng nhập số điện thoại Việt Nam (10-11 số, bắt đầu bằng 0 hoặc +84).")]
        public string PhoneNumber { get; set; } = null!;

        // Số nhà + tên đường
        [Required(ErrorMessage = "Địa chỉ đường là bắt buộc.")]
        [StringLength(200, ErrorMessage = "Địa chỉ đường không được vượt quá 200 ký tự.")]
        public string FullStreet { get; set; } = null!;

        // Phường/Xã/Thị trấn
        [Required(ErrorMessage = "Phường/Xã là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Phường/Xã không được vượt quá 100 ký tự.")]
        public string Ward { get; set; } = null!;

        // Quận/Huyện/Thị xã/Thành phố (cấp huyện)
        [Required(ErrorMessage = "Quận/Huyện là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Quận/Huyện không được vượt quá 100 ký tự.")]
        public string District { get; set; } = null!;

        // Tỉnh/Thành phố (cấp tỉnh)
        [Required(ErrorMessage = "Tỉnh/Thành phố là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tỉnh/Thành phố không được vượt quá 100 ký tự.")]
        public string Province { get; set; } = null!;

        [Required(ErrorMessage = "Quốc gia là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Quốc gia không được vượt quá 50 ký tự.")]
        public string Country { get; set; } = "Việt Nam";

        public bool IsDefault { get; set; } = false;
    }
}