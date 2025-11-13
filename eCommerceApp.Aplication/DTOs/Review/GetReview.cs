using eCommerceApp.Domain.Enums;

namespace eCommerceApp.Aplication.DTOs.Review
{
    public class GetReview
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? UserFullName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string? RejectionReason { get; set; }
        public ReviewStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}