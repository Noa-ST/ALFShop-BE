using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Featured;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class FeaturedService : IFeaturedService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMemoryCache _cache;

        public FeaturedService(IUnitOfWork uow, IMemoryCache cache)
        {
            _uow = uow;
            _cache = cache;
        }

        public async Task<IEnumerable<FeaturedCategoryDto>> GetFeaturedCategoriesAsync(int limit, string? region)
        {
            string key = $"featured:categories:{region}:{limit}";
            if (_cache.TryGetValue(key, out IEnumerable<FeaturedCategoryDto>? cached) && cached != null)
                return cached;

            var cats = await _uow.GlobalCategories.GetAllAsync();
            var scored = cats
                .Where(c => !c.IsDeleted)
                .Select(c =>
                {
                    double pinBoost = ComputePinBoost(c.IsPinned, c.FeaturedWeight, c.PinnedUntil);
                    double score = pinBoost + Normalize(c.Products?.Count ?? 0);
                    return new { c, score, pinBoost };
                })
                .OrderByDescending(x => x.c.IsPinned) // ưu tiên item ghim
                .ThenByDescending(x => x.score)
                .Take(limit)
                .Select(x => new FeaturedCategoryDto
                {
                    Id = x.c.Id,
                    Name = x.c.Name,
                    IsPinned = x.c.IsPinned,
                    FeaturedWeight = x.c.FeaturedWeight,
                    RankingScore = x.score
                })
                .ToList();

            _cache.Set(key, scored, TimeSpan.FromMinutes(5));
            return scored;
        }

        public async Task<IEnumerable<FeaturedShopDto>> GetFeaturedShopsAsync(int limit, string? city)
        {
            string key = $"featured:shops:{city}:{limit}";
            if (_cache.TryGetValue(key, out IEnumerable<FeaturedShopDto>? cached) && cached != null)
                return cached;

            var shops = await _uow.Shops.GetAllActiveAsync();
            if (!string.IsNullOrWhiteSpace(city))
                shops = shops.Where(s => s.City.Equals(city, StringComparison.OrdinalIgnoreCase));

            // tính normalization theo max
            float maxRating = Math.Max(1f, shops.Max(s => s.AverageRating));
            int maxReviews = Math.Max(1, shops.Max(s => s.ReviewCount));
            float maxFulfill = Math.Max(1f, shops.Max(s => s.FulfilledRate));
            float maxReturn = Math.Max(1f, shops.Max(s => s.ReturnRate));

            var scored = shops
                .Select(s =>
                {
                    double pinBoost = ComputePinBoost(s.IsPinned, s.FeaturedWeight, s.PinnedUntil);
                    double ratingComp = 0.4 * (s.AverageRating / maxRating) + 0.2 * ((float)s.ReviewCount / maxReviews);
                    double fulfillComp = 0.3 * (s.FulfilledRate / maxFulfill);
                    double returnPenalty = 0.2 * (s.ReturnRate / maxReturn);
                    double onlineBoost = s.OnlineStatus ? 0.1 : 0.0;

                    double score = pinBoost + ratingComp + fulfillComp + onlineBoost - returnPenalty;

                    return new { s, score, pinBoost, ratingComp, fulfillComp, returnPenalty, onlineBoost };
                })
                .OrderByDescending(x => x.s.IsPinned)
                .ThenByDescending(x => x.score)
                .Take(limit)
                .Select(x => new FeaturedShopDto
                {
                    Id = x.s.Id,
                    Name = x.s.Name,
                    City = x.s.City,
                    AverageRating = x.s.AverageRating,
                    ReviewCount = x.s.ReviewCount,
                    OnlineStatus = x.s.OnlineStatus,
                    FulfilledRate = x.s.FulfilledRate,
                    ReturnRate = x.s.ReturnRate,
                    IsPinned = x.s.IsPinned,
                    FeaturedWeight = x.s.FeaturedWeight,
                    RankingScore = x.score
                })
                .ToList();

            _cache.Set(key, scored, TimeSpan.FromMinutes(5));
            return scored;
        }

        public async Task<IEnumerable<FeaturedProductDto>> GetFeaturedProductsAsync(int limit, Guid? categoryId, decimal? priceMin, decimal? priceMax)
        {
            string key = $"featured:products:{categoryId}:{priceMin}:{priceMax}:{limit}";
            if (_cache.TryGetValue(key, out IEnumerable<FeaturedProductDto>? cached) && cached != null)
                return cached;

            var all = await _uow.Products.GetAllAsync();
            var products = all.Where(p => !p.IsDeleted && p.StockQuantity > 0);

            if (categoryId.HasValue && categoryId.Value != Guid.Empty)
                products = products.Where(p => p.GlobalCategoryId == categoryId.Value);

            if (priceMin.HasValue)
                products = products.Where(p => p.Price >= priceMin.Value);

            if (priceMax.HasValue)
                products = products.Where(p => p.Price <= priceMax.Value);

            // max để normalize
            int maxViews = Math.Max(1, products.Max(p => p.Views7d));
            int maxAdds = Math.Max(1, products.Max(p => p.AddsToCart7d));
            int maxSold = Math.Max(1, products.Max(p => p.Sold7d));
            float maxRating = Math.Max(1f, products.Max(p => p.AverageRating));
            float lambda = 0.02f; // time-decay
            DateTime now = DateTime.UtcNow;

            // đa dạng hóa theo shop
            var scoredRaw = products.Select(p =>
            {
                double pinBoost = ComputePinBoost(p.IsPinned, p.FeaturedWeight, p.PinnedUntil);

                double v = (double)p.Views7d / maxViews;
                double a = (double)p.AddsToCart7d / maxAdds;
                double s = (double)p.Sold7d / maxSold;
                double r = (double)p.AverageRating / maxRating;

                double ageDays = (now - p.CreatedAt).TotalDays;
                double decay = Math.Exp(-lambda * ageDays);

                double baseScore = 0.2 * v * decay + 0.3 * a * decay + 0.4 * s * decay + 0.1 * r;
                double outOfStockPenalty = p.StockQuantity <= 0 ? 0.5 : 0.0;

                double score = pinBoost + baseScore - outOfStockPenalty;

                return new { p, score, pinBoost, baseScore, outOfStockPenalty, shopId = p.ShopId };
            }).OrderByDescending(x => x.p.IsPinned)
              .ThenByDescending(x => x.score)
              .ToList();

            // diversity penalty by shop
            var perShopCount = new Dictionary<Guid, int>();
            var final = new List<(Product p, double score)>();

            foreach (var x in scoredRaw)
            {
                perShopCount.TryGetValue(x.shopId, out int count);
                double diversityPenalty = count > 0 ? count * 0.05 : 0.0;
                double finalScore = x.score - diversityPenalty;

                final.Add((x.p, finalScore));
                perShopCount[x.shopId] = count + 1;
            }

            var result = final
                .OrderByDescending(t => t.p.IsPinned)
                .ThenByDescending(t => t.score)
                .Take(limit)
                .Select(t => new FeaturedProductDto
                {
                    Id = t.p.Id,
                    ShopId = t.p.ShopId,
                    GlobalCategoryId = t.p.GlobalCategoryId,
                    Name = t.p.Name,
                    Price = t.p.Price,
                    DiscountPercent = t.p.DiscountPercent,
                    StockQuantity = t.p.StockQuantity,
                    AverageRating = t.p.AverageRating,
                    ReviewCount = t.p.ReviewCount,
                    IsPinned = t.p.IsPinned,
                    FeaturedWeight = t.p.FeaturedWeight,
                    RankingScore = t.score
                })
                .ToList();

            _cache.Set(key, result, TimeSpan.FromMinutes(5));
            return result;
        }

        public async Task<ServiceResponse<bool>> PinAsync(FeaturedPinRequest request)
        {
            try
            {
                if (string.Equals(request.Type, "product", StringComparison.OrdinalIgnoreCase))
                {
                    var entity = await _uow.Products.GetByIdAsync(request.Id);
                    if (entity == null) return ServiceResponse<bool>.Fail("Product not found", System.Net.HttpStatusCode.NotFound);

                    entity.IsPinned = true;
                    entity.FeaturedWeight = request.Weight;
                    entity.PinnedUntil = request.ExpiresAt;
                    await _uow.Products.UpdateAsync(entity);
                }
                else if (string.Equals(request.Type, "shop", StringComparison.OrdinalIgnoreCase))
                {
                    var entity = await _uow.Shops.GetByIdAsync(request.Id);
                    if (entity == null) return ServiceResponse<bool>.Fail("Shop not found", System.Net.HttpStatusCode.NotFound);

                    entity.IsPinned = true;
                    entity.FeaturedWeight = request.Weight;
                    entity.PinnedUntil = request.ExpiresAt;
                    await _uow.Shops.UpdateAsync(entity);
                }
                else if (string.Equals(request.Type, "category", StringComparison.OrdinalIgnoreCase))
                {
                    var entity = await _uow.GlobalCategories.GetByIdAsync(request.Id);
                    if (entity == null) return ServiceResponse<bool>.Fail("Category not found", System.Net.HttpStatusCode.NotFound);

                    entity.IsPinned = true;
                    entity.FeaturedWeight = request.Weight;
                    entity.PinnedUntil = request.ExpiresAt;
                    await _uow.GlobalCategories.UpdateAsync(entity);
                }
                else
                {
                    return ServiceResponse<bool>.Fail("Invalid type", System.Net.HttpStatusCode.BadRequest);
                }

                await _uow.SaveChangesAsync();
                return ServiceResponse<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.Fail(ex.Message, System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<IEnumerable<FeaturedScoreDebugDto>>> GetDebugScoresAsync(int topN = 20)
        {
            var products = await GetFeaturedProductsAsync(topN, null, null, null);
            var shops = await GetFeaturedShopsAsync(Math.Min(topN, 10), null);
            var cats = await GetFeaturedCategoriesAsync(Math.Min(topN, 10), null);

            var list = new List<FeaturedScoreDebugDto>();
            list.AddRange(products.Select(p => new FeaturedScoreDebugDto
            {
                Type = "product",
                Id = p.Id,
                Score = p.RankingScore,
                PinBoost = p.IsPinned ? (p.FeaturedWeight ?? 1) * 100.0 : 0.0,
                MetricComponent1 = p.AverageRating,
                MetricComponent2 = p.DiscountPercent,
                MetricComponent3 = p.StockQuantity,
                RatingComponent = p.AverageRating,
                PenaltyComponent = p.StockQuantity <= 0 ? 0.5 : 0.0
            }));
            list.AddRange(shops.Select(s => new FeaturedScoreDebugDto
            {
                Type = "shop",
                Id = s.Id,
                Score = s.RankingScore,
                PinBoost = s.IsPinned ? (s.FeaturedWeight ?? 1) * 100.0 : 0.0,
                MetricComponent1 = s.AverageRating,
                MetricComponent2 = s.FulfilledRate,
                MetricComponent3 = s.ReturnRate,
                RatingComponent = s.AverageRating,
                PenaltyComponent = s.ReturnRate
            }));
            list.AddRange(cats.Select(c => new FeaturedScoreDebugDto
            {
                Type = "category",
                Id = c.Id,
                Score = c.RankingScore,
                PinBoost = c.IsPinned ? (c.FeaturedWeight ?? 1) * 100.0 : 0.0,
                MetricComponent1 = c.RankingScore,
                MetricComponent2 = 0,
                MetricComponent3 = 0,
                RatingComponent = 0,
                PenaltyComponent = 0
            }));

            return ServiceResponse<IEnumerable<FeaturedScoreDebugDto>>.Success(list);
        }

        private static double Normalize(double value, double max = 0)
        {
            if (max <= 0) max = Math.Max(1, value);
            return value / max;
        }

        private static double ComputePinBoost(bool isPinned, double? featuredWeight, DateTime? pinnedUntil)
        {
            if (!isPinned) return 0.0;
            if (pinnedUntil.HasValue && pinnedUntil.Value < DateTime.UtcNow) return 0.0;
            return 100.0 * (featuredWeight ?? 1.0);
        }
    }
}