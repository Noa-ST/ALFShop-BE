using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class ShopLogoMigrator
    {
        private readonly IShopRepository _shopRepo;
        private readonly IImageStorageService _imageStorage;
        private readonly IWebHostEnvironment _env;
        private readonly IUnitOfWork _unitOfWork;
        private readonly HttpClient _httpClient = new HttpClient();

        public ShopLogoMigrator(
            IShopRepository shopRepo,
            IImageStorageService imageStorage,
            IWebHostEnvironment env,
            IUnitOfWork unitOfWork)
        {
            _shopRepo = shopRepo;
            _imageStorage = imageStorage;
            _env = env;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> MigrateAsync()
        {
            var all = await _shopRepo.GetAllAsync();
            if (all == null) return 0;

            string webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
            }

            int changed = 0;
            foreach (var shop in all.Where(s => !s.IsDeleted && !string.IsNullOrWhiteSpace(s.Logo)))
            {
                var input = shop.Logo!.Trim();

                // Bỏ qua nếu đã là Cloudinary
                bool isCloudinary = input.Contains("res.cloudinary.com", StringComparison.OrdinalIgnoreCase);
                if (isCloudinary) continue;

                string? finalUrl = null;
                try
                {
                    if (input.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                    {
                        // Đường dẫn tương đối -> đọc file local và upload Cloudinary
                        var relative = input.TrimStart('/');
                        var absolutePath = Path.Combine(webRoot, relative.Replace('/', Path.DirectorySeparatorChar));
                        if (!File.Exists(absolutePath)) continue;

                        var bytes = await File.ReadAllBytesAsync(absolutePath);
                        var base64 = Convert.ToBase64String(bytes);
                        var dataUrl = $"data:image/*;base64,{base64}";
                        finalUrl = await _imageStorage.UploadBase64Async(dataUrl, "uploads/shops");
                    }
                    else if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                             input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        // URL ngoài -> tải bytes, chuyển Base64, upload Cloudinary
                        var bytes = await _httpClient.GetByteArrayAsync(input);
                        var base64 = Convert.ToBase64String(bytes);
                        var dataUrl = $"data:image/*;base64,{base64}";
                        finalUrl = await _imageStorage.UploadBase64Async(dataUrl, "uploads/shops");
                    }
                    else
                    {
                        // Base64/data URL -> upload trực tiếp
                        finalUrl = await _imageStorage.UploadBase64Async(input, "uploads/shops");
                    }

                    if (!string.IsNullOrWhiteSpace(finalUrl))
                    {
                        shop.Logo = finalUrl;
                        shop.UpdatedAt = DateTime.UtcNow;
                        await _shopRepo.UpdateAsync(shop);
                        changed++;
                    }
                }
                catch
                {
                    // Bỏ qua lỗi từng shop, tiếp tục shop khác
                    continue;
                }
            }

            if (changed > 0)
            {
                await _unitOfWork.SaveChangesAsync();
            }

            return changed;
        }
    }
}