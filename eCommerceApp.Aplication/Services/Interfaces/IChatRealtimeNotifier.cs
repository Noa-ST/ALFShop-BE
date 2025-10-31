using eCommerceApp.Aplication.DTOs.Chat;

namespace eCommerceApp.Application.Services.Interfaces
{
    public interface IChatRealtimeNotifier
    {
        Task BroadcastMessageAsync(MessageDto message, ConversationSummaryDto summaryForSender, ConversationSummaryDto summaryForReceiver);
        Task BroadcastMessagesReadAsync(Guid conversationId, string readerId, int user1UnreadCount, int user2UnreadCount, IReadOnlyCollection<Guid> messageIds);
    }
}

