using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Cart
{
    public class UpdateCartItem
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng không được âm.")]
        public int Quantity { get; set; }
    }
}