using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Domain.Repositories
{
    public interface IMessageRepository
    {
        Task<Message?> GetByIdAsync(Guid messageId);
        Task<List<Message>> GetMessagesAsync(Guid conversationId, int skip = 0, int take = 50);
        Task<List<Message>> GetUnreadMessagesAsync(Guid conversationId, string userId, DateTime? upTo = null);
        Task<Message> AddAsync(Message message);
        Task UpdateAsync(Message message);
        Task<int> CountUnreadAsync(Guid conversationId, string userId);
        Task SaveChangesAsync();
    }
}

