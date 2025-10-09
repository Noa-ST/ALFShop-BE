using eCommerceApp.Aplication.DTOs.Shop;
using eCommerceApp.Aplication.DTOs.Category;

namespace eCommerceApp.Aplication.DTOs.Product
{
    public class GetProductDetail : GetProduct
    {
        public string? ShopDescription { get; set; }
        public string? ShopLogo { get; set; }
        public string? CategoryDescription { get; set; }
    }
}
