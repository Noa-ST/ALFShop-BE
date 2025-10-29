using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Shop
{
    public class ShopBase
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? Logo { get; set; }

        // ✅ [BỔ SUNG THÔNG TIN ĐỊA CHỈ SHOP]
        [Required]
        public string Street { get; set; } = string.Empty; // Đường, Phường/Xã
        
        [Required]
        public string City { get; set; } = string.Empty; // Tỉnh/Thành phố (Dùng cho Lọc)
        
        public string? Country { get; set; } = "Việt Nam"; // Mặc định
    }
}
