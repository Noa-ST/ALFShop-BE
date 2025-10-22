using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.ShopCategory
{
    public class UpdateShopCategory : ShopCategoryBase
    {
        [Required]
        public Guid Id { get; set; }
    }
}
