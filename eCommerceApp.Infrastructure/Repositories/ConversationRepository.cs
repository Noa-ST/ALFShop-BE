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
            IQueryable<Conversation> query = _context.Conversations
                .Where(c => !c.IsDeleted)
                .AsQueryable();

            if (includeMessages)
            {
                query = query
                    .Include(c => c.Messages!.Where(m => !m.IsDeleted))
                        .ThenInclude(m => m.Sender)
                    .Include(c => c.Messages!.Where(m => !m.IsDeleted))
                        .ThenInclude(m => m.ReplyToMessage)
                    .Include(c => c.Messages!.Where(m => !m.IsDeleted))
                        .ThenInclude(m => m.Order)
                    .Include(c => c.Messages!.Where(m => !m.IsDeleted))
                        .ThenInclude(m => m.Product!)
                            .ThenInclude(p => p.Images!)
                    .Include(c => c.Messages!.Where(m => !m.IsDeleted))
                        .ThenInclude(m => m.Product!)
                            .ThenInclude(p => p.Shop);
            }

            return await query.FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task<Conversation?> GetBetweenUsersAsync(string userId1, string userId2)
        {
            return await _context.Conversations
                .Where(c => !c.IsDeleted) // ✅ Fix: Filter deleted conversations
                .FirstOrDefaultAsync(c =>
                    (c.User1Id == userId1 && c.User2Id == userId2) ||
                    (c.User1Id == userId2 && c.User2Id == userId1));
        }

        public async Task<List<Conversation>> GetUserConversationsAsync(string userId, int skip = 0, int take = 20)
        {
            return await _context.Conversations
                .Where(c => (c.User1Id == userId || c.User2Id == userId) && !c.IsDeleted) // ✅ Fix: Filter deleted conversations
                .OrderByDescending(c => (c.User1Id == userId ? c.IsPinnedByUser1 : c.IsPinnedByUser2)) // ✅ New: Pin conversations first
                .ThenByDescending(c => c.LastMessageAt) // Then order by last message time
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Conversation> AddAsync(Conversation conversation)
        {
            await _context.Conversations.AddAsync(conversation);
            // ✅ Không tự động save - để UnitOfWork quản lý
            return conversation;
        }

        public async Task UpdateAsync(Conversation conversation)
        {
            _context.Conversations.Update(conversation);
            // ✅ Không tự động save - để UnitOfWork quản lý
            await Task.CompletedTask;
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        // ✅ New: Search conversations
        public async Task<(List<Conversation> Conversations, int TotalCount)> SearchConversationsAsync(
            string userId, 
            string keyword, 
            int skip = 0, 
            int take = 20)
        {
            var query = _context.Conversations
                .Where(c => (c.User1Id == userId || c.User2Id == userId) 
                    && !c.IsDeleted
                    && (c.LastMessageContent != null && c.LastMessageContent.Contains(keyword) ||
                        c.User1 != null && (c.User1.UserName != null && c.User1.UserName.Contains(keyword) || 
                                            c.User1.FullName != null && c.User1.FullName.Contains(keyword)) ||
                        c.User2 != null && (c.User2.UserName != null && c.User2.UserName.Contains(keyword) || 
                                            c.User2.FullName != null && c.User2.FullName.Contains(keyword))));

            var totalCount = await query.CountAsync();

            var conversations = await query
                .OrderByDescending(c => c.LastMessageAt)
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();

            return (conversations, totalCount);
        }

        // ✅ New: Get total count
        public async Task<int> GetUserConversationsCountAsync(string userId)
        {
            return await _context.Conversations
                .Where(c => (c.User1Id == userId || c.User2Id == userId) && !c.IsDeleted)
                .CountAsync();
        }
    }
}

