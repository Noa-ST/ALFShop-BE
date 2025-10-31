namespace eCommerceApp.Aplication.DTOs.Chat
{
    public class ConversationDetailDto
    {
        public Guid ConversationId { get; set; }
        public string User1Id { get; set; } = null!;
        public string User2Id { get; set; } = null!;
        public DateTime LastMessageAt { get; set; }
        public string? LastMessageContent { get; set; }
        public string? LastMessageSenderId { get; set; }
        public int User1UnreadCount { get; set; }
        public int User2UnreadCount { get; set; }
        public bool IsArchivedByUser1 { get; set; }
        public bool IsArchivedByUser2 { get; set; }
        public bool IsMutedByUser1 { get; set; }
        public bool IsMutedByUser2 { get; set; }
        public bool IsBlocked { get; set; }
        public string? BlockedByUserId { get; set; }
        public DateTime? BlockedAt { get; set; }
        public IEnumerable<MessageDto> Messages { get; set; } = Enumerable.Empty<MessageDto>();
    }
}

