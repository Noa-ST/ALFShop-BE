using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Address
{
    public class CreateAddress
    {
        [Required] public string RecipientName { get; set; } = null!;
        [Required] public string PhoneNumber { get; set; } = null!;
        // Số nhà + tên đường
        [Required] public string FullStreet { get; set; } = null!;
        // Phường/Xã/Thị trấn
        [Required] public string Ward { get; set; } = null!;
        // Quận/Huyện/Thị xã/Thành phố (cấp huyện)
        [Required] public string District { get; set; } = null!;
        // Tỉnh/Thành phố (cấp tỉnh)
        [Required] public string Province { get; set; } = null!;
        [Required] public string Country { get; set; } = "Việt Nam";
        public bool IsDefault { get; set; } = false;
    }
}