using System.ComponentModel.DataAnnotations;
using eCommerceApp.Domain.Enums;

namespace eCommerceApp.Aplication.DTOs.Chat
{
    public class EditMessageRequest
    {
        [Required(ErrorMessage = "Nội dung tin nhắn không được để trống.")]
        [MaxLength(4096, ErrorMessage = "Nội dung tin nhắn không được vượt quá 4096 ký tự.")]
        public string Content { get; set; } = string.Empty;

        [MaxLength(1024, ErrorMessage = "URL đính kèm không được vượt quá 1024 ký tự.")]
        [Url(ErrorMessage = "URL đính kèm không hợp lệ.")]
        public string? AttachmentUrl { get; set; }

        [MaxLength(2048, ErrorMessage = "Metadata không được vượt quá 2048 ký tự.")]
        public string? Metadata { get; set; }
    }
}

