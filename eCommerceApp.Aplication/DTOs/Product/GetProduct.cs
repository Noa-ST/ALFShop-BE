using eCommerceApp.Aplication.DTOs.Category;
using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Product
{
    public class GetProduct : ProductBase
    {
        [Required]
        public Guid Id { get; set; }
        public GetCategory? Category { get; set; }
    }
}
