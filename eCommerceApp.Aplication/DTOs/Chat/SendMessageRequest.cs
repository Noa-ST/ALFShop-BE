using eCommerceApp.Domain.Enums;

namespace eCommerceApp.Aplication.DTOs.Chat
{
    public class SendMessageRequest
    {
        public string Content { get; set; } = string.Empty;
        public MessageType Type { get; set; } = MessageType.Text;
        public string? AttachmentUrl { get; set; }
        public string? Metadata { get; set; }
        public Guid? ReplyToMessageId { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? ProductId { get; set; }
    }
}

