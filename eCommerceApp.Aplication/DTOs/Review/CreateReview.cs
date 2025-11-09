using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Review
{
    public class CreateReview
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5.")]
        public int Rating { get; set; }

        public string? Comment { get; set; }
    }
}