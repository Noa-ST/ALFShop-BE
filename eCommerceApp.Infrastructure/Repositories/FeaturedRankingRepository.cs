using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class FeaturedRankingRepository : GenericRepository<FeaturedRanking>, IFeaturedRankingRepository
    {
        private readonly AppDbContext _context;

        public FeaturedRankingRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<int> AddRankingAsync(FeaturedRanking ranking)
        {
            await _context.FeaturedRankings.AddAsync(ranking);
            return 1;
        }

        public async Task<FeaturedRanking?> GetLatestByEntityAsync(string entityType, Guid entityId)
        {
            return await _context.FeaturedRankings
                .AsNoTracking()
                .Where(r => !r.IsDeleted && r.EntityType == entityType && r.EntityId == entityId)
                .OrderByDescending(r => r.ComputedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<FeaturedRanking>> GetLatestScoresAsync(string entityType, int limit = 20)
        {
            // Lấy bản ghi xếp hạng mới nhất cho mỗi entity (max ComputedAt)
            var latestPerEntity = await _context.FeaturedRankings
                .AsNoTracking()
                .Where(r => !r.IsDeleted && r.EntityType == entityType)
                .GroupBy(r => r.EntityId)
                .Select(g => g.OrderByDescending(x => x.ComputedAt).First())
                .OrderByDescending(r => r.Score)
                .Take(limit)
                .ToListAsync();

            // Đảm bảo trả về List không-null (khắc phục CS8619)
            return latestPerEntity ?? new List<FeaturedRanking>();
        }
    }
}