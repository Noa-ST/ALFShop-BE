using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Aplication.Services.Interfaces.Logging;
using eCommerceApp.Aplication.DTOs.Payment;
using Microsoft.Extensions.Configuration;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class PayOSService : IPayOSService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IAppLogger<PayOSService> _logger;
        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _checksumKey;
        private readonly string _organizationId;
        private readonly string _baseUrl;

        public PayOSService(HttpClient httpClient, IConfiguration configuration, IAppLogger<PayOSService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _clientId = _configuration["PayOS:ClientId"] ?? string.Empty;
            _apiKey = _configuration["PayOS:ApiKey"] ?? string.Empty;
            _checksumKey = _configuration["PayOS:ChecksumKey"] ?? string.Empty;
            _organizationId = _configuration["PayOS:OrganizationId"] ?? string.Empty;
            _baseUrl = _configuration["PayOS:BaseUrl"] ?? "https://api-merchant.payos.vn";

            // ✅ Validate configuration
            if (string.IsNullOrEmpty(_clientId))
            {
                _logger.LogWarning("PayOS ClientId is not configured");
            }
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("PayOS ApiKey is not configured");
            }
            if (string.IsNullOrEmpty(_baseUrl))
            {
                _logger.LogWarning("PayOS BaseUrl is not configured");
            }
            if (string.IsNullOrEmpty(_organizationId))
            {
                _logger.LogWarning("PayOS OrganizationId is not configured");
            }

            // Setup HTTP client
            try
            {
                _httpClient.BaseAddress = new Uri(_baseUrl);
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", _clientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                _httpClient.Timeout = TimeSpan.FromSeconds(30);
                
                _logger.LogInformation($"PayOSService initialized: BaseUrl={_baseUrl}, ClientId configured={!string.IsNullOrEmpty(_clientId)}, OrganizationId configured={!string.IsNullOrEmpty(_organizationId)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to initialize PayOSService: BaseUrl={_baseUrl}");
                throw;
            }
        }

  public string CreatePaymentSignature(PayOSCreatePaymentRequest request)
{
    if (request == null) throw new ArgumentNullException(nameof(request));
    if (string.IsNullOrEmpty(_checksumKey))
    {
        _logger.LogWarning("PayOS ChecksumKey is not configured; cannot create signature");
        return string.Empty;
    }

    // ✅ PayOS chỉ yêu cầu 5 trường cơ bản trong signature (KHÔNG bao gồm expiredAt và items)
    // Theo tài liệu PayOS: amount, cancelUrl, description, orderCode, returnUrl
    var dict = new Dictionary<string, object>
    {
        ["amount"] = request.Amount,
        ["cancelUrl"] = request.CancelUrl ?? string.Empty,
        ["description"] = request.Description ?? string.Empty,
        ["orderCode"] = request.OrderCode,
        ["returnUrl"] = request.ReturnUrl ?? string.Empty
    };

    // Sort keys alphabetically (dictionary may not keep order)
    var ordered = dict.OrderBy(kv => kv.Key, StringComparer.Ordinal);

    // Build query string - KHÔNG URL-encode values (theo PayOS docs cho payment-requests)
    var parts = ordered.Select(kv =>
    {
        var value = kv.Value;
        // Convert null/undefined to empty string
        if (value == null)
        {
            value = string.Empty;
        }
        var valueStr = value.ToString();
        
        return $"{kv.Key}={valueStr}";
    });

    var rawData = string.Join("&", parts);

    // Log rawData for debugging (you can compare this to docs / dashboard)
    _logger.LogInformation($"PayOS signature rawData: {rawData}");

    // Compute HMAC SHA256
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
    var signature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

    _logger.LogInformation($"PayOS signature generated for OrderCode={request.OrderCode} (5 basic fields only)");
    return signature;
}

        public async Task<PayOSCreatePaymentResponse> CreatePaymentLinkAsync(PayOSCreatePaymentRequest request)
        {
            var endpoint = "/v2/payment-requests";
            var fullUrl = $"{_baseUrl}{endpoint}";
            
            try
            {
                _logger.LogInformation($"Creating PayOS payment link: OrderCode={request.OrderCode}, Amount={request.Amount}, Url={fullUrl}");

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                // ✅ Log request payload để debug
                _logger.LogInformation($"PayOS request payload: {json}");
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // ✅ Set timeout cho HttpClient request
                var timeout = TimeSpan.FromSeconds(30);
                using var cts = new CancellationTokenSource(timeout);

                var response = await _httpClient.PostAsync(endpoint, content, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"PayOS API response: StatusCode={response.StatusCode}, OrderCode={request.OrderCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = $"PayOS API error (Status: {response.StatusCode}): {responseContent}";
                    _logger.LogError(new Exception(errorMsg), $"PayOS API error: OrderCode={request.OrderCode}, StatusCode={response.StatusCode}");
                    throw new Exception(errorMsg);
                }

                // ✅ Log raw response để debug
                _logger.LogInformation($"PayOS raw response: {responseContent}");

                var result = JsonSerializer.Deserialize<PayOSCreatePaymentResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                {
                    var errorMsg = $"Failed to parse PayOS response: {responseContent}";
                    _logger.LogError(new Exception(errorMsg), $"Failed to parse PayOS response: OrderCode={request.OrderCode}");
                    throw new Exception(errorMsg);
                }

                if (result.Code != "00")
                {
                    _logger.LogWarning($"PayOS returned non-zero code: OrderCode={request.OrderCode}, Code={result.Code}, Desc={result.Desc}");
                }
                else
                {
                    _logger.LogInformation($"PayOS payment link created successfully: OrderCode={request.OrderCode}, PaymentLinkId={result.Data?.PaymentLinkId}");
                }

                return result;
            }
            catch (TaskCanceledException ex)
            {
                var errorMsg = $"PayOS API request timeout after 30 seconds. Endpoint: {fullUrl}";
                _logger.LogError(ex, $"PayOS timeout: OrderCode={request.OrderCode}, Url={fullUrl}");
                throw new Exception(errorMsg, ex);
            }
            catch (HttpRequestException ex)
            {
                // ✅ Enhanced error message for DNS/connection issues
                var isDnsError = ex.Message.Contains("name is valid") || 
                                 ex.Message.Contains("no data of the requested type") ||
                                 ex.Message.Contains("Name or service not known") ||
                                 ex.Message.Contains("Could not resolve host");
                
                var errorMsg = isDnsError
                    ? $"Không thể kết nối đến PayOS API. Lỗi DNS/Network: {ex.Message}. BaseUrl: {_baseUrl}. Vui lòng kiểm tra kết nối internet và cấu hình DNS."
                    : $"PayOS API connection error: {ex.Message}. BaseUrl: {_baseUrl}";
                
                _logger.LogError(ex, $"PayOS connection error: OrderCode={request.OrderCode}, BaseUrl={_baseUrl}, Message={ex.Message}, InnerException={ex.InnerException?.Message}");
                throw new Exception(errorMsg, ex);
            }
            catch (UriFormatException ex)
            {
                var errorMsg = $"PayOS BaseUrl không hợp lệ: {_baseUrl}";
                _logger.LogError(ex, $"Invalid PayOS BaseUrl: {_baseUrl}");
                throw new Exception(errorMsg, ex);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error creating PayOS payment link: {ex.Message}";
                _logger.LogError(ex, $"PayOS error: OrderCode={request.OrderCode}, Message={ex.Message}");
                throw new Exception(errorMsg, ex);
            }
        }

        public Task<bool> VerifyWebhookSignatureAsync(PayOSWebhookRequest webhook, string checksumKey)
        {
            try
            {
                if (webhook == null || webhook.Data == null || string.IsNullOrEmpty(checksumKey))
                {
                    _logger.LogWarning("Webhook signature verification failed: Invalid webhook data or checksumKey");
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
                    _logger.LogWarning($"Webhook signature verification failed: OrderCode={webhook.Data?.OrderCode}, Expected={computedSignature.Substring(0, Math.Min(16, computedSignature.Length))}..., Received={webhook.Signature?.Substring(0, Math.Min(16, webhook.Signature?.Length ?? 0))}...");
                    // Note: Không log checksumKey hoặc full signature vì lý do bảo mật
                }
                else
                {
                    _logger.LogInformation($"Webhook signature verified successfully: OrderCode={webhook.Data?.OrderCode}");
                }

                return Task.FromResult(isValid);
            }
            catch (Exception ex)
            {
                // ✅ Log exception để debug signature verification issues
                _logger.LogError(ex, $"Error verifying webhook signature: OrderCode={webhook?.Data?.OrderCode}");
                // Note: Return false để reject webhook nếu có lỗi trong quá trình verify
                return Task.FromResult(false);
            }
        }

        public async Task<bool> RefundAsync(int orderCode, int amount, string reason)
        {
            var endpoint = "/v2/refund";
            var fullUrl = $"{_baseUrl}{endpoint}";
            
            try
            {
                _logger.LogInformation($"Processing PayOS refund: OrderCode={orderCode}, Amount={amount}, Url={fullUrl}");

                var refundRequest = new
                {
                    orderCode = orderCode,
                    amount = amount,
                    reason = reason
                };

                var json = JsonSerializer.Serialize(refundRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"PayOS refund response: StatusCode={response.StatusCode}, OrderCode={orderCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = $"PayOS refund error: {responseContent}";
                    _logger.LogError(new Exception(errorMsg), $"PayOS refund error: OrderCode={orderCode}, StatusCode={response.StatusCode}");
                    throw new Exception(errorMsg);
                }

                _logger.LogInformation($"PayOS refund successful: OrderCode={orderCode}, Amount={amount}");
                return true;
            }
            catch (HttpRequestException ex)
            {
                var isDnsError = ex.Message.Contains("name is valid") || 
                                 ex.Message.Contains("no data of the requested type") ||
                                 ex.Message.Contains("Name or service not known") ||
                                 ex.Message.Contains("Could not resolve host");
                
                var errorMsg = isDnsError
                    ? $"Không thể kết nối đến PayOS API để hoàn tiền. Lỗi DNS/Network: {ex.Message}. BaseUrl: {_baseUrl}."
                    : $"PayOS refund connection error: {ex.Message}. BaseUrl: {_baseUrl}";
                
                _logger.LogError(ex, $"PayOS refund connection error: OrderCode={orderCode}, BaseUrl={_baseUrl}, Message={ex.Message}");
                throw new Exception(errorMsg, ex);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error processing refund: {ex.Message}";
                _logger.LogError(ex, $"PayOS refund error: OrderCode={orderCode}, Message={ex.Message}");
                throw new Exception(errorMsg, ex);
            }
        }
    }
}

