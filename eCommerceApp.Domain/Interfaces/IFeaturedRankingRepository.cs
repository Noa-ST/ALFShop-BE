using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Domain.Interfaces
{
    public interface IFeaturedRankingRepository : IGeneric<FeaturedRanking>
    {
        Task<int> AddRankingAsync(FeaturedRanking ranking);

        Task<FeaturedRanking?> GetLatestByEntityAsync(string entityType, Guid entityId);

        Task<List<FeaturedRanking>> GetLatestScoresAsync(string entityType, int limit = 20);
    }
}