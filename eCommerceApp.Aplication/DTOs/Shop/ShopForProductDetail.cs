namespace eCommerceApp.Aplication.DTOs.Shop
{
    // DTO chỉ chứa các thông tin cần thiết để hiển thị trên trang Product Detail
    public class ShopForProductDetail
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? LogoUrl { get; set; }
        public float Rating { get; set; }
    }
}