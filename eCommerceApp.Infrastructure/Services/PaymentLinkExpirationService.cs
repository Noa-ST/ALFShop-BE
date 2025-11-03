using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Aplication.Services.Interfaces.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace eCommerceApp.Infrastructure.Services
{
    /// <summary>
    /// Background service để tự động expire các payment links đã hết hạn
    /// Chạy mỗi 15 phút một lần để dọn dẹp expired payment links
    /// </summary>
    public class PaymentLinkExpirationService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15); // Check mỗi 15 phút

        public PaymentLinkExpirationService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Create initial scope for startup log
            using (var startupScope = _serviceScopeFactory.CreateScope())
            {
                var startupLogger = startupScope.ServiceProvider.GetRequiredService<IAppLogger<PaymentLinkExpirationService>>();
                startupLogger.LogInformation("PaymentLinkExpirationService started. Will check for expired payment links every 15 minutes.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Create a scope to resolve scoped services
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var logger = scope.ServiceProvider.GetRequiredService<IAppLogger<PaymentLinkExpirationService>>();
                        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                        
                        logger.LogInformation("Starting payment link expiration check...");
                        var result = await paymentService.ExpirePaymentLinksAsync();
                        
                        if (result.Succeeded && result.Data > 0)
                        {
                            logger.LogInformation($"Payment link expiration completed. Expired {result.Data} payment links.");
                        }
                        else if (result.Succeeded)
                        {
                            logger.LogInformation("Payment link expiration completed. No expired payment links found.");
                        }
                        else
                        {
                            logger.LogWarning($"Payment link expiration failed: {result.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Create scope for error logging
                    using (var errorScope = _serviceScopeFactory.CreateScope())
                    {
                        var errorLogger = errorScope.ServiceProvider.GetRequiredService<IAppLogger<PaymentLinkExpirationService>>();
                        errorLogger.LogError(ex, "Error occurred during payment link expiration");
                    }
                }

                // Đợi 15 phút trước khi chạy lại
                await Task.Delay(_checkInterval, stoppingToken);
            }

            // Create scope for shutdown log
            using (var shutdownScope = _serviceScopeFactory.CreateScope())
            {
                var shutdownLogger = shutdownScope.ServiceProvider.GetRequiredService<IAppLogger<PaymentLinkExpirationService>>();
                shutdownLogger.LogInformation("PaymentLinkExpirationService stopped");
            }
        }
    }
}

