using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Repositories;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _context;

        public MessageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Message?> GetByIdAsync(Guid messageId)
        {
            return await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.ReplyToMessage)
                .Include(m => m.Order)
                .Include(m => m.Product)
                    .ThenInclude(p => p.Images)
                .Include(m => m.Product)
                    .ThenInclude(p => p.Shop)
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }

        public async Task<List<Message>> GetMessagesAsync(Guid conversationId, int skip = 0, int take = 50)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(m => m.Sender)
                .Include(m => m.ReplyToMessage)
                .Include(m => m.Order)
                .Include(m => m.Product)
                    .ThenInclude(p => p.Images)
                .Include(m => m.Product)
                    .ThenInclude(p => p.Shop)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Message>> GetUnreadMessagesAsync(Guid conversationId, string userId, DateTime? upTo = null)
        {
            var query = _context.Messages
                .Where(m => m.ConversationId == conversationId && !m.IsRead && m.SenderId != userId);

            if (upTo.HasValue)
            {
                query = query.Where(m => m.CreatedAt <= upTo.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<Message> AddAsync(Message message)
        {
            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task UpdateAsync(Message message)
        {
            _context.Messages.Update(message);
            await _context.SaveChangesAsync();
        }

        public async Task<int> CountUnreadAsync(Guid conversationId, string userId)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId && !m.IsRead && m.SenderId != userId)
                .CountAsync();
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}

