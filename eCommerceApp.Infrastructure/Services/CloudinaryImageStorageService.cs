using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Aplication.Services.Interfaces.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.IO;

namespace eCommerceApp.Infrastructure.Services
{
    public class CloudinaryImageStorageService : IImageStorageService
    {
        private readonly string _cloudName;
        private readonly string _uploadPreset;
        private readonly string? _defaultFolder;
        private readonly IAppLogger<CloudinaryImageStorageService> _logger;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly IWebHostEnvironment _env;

        public CloudinaryImageStorageService(IConfiguration config, IAppLogger<CloudinaryImageStorageService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _cloudName = config["Storage:Cloudinary:CloudName"] ?? throw new ArgumentException("Missing Storage:Cloudinary:CloudName");
            _uploadPreset = config["Storage:Cloudinary:UploadPreset"] ?? throw new ArgumentException("Missing Storage:Cloudinary:UploadPreset");
            _defaultFolder = config["Storage:Cloudinary:Folder"];
            _env = env;
        }

        public async Task<string> UploadBase64Async(string base64, string? folder = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(base64))
                throw new ArgumentException("Base64 content is empty");

            // Chấp nhận cả dạng data URI "data:image/png;base64,AAAA..." hoặc chỉ phần base64
            var data = base64;
            var commaIndex = base64.IndexOf(',');
            if (commaIndex >= 0)
            {
                data = base64.Substring(commaIndex + 1);
            }

            var endpoint = $"https://api.cloudinary.com/v1_1/{_cloudName}/image/upload";
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent($"data:image/*;base64,{data}"), "file");
            form.Add(new StringContent(_uploadPreset), "upload_preset");
            var targetFolder = folder ?? _defaultFolder;
            if (!string.IsNullOrWhiteSpace(targetFolder))
            {
                form.Add(new StringContent(targetFolder!), "folder");
            }

            var resp = await _httpClient.PostAsync(endpoint, form, cancellationToken);
            var content = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                // Fallback: lưu ảnh local trong wwwroot nếu Cloudinary lỗi (400/401/5xx...)
                _logger.LogWarning($"Cloudinary upload failed: Status={(int)resp.StatusCode}, Body={content}. Falling back to local storage.");
                return await SaveBase64ToLocalAsync(base64, folder, cancellationToken);
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var url = root.GetProperty("secure_url").GetString() ?? root.GetProperty("url").GetString();
            if (string.IsNullOrEmpty(url))
                return await SaveBase64ToLocalAsync(base64, folder, cancellationToken);

            return url!;
        }

        private async Task<string> SaveBase64ToLocalAsync(string base64, string? folder, CancellationToken ct)
        {
            // Extract data part and detect extension
            var data = base64;
            string extension = "jpg";
            var commaIndex = base64.IndexOf(',');
            if (commaIndex >= 0)
            {
                var header = base64.Substring(0, commaIndex);
                data = base64.Substring(commaIndex + 1);
                if (header.Contains("image/png", StringComparison.OrdinalIgnoreCase)) extension = "png";
                else if (header.Contains("image/webp", StringComparison.OrdinalIgnoreCase)) extension = "webp";
                else if (header.Contains("image/jpeg", StringComparison.OrdinalIgnoreCase) || header.Contains("image/jpg", StringComparison.OrdinalIgnoreCase)) extension = "jpg";
            }

            // Determine webroot and target folder
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
            }

            // Default to uploads/products nếu không truyền folder
            var relativeFolder = string.IsNullOrWhiteSpace(folder) ? "uploads/products" : folder!.Trim().TrimStart('/');
            var localDir = Path.Combine(webRoot, relativeFolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(localDir);

            // Save file
            var fileName = $"{Guid.NewGuid():N}.{extension}";
            var filePath = Path.Combine(localDir, fileName);
            try
            {
                var bytes = Convert.FromBase64String(data);
                await File.WriteAllBytesAsync(filePath, bytes, ct);
                var relativeUrl = $"/{relativeFolder.Replace('\\', '/')}/{fileName}";
                return relativeUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save image locally.");
                throw new InvalidOperationException("Failed to upload image: cloud and local storage both failed.");
            }
        }
    }
}