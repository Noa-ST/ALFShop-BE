using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Domain.Interfaces
{
    public interface IFeaturedEventRepository : IGeneric<FeaturedEvent>
    {
        Task<int> AddEventAsync(FeaturedEvent ev);

        Task<(int Clicks, int Impressions, int AddsToCart)> GetTotalsAsync(
            string? entityType = null,
            DateTime? from = null,
            DateTime? to = null);

        Task<List<(Guid EntityId, int Clicks, int Impressions, int AddsToCart)>> GetTopEntitiesAsync(
            string entityType,
            DateTime? from,
            DateTime? to,
            int topN = 10);

        // New: totals for a specific entity
        Task<(int Clicks, int Impressions, int AddsToCart)> GetTotalsForEntityAsync(
            string entityType,
            Guid entityId,
            DateTime? from = null,
            DateTime? to = null);

        // New: paginated top entities
        Task<(List<(Guid EntityId, int Clicks, int Impressions, int AddsToCart)> Items, int TotalCount)> GetTopEntitiesPagedAsync(
            string entityType,
            DateTime? from,
            DateTime? to,
            int page = 1,
            int pageSize = 10);
    }
}