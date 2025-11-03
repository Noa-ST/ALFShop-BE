using eCommerceApp.Domain.Interfaces.Authentication;
using eCommerceApp.Aplication.Services.Interfaces.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace eCommerceApp.Infrastructure.Services
{
    /// <summary>
    /// Background service để tự động cleanup các refresh tokens đã hết hạn hoặc bị revoke
    /// Chạy mỗi 24 giờ một lần để dọn dẹp database
    /// </summary>
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Chạy mỗi 24 giờ

        public RefreshTokenCleanupService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Create initial scope for startup log
            using (var startupScope = _serviceScopeFactory.CreateScope())
            {
                var startupLogger = startupScope.ServiceProvider.GetRequiredService<IAppLogger<RefreshTokenCleanupService>>();
                startupLogger.LogInformation("RefreshTokenCleanupService started");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Create a scope to resolve scoped services
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var logger = scope.ServiceProvider.GetRequiredService<IAppLogger<RefreshTokenCleanupService>>();
                        var tokenManagement = scope.ServiceProvider.GetRequiredService<ITokenManagement>();
                        
                        logger.LogInformation("Starting refresh token cleanup...");
                        var deletedCount = await tokenManagement.CleanupExpiredTokens();
                        
                        if (deletedCount > 0)
                        {
                            logger.LogInformation($"Cleanup completed. Deleted {deletedCount} expired/revoked refresh tokens.");
                        }
                        else
                        {
                            logger.LogInformation("Cleanup completed. No expired tokens found.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Create scope for error logging
                    using (var errorScope = _serviceScopeFactory.CreateScope())
                    {
                        var errorLogger = errorScope.ServiceProvider.GetRequiredService<IAppLogger<RefreshTokenCleanupService>>();
                        errorLogger.LogError(ex, "Error occurred during refresh token cleanup");
                    }
                }

                // Đợi 24 giờ trước khi chạy lại
                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            // Create scope for shutdown log
            using (var shutdownScope = _serviceScopeFactory.CreateScope())
            {
                var shutdownLogger = shutdownScope.ServiceProvider.GetRequiredService<IAppLogger<RefreshTokenCleanupService>>();
                shutdownLogger.LogInformation("RefreshTokenCleanupService stopped");
            }
        }
    }
}

