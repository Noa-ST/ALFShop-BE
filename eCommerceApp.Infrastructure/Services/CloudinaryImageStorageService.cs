using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Aplication.Services.Interfaces.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace eCommerceApp.Infrastructure.Services
{
    public class CloudinaryImageStorageService : IImageStorageService
    {
        private readonly string _cloudName;
        private readonly string _uploadPreset;
        private readonly string? _defaultFolder;
        private readonly IAppLogger<CloudinaryImageStorageService> _logger;
        private readonly HttpClient _httpClient = new HttpClient();
        
        public CloudinaryImageStorageService(IConfiguration config, IAppLogger<CloudinaryImageStorageService> logger)
        {
            _logger = logger;
            _cloudName = (config["Storage:Cloudinary:CloudName"] ?? throw new ArgumentException("Missing Storage:Cloudinary:CloudName")).Trim();
            _uploadPreset = (config["Storage:Cloudinary:UploadPreset"] ?? throw new ArgumentException("Missing Storage:Cloudinary:UploadPreset")).Trim();
            var folder = config["Storage:Cloudinary:Folder"];
            _defaultFolder = string.IsNullOrWhiteSpace(folder) ? null : folder.Trim();

            // Log cấu hình Cloudinary ở runtime để xác nhận giá trị đang dùng
            _logger.LogInformation($"Cloudinary initialized: CloudName={_cloudName}, UploadPreset={_uploadPreset}, DefaultFolder={_defaultFolder ?? "(none)"}");
        }

        public async Task<string> UploadBase64Async(string base64, string? folder = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(base64))
                throw new ArgumentException("Base64 content is empty");

            // Giữ nguyên data URI nếu đầu vào đã là data:image/...; nếu không, tách phần sau dấu ','
            var data = base64;
            var commaIndex = base64.IndexOf(',');
            if (commaIndex >= 0)
            {
                data = base64.Substring(commaIndex + 1);
            }

            string mime = "image/png";
            if (base64.StartsWith("data:image/jpeg")) mime = "image/jpeg";
            if (base64.StartsWith("data:image/jpg"))  mime = "image/jpg";
            if (base64.StartsWith("data:image/png"))  mime = "image/png";

            var endpointBase = $"https://api.cloudinary.com/v1_1/{_cloudName}/image/upload";
            var targetFolder = string.IsNullOrWhiteSpace(folder) ? _defaultFolder : folder;

            var safeBaseName = $"img_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            _logger.LogInformation($"Cloudinary upload: endpoint={endpointBase}, preset={_uploadPreset}, folder={(string.IsNullOrWhiteSpace(targetFolder) ? "(none)" : targetFolder)}, publicId={safeBaseName}, dataLen={(data?.Length ?? 0)}");

            var values = new List<KeyValuePair<string, string>>
            {
                new("file", $"data:{mime};base64,{data}"),
                new("upload_preset", _uploadPreset),
                new("public_id", safeBaseName),
                new("filename_override", safeBaseName)
            };
            if (!string.IsNullOrWhiteSpace(targetFolder))
            {
                values.Add(new("folder", targetFolder!));
            }

            using var formUrlEncoded = new FormUrlEncodedContent(values);
            var resp = await _httpClient.PostAsync(endpointBase, formUrlEncoded, cancellationToken);
            var content = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode && content.Contains("Upload preset must be specified", StringComparison.OrdinalIgnoreCase))
            {
                var endpointWithQuery = $"{endpointBase}?upload_preset={Uri.EscapeDataString(_uploadPreset)}";
                _logger.LogInformation($"Retry with query preset: endpoint={endpointWithQuery}, folder={(string.IsNullOrWhiteSpace(targetFolder) ? "(none)" : targetFolder)}");

                var retryValues = new List<KeyValuePair<string, string>>
                {
                    new("file", $"data:{mime};base64,{data}"),
                    new("public_id", safeBaseName),
                    new("filename_override", safeBaseName)
                };
                if (!string.IsNullOrWhiteSpace(targetFolder))
                {
                    retryValues.Add(new("folder", targetFolder!));
                }

                using var retryForm = new FormUrlEncodedContent(retryValues);
                resp = await _httpClient.PostAsync(endpointWithQuery, retryForm, cancellationToken);
                content = await resp.Content.ReadAsStringAsync(cancellationToken);
            }

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Cloudinary upload failed: Status={(int)resp.StatusCode}, Body={content}.");
                throw new InvalidOperationException("Cloudinary upload failed");
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var url = root.GetProperty("secure_url").GetString() ?? root.GetProperty("url").GetString();
            if (string.IsNullOrEmpty(url))
                throw new InvalidOperationException("Cloudinary did not return a URL");

            return url!;
        }
    }
}