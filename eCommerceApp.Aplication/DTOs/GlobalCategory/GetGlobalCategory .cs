using eCommerceApp.Aplication.DTOs.Product;

namespace eCommerceApp.Aplication.DTOs.GlobalCategory
{
    public class GetGlobalCategory : GlobalCategoryBase
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation Properties cho cấu trúc cây
        public GetGlobalCategory? Parent { get; set; }
        public ICollection<GetGlobalCategory>? Children { get; set; }
    }
}
