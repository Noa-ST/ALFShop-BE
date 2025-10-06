using System.ComponentModel.DataAnnotations;


namespace eCommerceApp.Aplication.DTOs.Shop
{
    public class CreateShop : ShopBase
    {
        [Required]
        public string SellerId { get; set; } = string.Empty;
    }
}
