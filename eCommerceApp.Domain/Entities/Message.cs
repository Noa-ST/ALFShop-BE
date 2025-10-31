using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Message
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ConversationId { get; set; } // FK -> Conversation
        public string SenderId { get; set; } = null!; // FK -> User.Id (là Customer hoặc Seller)

        [MaxLength(4096)]
        public string Content { get; set; } = null!;

        public MessageType Type { get; set; } = MessageType.Text;

        [MaxLength(1024)]
        public string? AttachmentUrl { get; set; }

        [MaxLength(2048)]
        public string? Metadata { get; set; } // JSON metadata (ví dụ: kích thước file, toạ độ...)

        public Guid? OrderId { get; set; }
        public Guid? ProductId { get; set; }

        public bool IsRead { get; set; } = false; // Trạng thái đã đọc/chưa đọc
        public DateTime? ReadAt { get; set; }

        public bool IsEdited { get; set; } = false;
        public DateTime? EditedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public Guid? ReplyToMessageId { get; set; }

        [ForeignKey(nameof(ReplyToMessageId))]
        public Message? ReplyToMessage { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ConversationId))]
        public Conversation? Conversation { get; set; }

        [ForeignKey(nameof(SenderId))]
        public User? Sender { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order? Order { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        public ICollection<Message>? Replies { get; set; }
    }
}