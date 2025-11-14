using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace eCommerceApp.Infrastructure.Services
{
    public class FeaturedRankingService : BackgroundService
    {
        private readonly ILogger<FeaturedRankingService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _interval;

        public FeaturedRankingService(ILogger<FeaturedRankingService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _interval = TimeSpan.FromMinutes(30);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FeaturedRankingService started.");

            // Initial run
            await SafeComputeAllAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_interval, stoppingToken);
                    await SafeComputeAllAsync(stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during featured ranking computation.");
                }
            }

            _logger.LogInformation("FeaturedRankingService stopped.");
        }

        private async Task SafeComputeAllAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                await ComputeProductRankingsAsync(uow, ct);
                await ComputeShopRankingsAsync(uow, ct);
                await ComputeCategoryRankingsAsync(uow, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Featured ranking computation failed.");
            }
        }

        private async Task ComputeProductRankingsAsync(IUnitOfWork _uow, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var products = await _uow.Products.GetAllAsync();

            foreach (var p in products)
            {
                double pinBoost = (p.IsPinned && (!p.PinnedUntil.HasValue || p.PinnedUntil.Value >= now)) ? 20 : 0;
                double ratingWeight = p.AverageRating * 5 + p.ReviewCount * 0.1;
                double engagementWeight = p.AddsToCart7d * 1.5 + p.Views7d * 0.05 + p.Sold7d * 2;
                double discountWeight = p.DiscountPercent * 0.5;
                double penalty = p.StockQuantity <= 0 ? 10 : 0;

                double score = pinBoost + ratingWeight + engagementWeight + discountWeight - penalty;

                var ranking = new FeaturedRanking
                {
                    EntityType = "product",
                    EntityId = p.Id,
                    Score = score,
                    PinBoost = pinBoost,
                    Metric1 = ratingWeight,
                    Metric2 = engagementWeight,
                    Metric3 = discountWeight,
                    Penalty = penalty,
                    ComputedAt = now
                };

                await _uow.FeaturedRankings.AddRankingAsync(ranking);
            }

            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("Computed {Count} product rankings.", products.Count());
        }

        private async Task ComputeShopRankingsAsync(IUnitOfWork _uow, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var shops = await _uow.Shops.GetAllAsync();

            foreach (var s in shops)
            {
                double pinBoost = (s.IsPinned && (!s.PinnedUntil.HasValue || s.PinnedUntil.Value >= now)) ? 15 : 0;
                double ratingWeight = s.AverageRating * 6 + s.ReviewCount * 0.05;
                double performanceWeight = s.FulfilledRate * 3 - s.ReturnRate * 2 + (s.OnlineStatus ? 5 : 0) - (s.AverageResponseTimeSeconds / 300.0);
                double penalty = 0;

                double score = pinBoost + ratingWeight + performanceWeight - penalty;

                var ranking = new FeaturedRanking
                {
                    EntityType = "shop",
                    EntityId = s.Id,
                    Score = score,
                    PinBoost = pinBoost,
                    Metric1 = ratingWeight,
                    Metric2 = performanceWeight,
                    Metric3 = 0,
                    Penalty = penalty,
                    ComputedAt = now
                };

                await _uow.FeaturedRankings.AddRankingAsync(ranking);
            }

            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("Computed {Count} shop rankings.", shops.Count());
        }

        private async Task ComputeCategoryRankingsAsync(IUnitOfWork _uow, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var categories = await _uow.GlobalCategories.GetAllAsync();

            // For a simple first pass: rank by product count and average product rating in category
            foreach (var c in categories)
            {
                // Pull products per category via Product repository
                var productsInCategory = await _uow.Products.GetByGlobalCategoryIdAsync(c.Id);
                var count = productsInCategory.Count();
                var avgRating = productsInCategory.Any() ? productsInCategory.Average(p => p.AverageRating) : 0.0;
                var avgReviews = productsInCategory.Any() ? productsInCategory.Average(p => p.ReviewCount) : 0.0;

                double pinBoost = (c.IsPinned && (!c.PinnedUntil.HasValue || c.PinnedUntil.Value >= now)) ? 10 : 0;
                double metricWeight = count * 1.0 + avgRating * 4 + avgReviews * 0.02;
                double penalty = 0;
                double score = pinBoost + metricWeight - penalty;

                var ranking = new FeaturedRanking
                {
                    EntityType = "category",
                    EntityId = c.Id,
                    Score = score,
                    PinBoost = pinBoost,
                    Metric1 = metricWeight,
                    Metric2 = 0,
                    Metric3 = 0,
                    Penalty = penalty,
                    ComputedAt = now
                };

                await _uow.FeaturedRankings.AddRankingAsync(ranking);
            }

            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("Computed {Count} category rankings.", categories.Count());
        }
    }
}