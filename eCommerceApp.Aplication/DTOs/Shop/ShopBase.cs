using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Shop
{
    public class ShopBase
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? Logo { get; set; }
    }
}
