namespace eCommerceApp.Aplication.DTOs.ShopCategory
{
    public class GetShopCategory : ShopCategoryBase
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; } // Trả về ShopId mà nó thuộc về
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation Properties cho cấu trúc cây
        public GetShopCategory? Parent { get; set; }
        public ICollection<GetShopCategory>? Children { get; set; }
    }
}
