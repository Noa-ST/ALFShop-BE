using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class FeaturedEventRepository(AppDbContext context) : GenericRepository<FeaturedEvent>(context), IFeaturedEventRepository
    {
        public async Task<int> AddEventAsync(FeaturedEvent ev)
        {
            await context.FeaturedEvents.AddAsync(ev);
            return 1;
        }

        public async Task<(int Clicks, int Impressions, int AddsToCart)> GetTotalsAsync(string? entityType = null, DateTime? from = null, DateTime? to = null)
        {
            var query = context.FeaturedEvents.AsNoTracking().Where(e => !e.IsDeleted);
            if (!string.IsNullOrWhiteSpace(entityType))
                query = query.Where(e => e.EntityType == entityType);
            if (from.HasValue)
                query = query.Where(e => e.CreatedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(e => e.CreatedAt <= to.Value);

            var clicks = await query.CountAsync(e => e.EventType == "click");
            var impressions = await query.CountAsync(e => e.EventType == "impression");
            var addToCart = await query.CountAsync(e => e.EventType == "add_to_cart");
            return (clicks, impressions, addToCart);
        }

        public async Task<List<(Guid EntityId, int Clicks, int Impressions, int AddsToCart)>> GetTopEntitiesAsync(
            string entityType,
            DateTime? from,
            DateTime? to,
            int topN = 10)
        {
            var query = context.FeaturedEvents.AsNoTracking().Where(e => !e.IsDeleted && e.EntityType == entityType);
            if (from.HasValue)
                query = query.Where(e => e.CreatedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(e => e.CreatedAt <= to.Value);

            var grouped = await query
                .GroupBy(e => e.EntityId)
                .Select(g => new
                {
                    EntityId = g.Key,
                    Clicks = g.Count(e => e.EventType == "click"),
                    Impressions = g.Count(e => e.EventType == "impression"),
                    AddsToCart = g.Count(e => e.EventType == "add_to_cart")
                })
                .OrderByDescending(x => x.Clicks + x.AddsToCart)
                .Take(topN)
                .ToListAsync();

            return grouped.Select(x => (x.EntityId, x.Clicks, x.Impressions, x.AddsToCart)).ToList();
        }

        public async Task<(int Clicks, int Impressions, int AddsToCart)> GetTotalsForEntityAsync(
            string entityType,
            Guid entityId,
            DateTime? from = null,
            DateTime? to = null)
        {
            var query = context.FeaturedEvents.AsNoTracking()
                .Where(e => !e.IsDeleted && e.EntityType == entityType && e.EntityId == entityId);
            if (from.HasValue)
                query = query.Where(e => e.CreatedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(e => e.CreatedAt <= to.Value);

            var clicks = await query.CountAsync(e => e.EventType == "click");
            var impressions = await query.CountAsync(e => e.EventType == "impression");
            var addToCart = await query.CountAsync(e => e.EventType == "add_to_cart");
            return (clicks, impressions, addToCart);
        }

        public async Task<(List<(Guid EntityId, int Clicks, int Impressions, int AddsToCart)> Items, int TotalCount)> GetTopEntitiesPagedAsync(
            string entityType,
            DateTime? from,
            DateTime? to,
            int page = 1,
            int pageSize = 10)
        {
            var query = context.FeaturedEvents.AsNoTracking().Where(e => !e.IsDeleted && e.EntityType == entityType);
            if (from.HasValue)
                query = query.Where(e => e.CreatedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(e => e.CreatedAt <= to.Value);

            var groupedQuery = query
                .GroupBy(e => e.EntityId)
                .Select(g => new
                {
                    EntityId = g.Key,
                    Clicks = g.Count(e => e.EventType == "click"),
                    Impressions = g.Count(e => e.EventType == "impression"),
                    AddsToCart = g.Count(e => e.EventType == "add_to_cart")
                })
                .OrderByDescending(x => x.Clicks + x.AddsToCart);

            int totalCount = await groupedQuery.CountAsync();
            var itemsRaw = await groupedQuery
                .Skip((Math.Max(1, page) - 1) * Math.Max(1, pageSize))
                .Take(Math.Max(1, pageSize))
                .ToListAsync();

            var items = itemsRaw.Select(x => (x.EntityId, x.Clicks, x.Impressions, x.AddsToCart)).ToList();
            return (items, totalCount);
        }
    }
}