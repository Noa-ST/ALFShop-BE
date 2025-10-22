using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.GlobalCategory
{
    public class GlobalCategoryBase
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        // Hỗ trợ phân cấp (ParentId)
        public Guid? ParentId { get; set; }
    }
}
