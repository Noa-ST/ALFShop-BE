namespace eCommerceApp.Aplication.DTOs.Chat
{
    public class ConversationSummaryDto
    {
        public Guid ConversationId { get; set; }
        public string CurrentUserId { get; set; } = null!;
        public string PartnerId { get; set; } = null!;
        public string? PartnerName { get; set; }
        public string? PartnerAvatarUrl { get; set; }
        public string? LastMessageContent { get; set; }
        public string? LastMessageSenderId { get; set; }
        public DateTime LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public bool IsMuted { get; set; }
        public bool IsArchived { get; set; }
        public bool IsBlocked { get; set; }
    }
}

