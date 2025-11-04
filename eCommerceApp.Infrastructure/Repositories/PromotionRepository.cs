using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly AppDbContext _context;

        public PromotionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Promotion promotion)
        {
            await _context.Promotions.AddAsync(promotion);
        }

        public void Delete(Promotion promotion)
        {
            _context.Promotions.Remove(promotion);
        }

        public async Task<IEnumerable<Promotion>> GetActivePromotionsAsync()
        {
            return await _context.Promotions
                .Where(p => p.IsActive && p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<Promotion> GetByCodeAsync(string code)
        {
            // Thêm AsNoTracking() để tối ưu việc chỉ đọc
            return await _context.Promotions.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == code);
        }

        public async Task<Promotion> GetByIdAsync(Guid id)
        {
            return await _context.Promotions.FindAsync(id);
        }

        public void Update(Promotion promotion)
        {
            _context.Promotions.Update(promotion);
        }
    }
}