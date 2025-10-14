using eCommerceApp.Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Conversation : AuditableEntity
    {
        // User1Id là CustomerId hoặc SellerId
        public string User1Id { get; set; } = null!;

        // User2Id là SellerId hoặc CustomerId
        public string User2Id { get; set; } = null!;

        // Thời gian tin nhắn cuối cùng để sắp xếp
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(User1Id))]
        public User? User1 { get; set; }

        [ForeignKey(nameof(User2Id))]
        public User? User2 { get; set; }

        public ICollection<Message>? Messages { get; set; }
    }
}