using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Order
{
    public class UpdateTrackingNumberDto
    {
        [Required(ErrorMessage = "Mã vận chuyển không được để trống.")]
        [StringLength(100, ErrorMessage = "Mã vận chuyển không được vượt quá 100 ký tự.")]
        public string TrackingNumber { get; set; } = string.Empty;
    }
}

