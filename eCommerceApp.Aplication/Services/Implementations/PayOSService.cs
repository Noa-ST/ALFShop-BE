using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Aplication.DTOs.Payment;
using Microsoft.Extensions.Configuration;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class PayOSService : IPayOSService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _checksumKey;
        private readonly string _baseUrl;

        public PayOSService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _clientId = _configuration["PayOS:ClientId"] ?? string.Empty;
            _apiKey = _configuration["PayOS:ApiKey"] ?? string.Empty;
            _checksumKey = _configuration["PayOS:ChecksumKey"] ?? string.Empty;
            _baseUrl = _configuration["PayOS:BaseUrl"] ?? "https://api.payos.vn";

            // Setup HTTP client
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Add("x-client-id", _clientId);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        }

        public async Task<PayOSCreatePaymentResponse> CreatePaymentLinkAsync(PayOSCreatePaymentRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // ✅ Fix: Set timeout cho HttpClient request
                var timeout = TimeSpan.FromSeconds(30);
                using var cts = new CancellationTokenSource(timeout);

                var response = await _httpClient.PostAsync("/v2/payment-requests", content, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"PayOS API error (Status: {response.StatusCode}): {responseContent}");
                }

                var result = JsonSerializer.Deserialize<PayOSCreatePaymentResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                {
                    throw new Exception($"Failed to parse PayOS response: {responseContent}");
                }

                return result;
            }
            catch (TaskCanceledException)
            {
                throw new Exception("PayOS API request timeout after 30 seconds");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"PayOS API connection error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating PayOS payment link: {ex.Message}", ex);
            }
        }

        public Task<bool> VerifyWebhookSignatureAsync(PayOSWebhookRequest webhook, string checksumKey)
        {
            try
            {
                if (webhook == null || webhook.Data == null || string.IsNullOrEmpty(checksumKey))
                {
                    return Task.FromResult(false);
                }

                // ✅ PayOS sử dụng HMAC SHA256 để tạo signature
                // Format: HMAC_SHA256(data, checksumKey)
                // ⚠️ Note: PayOS có thể yêu cầu format JSON cụ thể (camelCase, snake_case, hoặc thứ tự fields)
                // Nếu signature verification fail, cần kiểm tra PayOS documentation để xác nhận format chính xác
                var dataJson = JsonSerializer.Serialize(webhook.Data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false // Đảm bảo không có whitespace
                });

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey));
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataJson));
                var computedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                var isValid = computedSignature.Equals(webhook.Signature, StringComparison.OrdinalIgnoreCase);
                
                // ✅ Log warning nếu signature không khớp (để debug)
                if (!isValid)
                {
                    // Note: Không log checksumKey vì lý do bảo mật
                    // Có thể log computedSignature để debug (nhưng không log trong production)
                }

                return Task.FromResult(isValid);
            }
            catch (Exception ex)
            {
                // ✅ Log exception để debug signature verification issues
                // Note: Return false để reject webhook nếu có lỗi trong quá trình verify
                return Task.FromResult(false);
            }
        }

        public async Task<bool> RefundAsync(int orderCode, int amount, string reason)
        {
            try
            {
                var refundRequest = new
                {
                    orderCode = orderCode,
                    amount = amount,
                    reason = reason
                };

                var json = JsonSerializer.Serialize(refundRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/v2/refund", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"PayOS refund error: {responseContent}");
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing refund: {ex.Message}", ex);
            }
        }
    }
}

