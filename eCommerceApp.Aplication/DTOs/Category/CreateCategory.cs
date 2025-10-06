using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Category
{
    public class CreateCategory : CategoryBase
    {
        [Required]
        public Guid ShopId { get; set; } // Mỗi category thuộc 1 shop
    }
}
