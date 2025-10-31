using eCommerceApp.Domain.Entities.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        // Nội dung tin nhắn cuối cùng để hiển thị ở danh sách hội thoại
        [MaxLength(1024)]
        public string? LastMessageContent { get; set; }

        // Người gửi tin nhắn cuối cùng
        [MaxLength(450)]
        public string? LastMessageSenderId { get; set; }

        // Số lượng tin nhắn chưa đọc theo từng người dùng
        public int User1UnreadCount { get; set; } = 0;
        public int User2UnreadCount { get; set; } = 0;

        // Trạng thái ẩn/ghim/đã lưu cho từng người dùng
        public bool IsArchivedByUser1 { get; set; } = false;
        public bool IsArchivedByUser2 { get; set; } = false;

        public bool IsMutedByUser1 { get; set; } = false;
        public bool IsMutedByUser2 { get; set; } = false;

        // Trạng thái khóa cuộc trò chuyện
        public bool IsBlocked { get; set; } = false;
        [MaxLength(450)]
        public string? BlockedByUserId { get; set; }
        public DateTime? BlockedAt { get; set; }

        [ForeignKey(nameof(User1Id))]
        public User? User1 { get; set; }

        [ForeignKey(nameof(User2Id))]
        public User? User2 { get; set; }

        public ICollection<Message>? Messages { get; set; }
    }
}