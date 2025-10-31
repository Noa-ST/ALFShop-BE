using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Repositories;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly AppDbContext _context;

        public ConversationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Conversation?> GetByIdAsync(Guid conversationId, bool includeMessages = false)
        {
            IQueryable<Conversation> query = _context.Conversations.AsQueryable();

            if (includeMessages)
            {
                query = query
                    .Include(c => c.Messages!)
                        .ThenInclude(m => m.Sender)
                    .Include(c => c.Messages)
                        .ThenInclude(m => m.ReplyToMessage)
                    .Include(c => c.Messages)
                        .ThenInclude(m => m.Order)
                    .Include(c => c.Messages)
                        .ThenInclude(m => m.Product)
                            .ThenInclude(p => p.Images)
                    .Include(c => c.Messages)
                        .ThenInclude(m => m.Product)
                            .ThenInclude(p => p.Shop);
            }

            return await query.FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task<Conversation?> GetBetweenUsersAsync(string userId1, string userId2)
        {
            return await _context.Conversations
                .FirstOrDefaultAsync(c =>
                    (c.User1Id == userId1 && c.User2Id == userId2) ||
                    (c.User1Id == userId2 && c.User2Id == userId1));
        }

        public async Task<List<Conversation>> GetUserConversationsAsync(string userId, int skip = 0, int take = 20)
        {
            return await _context.Conversations
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .OrderByDescending(c => c.LastMessageAt)
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Conversation> AddAsync(Conversation conversation)
        {
            await _context.Conversations.AddAsync(conversation);
            await _context.SaveChangesAsync();
            return conversation;
        }

        public async Task UpdateAsync(Conversation conversation)
        {
            _context.Conversations.Update(conversation);
            await _context.SaveChangesAsync();
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}

