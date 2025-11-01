using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Chat
{
    public class CreateConversationRequest
    {
        [Required(ErrorMessage = "TargetUserId không được để trống.")]
        [StringLength(450, ErrorMessage = "TargetUserId không hợp lệ.")]
        public string TargetUserId { get; set; } = null!;
    }
}

