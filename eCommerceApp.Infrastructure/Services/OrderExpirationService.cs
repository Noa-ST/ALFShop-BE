using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Aplication.Services.Interfaces.Logging;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace eCommerceApp.Infrastructure.Services
{
    /// <summary>
    /// Background service để tự động hủy các đơn hàng chưa thanh toán sau thời gian nhất định
    /// Chạy mỗi 10 phút một lần để tự động cancel các orders unpaid
    /// Chỉ áp dụng cho online payment methods (Wallet, Bank), không áp dụng cho COD/Cash
    /// </summary>
    public class OrderExpirationService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10); // Check mỗi 10 phút
        private readonly TimeSpan _orderTimeout = TimeSpan.FromMinutes(30); // Orders chưa thanh toán sau 30 phút sẽ bị cancel

        public OrderExpirationService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Create initial scope for startup log
            using (var startupScope = _serviceScopeFactory.CreateScope())
            {
                var startupLogger = startupScope.ServiceProvider.GetRequiredService<IAppLogger<OrderExpirationService>>();
                startupLogger.LogInformation($"OrderExpirationService started. Will auto-cancel unpaid orders older than {_orderTimeout.TotalMinutes} minutes, checking every {_checkInterval.TotalMinutes} minutes.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Create a scope to resolve scoped services
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var logger = scope.ServiceProvider.GetRequiredService<IAppLogger<OrderExpirationService>>();
                        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                        
                        logger.LogInformation("Starting unpaid order cancellation check...");
                        
                        // Lấy các orders chưa thanh toán, loại trừ COD (vì COD có thể thanh toán khi nhận hàng)
                        var unpaidOrders = await orderRepository.GetUnpaidOrdersOlderThanAsync(
                            _orderTimeout, 
                            excludePaymentMethod: PaymentMethod.COD); // Không auto-cancel COD orders
                        
                        int canceledCount = 0;
                        int failedCount = 0;
                        
                        foreach (var order in unpaidOrders)
                        {
                            try
                            {
                                // Cancel order (sẽ tự động restore stock)
                                // System userId = "System" cho auto-cancellation
                                var result = await orderService.CancelOrderAsync(order.Id, "System");
                                
                                if (result.Succeeded)
                                {
                                    canceledCount++;
                                    logger.LogInformation($"Auto-cancelled unpaid order: OrderId={order.Id}, CustomerId={order.CustomerId}, CreatedAt={order.CreatedAt}, PaymentMethod={order.PaymentMethod}");
                                }
                                else
                                {
                                    failedCount++;
                                    logger.LogWarning($"Failed to auto-cancel order: OrderId={order.Id}, Reason={result.Message}");
                                }
                            }
                            catch (Exception ex)
                            {
                                failedCount++;
                                logger.LogError(ex, $"Error canceling order: OrderId={order.Id}, Error={ex.Message}");
                            }
                        }
                        
                        if (canceledCount > 0)
                        {
                            logger.LogInformation($"Unpaid order cancellation completed. Canceled {canceledCount} orders, {failedCount} failed.");
                        }
                        else if (unpaidOrders.Any())
                        {
                            logger.LogWarning($"Unpaid order cancellation completed. Found {unpaidOrders.Count()} unpaid orders but none were canceled successfully. {failedCount} failed.");
                        }
                        else
                        {
                            logger.LogInformation("Unpaid order cancellation completed. No unpaid orders found.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Create scope for error logging
                    using (var errorScope = _serviceScopeFactory.CreateScope())
                    {
                        var errorLogger = errorScope.ServiceProvider.GetRequiredService<IAppLogger<OrderExpirationService>>();
                        errorLogger.LogError(ex, "Error occurred during unpaid order cancellation");
                    }
                }

                // Đợi 10 phút trước khi chạy lại
                await Task.Delay(_checkInterval, stoppingToken);
            }

            // Create scope for shutdown log
            using (var shutdownScope = _serviceScopeFactory.CreateScope())
            {
                var shutdownLogger = shutdownScope.ServiceProvider.GetRequiredService<IAppLogger<OrderExpirationService>>();
                shutdownLogger.LogInformation("OrderExpirationService stopped");
            }
        }
    }
}

