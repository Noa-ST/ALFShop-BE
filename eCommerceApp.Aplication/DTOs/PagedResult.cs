namespace eCommerceApp.Aplication.DTOs
{
    /// <summary>
    /// Generic class để trả về kết quả có phân trang
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }
}

