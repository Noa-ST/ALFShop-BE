using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Chat;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IConversationService
    {
        Task<ServiceResponse<PagedResult<ConversationSummaryDto>>> GetConversationsAsync(string currentUserId, int page = 1, int pageSize = 20);
        Task<ServiceResponse<ConversationDetailDto>> GetConversationAsync(string currentUserId, Guid conversationId, int messagePage = 1, int pageSize = 50);
        Task<ServiceResponse<ConversationSummaryDto>> CreateConversationAsync(string currentUserId, CreateConversationRequest request);
        Task<ServiceResponse<MessageDto>> SendMessageAsync(string currentUserId, Guid conversationId, SendMessageRequest request);
        Task<ServiceResponse<bool>> MarkMessagesReadAsync(string currentUserId, Guid conversationId, MarkMessagesReadRequest request);
        Task<ServiceResponse<bool>> UpdatePreferencesAsync(string currentUserId, Guid conversationId, UpdateConversationPreferenceRequest request);
        
        // ✅ New: Edit Message
        Task<ServiceResponse<MessageDto>> EditMessageAsync(string currentUserId, Guid messageId, EditMessageRequest request);
        
        // ✅ New: Delete Message
        Task<ServiceResponse<bool>> DeleteMessageAsync(string currentUserId, Guid messageId);
        
        // ✅ New: Delete Conversation
        Task<ServiceResponse<bool>> DeleteConversationAsync(string currentUserId, Guid conversationId);
        
        // ✅ New: Search Messages
        Task<ServiceResponse<PagedResult<MessageDto>>> SearchMessagesAsync(string currentUserId, Guid conversationId, string keyword, int page = 1, int pageSize = 50);
        
        // ✅ New: Search Conversations
        Task<ServiceResponse<PagedResult<ConversationSummaryDto>>> SearchConversationsAsync(string currentUserId, string keyword, int page = 1, int pageSize = 20);
        
        // ✅ New: Get Total Unread Count
        Task<ServiceResponse<int>> GetTotalUnreadCountAsync(string currentUserId);
    }
}

