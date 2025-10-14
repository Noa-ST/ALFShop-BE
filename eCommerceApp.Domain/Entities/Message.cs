using eCommerceApp.Domain.Entities.Identity;
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

        public string Content { get; set; } = null!;
        public bool IsRead { get; set; } = false; // Trạng thái đã đọc/chưa đọc
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(ConversationId))]
        public Conversation? Conversation { get; set; }

        [ForeignKey(nameof(SenderId))]
        public User? Sender { get; set; }
    }
}