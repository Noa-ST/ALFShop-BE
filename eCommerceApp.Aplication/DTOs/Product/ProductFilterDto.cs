using eCommerceApp.Domain.Enums;
using System.Linq;

namespace eCommerceApp.Aplication.DTOs.Product
{
    /// <summary>
    /// DTO cho filter và search products
    /// </summary>
    public class ProductFilterDto
    {
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Search
        public string? Keyword { get; set; }

        // Filters
        public Guid? ShopId { get; set; }
        public Guid? CategoryId { get; set; }
        // ✅ New: Danh sách categoryIds để lọc theo nhiều danh mục (ví dụ descendants)
        public IEnumerable<Guid>? CategoryIds { get; set; }
        public ProductStatus? Status { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        // Sort
        public string SortBy { get; set; } = "createdAt"; // createdAt, price, name, rating
        public string SortOrder { get; set; } = "desc"; // asc, desc

        // Validation
        public void Validate()
        {
            if (Page < 1) Page = 1;
            if (PageSize < 1) PageSize = 20;
            if (PageSize > 100) PageSize = 100; // Max 100 items per page

            // Normalize sort fields
            var validSortFields = new[] { "createdAt", "price", "name", "rating", "updatedAt" };
            if (!validSortFields.Contains(SortBy.ToLower()))
                SortBy = "createdAt";

            if (SortOrder.ToLower() != "asc" && SortOrder.ToLower() != "desc")
                SortOrder = "desc";

            // Chuẩn hóa CategoryIds: bỏ trùng
            if (CategoryIds != null)
                CategoryIds = CategoryIds.Distinct().ToList();
        }
    }
}

