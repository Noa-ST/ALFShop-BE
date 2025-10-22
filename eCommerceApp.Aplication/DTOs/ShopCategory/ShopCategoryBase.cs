using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.ShopCategory
{
    public abstract class ShopCategoryBase
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        public Guid? ParentId { get; set; }
    }
}
