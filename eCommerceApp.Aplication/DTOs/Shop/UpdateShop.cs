using System.ComponentModel.DataAnnotations;


namespace eCommerceApp.Aplication.DTOs.Shop
{
    public class UpdateShop : ShopBase
    {
        [Required]
        public Guid Id { get; set; }

    }
}
