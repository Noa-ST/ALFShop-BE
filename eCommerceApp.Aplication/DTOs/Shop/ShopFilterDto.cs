using System.Linq;

namespace eCommerceApp.Aplication.DTOs.Shop
{
    /// <summary>
    /// DTO cho filter và search shops
    /// </summary>
    public class ShopFilterDto
    {
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Search
        public string? Keyword { get; set; }

        // Filters
        public string? City { get; set; }
        public string? Country { get; set; }
        public float? MinRating { get; set; }
        public float? MaxRating { get; set; }

        // Sort
        public string SortBy { get; set; } = "createdAt"; // createdAt, name, rating, city
        public string SortOrder { get; set; } = "desc"; // asc, desc

        // Validation
        public void Validate()
        {
            if (Page < 1) Page = 1;
            if (PageSize < 1) PageSize = 20;
            if (PageSize > 100) PageSize = 100; // Max 100 items per page

            // Normalize sort fields
            var validSortFields = new[] { "createdAt", "name", "rating", "city", "updatedAt" };
            if (!validSortFields.Contains(SortBy.ToLower()))
                SortBy = "createdAt";

            if (SortOrder.ToLower() != "asc" && SortOrder.ToLower() != "desc")
                SortOrder = "desc";

            // Validate rating range
            if (MinRating.HasValue && MinRating < 0) MinRating = 0;
            if (MaxRating.HasValue && MaxRating > 5) MaxRating = 5;
            if (MinRating.HasValue && MaxRating.HasValue && MinRating > MaxRating)
            {
                var temp = MinRating;
                MinRating = MaxRating;
                MaxRating = temp;
            }
        }
    }
}

