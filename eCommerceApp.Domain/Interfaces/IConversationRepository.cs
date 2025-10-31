using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Domain.Repositories
{
    public interface IConversationRepository
    {
        Task<Conversation?> GetByIdAsync(Guid conversationId, bool includeMessages = false);
        Task<Conversation?> GetBetweenUsersAsync(string userId1, string userId2);
        Task<List<Conversation>> GetUserConversationsAsync(string userId, int skip = 0, int take = 20);
        Task<Conversation> AddAsync(Conversation conversation);
        Task UpdateAsync(Conversation conversation);
        Task SaveChangesAsync();
    }
}

