using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Product
{
    public class UpdateStockDto
    {
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải lớn hơn hoặc bằng 0")]
        public int StockQuantity { get; set; }
    }
}

