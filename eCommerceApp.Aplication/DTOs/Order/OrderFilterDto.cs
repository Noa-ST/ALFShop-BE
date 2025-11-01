using eCommerceApp.Domain.Enums;
using System.Linq;

namespace eCommerceApp.Aplication.DTOs.Order
{
    /// <summary>
    /// DTO cho filter và search orders
    /// </summary>
    public class OrderFilterDto
    {
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Search
        public string? Keyword { get; set; } // Search by order ID, customer name, shop name

        // Filters
        public OrderStatus? Status { get; set; }
        public Guid? ShopId { get; set; }
        public string? CustomerId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }

        // Sort
        public string SortBy { get; set; } = "createdAt"; // createdAt, totalAmount, status
        public string SortOrder { get; set; } = "desc"; // asc, desc

        // Validation
        public void Validate()
        {
            if (Page < 1) Page = 1;
            if (PageSize < 1) PageSize = 20;
            if (PageSize > 100) PageSize = 100; // Max 100 items per page

            // Normalize sort fields
            var validSortFields = new[] { "createdAt", "totalAmount", "status", "updatedAt" };
            if (!validSortFields.Contains(SortBy.ToLower()))
                SortBy = "createdAt";

            if (SortOrder.ToLower() != "asc" && SortOrder.ToLower() != "desc")
                SortOrder = "desc";

            // Validate date range
            if (StartDate.HasValue && EndDate.HasValue && StartDate > EndDate)
            {
                var temp = StartDate;
                StartDate = EndDate;
                EndDate = temp;
            }

            // Validate amount range
            if (MinAmount.HasValue && MaxAmount.HasValue && MinAmount > MaxAmount)
            {
                var temp = MinAmount;
                MinAmount = MaxAmount;
                MaxAmount = temp;
            }
        }
    }
}

