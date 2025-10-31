using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Chat;

namespace eCommerceApp.Application.Services.Interfaces
{
    public interface IConversationService
    {
        Task<ServiceResponse<List<ConversationSummaryDto>>> GetConversationsAsync(string currentUserId, int page = 1, int pageSize = 20);
        Task<ServiceResponse<ConversationDetailDto>> GetConversationAsync(string currentUserId, Guid conversationId, int messagePage = 1, int pageSize = 50);
        Task<ServiceResponse<ConversationSummaryDto>> CreateConversationAsync(string currentUserId, CreateConversationRequest request);
        Task<ServiceResponse<MessageDto>> SendMessageAsync(string currentUserId, Guid conversationId, SendMessageRequest request);
        Task<ServiceResponse<bool>> MarkMessagesReadAsync(string currentUserId, Guid conversationId, MarkMessagesReadRequest request);
        Task<ServiceResponse<bool>> UpdatePreferencesAsync(string currentUserId, Guid conversationId, UpdateConversationPreferenceRequest request);
    }
}

