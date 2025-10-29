using System;
using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Cart
{
    public class AddCartItem
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public int Quantity { get; set; } = 1;
    }
}