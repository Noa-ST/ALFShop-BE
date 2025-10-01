using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Product
{
    public class UpdateProduct : ProductBase
    {
        [Required]
        public Guid Id { get; set; }
    }
}
