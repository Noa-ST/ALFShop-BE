using eCommerceApp.Aplication.Services.Interfaces.Logging;
using Microsoft.Extensions.Logging;

namespace eCommerceApp.Infrastructure.Service
{
    /// <summary>
    /// Adapter cho hệ thống logging, implement IAppLogger<T>.
    /// Dùng ILogger<T> nội bộ (có thể là Serilog, ConsoleLogger, v.v...)
    /// để log ra thông tin theo mức độ (Info, Warning, Error).
    /// </summary>
    public class SerilogLoggerAdapter<T>(ILogger<T> logger) : IAppLogger<T>
    {
        public void LogError(Exception ex, string message) => logger.LogError(ex, message);
        public void LogInformation(string message) => logger.LogInformation(message);
        public void LogWarning(string message) => logger.LogWarning(message);
    }
}

