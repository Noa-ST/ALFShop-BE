using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace eCommerceApp.Infrastructure.Midleware
{
    /// <summary>
    /// Simple rate limiting middleware để giới hạn số lượng requests từ cùng một IP
    /// Sử dụng sliding window để theo dõi requests
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly int _maxRequests;
        private readonly TimeSpan _timeWindow;
        private readonly ConcurrentDictionary<string, Queue<DateTime>> _requestHistory = new();

        public RateLimitMiddleware(RequestDelegate next)
        {
            _next = next;
            // Default: 5 requests per 60 seconds for authentication endpoints
            _maxRequests = 5;
            _timeWindow = TimeSpan.FromSeconds(60);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Chỉ áp dụng rate limiting cho login và create endpoints
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (!path.Contains("/api/authencation/login") && !path.Contains("/api/authencation/create"))
            {
                await _next(context);
                return;
            }

            var clientIp = GetClientIp(context);
            var now = DateTime.UtcNow;

            // Cleanup old entries
            CleanupOldEntries(now);

            // Check rate limit
            if (!_requestHistory.TryGetValue(clientIp, out var requestQueue))
            {
                requestQueue = new Queue<DateTime>();
                _requestHistory[clientIp] = requestQueue;
            }

            // Remove requests outside the time window
            while (requestQueue.Count > 0 && now - requestQueue.Peek() > _timeWindow)
            {
                requestQueue.Dequeue();
            }

            // Check if limit exceeded
            if (requestQueue.Count >= _maxRequests)
            {
                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        succeeded = false,
                        message = $"Rate limit exceeded. Maximum {_maxRequests} requests per {_timeWindow.TotalSeconds} seconds allowed."
                    })
                );
                return;
            }

            // Add current request
            requestQueue.Enqueue(now);

            await _next(context);
        }

        private string GetClientIp(HttpContext context)
        {
            // Check for forwarded IP (when behind proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',').FirstOrDefault()?.Trim() ?? "unknown";
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private void CleanupOldEntries(DateTime now)
        {
            var keysToRemove = new List<string>();
            foreach (var kvp in _requestHistory)
            {
                var queue = kvp.Value;
                while (queue.Count > 0 && now - queue.Peek() > _timeWindow)
                {
                    queue.Dequeue();
                }

                // Remove empty queues
                if (queue.Count == 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _requestHistory.TryRemove(key, out _);
            }
        }
    }
}

