using eCommerceApp.Domain.Enums;

namespace eCommerceApp.Aplication.DTOs.Chat
{
    public class MessageDto
    {
        public Guid MessageId { get; set; }
        public Guid ConversationId { get; set; }
        public string SenderId { get; set; } = null!;
        public string? SenderName { get; set; }
        public string? SenderAvatarUrl { get; set; }
        public string Content { get; set; } = null!;
        public MessageType Type { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? Metadata { get; set; }
        public MessageOrderAttachmentDto? OrderAttachment { get; set; }
        public MessageProductAttachmentDto? ProductAttachment { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? ReplyToMessageId { get; set; }
        public MessageDto? ReplyToMessage { get; set; }
    }
}

