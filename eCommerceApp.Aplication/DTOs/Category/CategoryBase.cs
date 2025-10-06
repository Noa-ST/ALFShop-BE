using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Category
{
    public class CategoryBase
    {
        [Required]
        public string? Name { get; set; }

        public string? Description { get; set; }
    }
}
