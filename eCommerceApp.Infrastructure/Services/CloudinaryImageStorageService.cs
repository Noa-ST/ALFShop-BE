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
            _cloudName = config["Storage:Cloudinary:CloudName"] ?? throw new ArgumentException("Missing Storage:Cloudinary:CloudName");
            _uploadPreset = config["Storage:Cloudinary:UploadPreset"] ?? throw new ArgumentException("Missing Storage:Cloudinary:UploadPreset");
            _defaultFolder = config["Storage:Cloudinary:Folder"];
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