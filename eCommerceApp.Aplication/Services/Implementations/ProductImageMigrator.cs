using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace eCommerceApp.Aplication.Services.Implementations
{
    /// <summary>
    /// Utility to migrate ProductImage.Url values that contain embedded data URLs to physical files
    /// under wwwroot/uploads/products and update the DB records to point to the new relative path.
    /// </summary>
    public class ProductImageMigrator
    {
        private readonly IProductImageRepository _imageRepo;
        private readonly IWebHostEnvironment _env;

        public ProductImageMigrator(IProductImageRepository imageRepo, IWebHostEnvironment env)
        {
            _imageRepo = imageRepo;
            _env = env;
        }

        public async Task<int> MigrateAsync()
        {
            // Get all images (assumes GenericRepository exposes GetAllAsync)
            var all = await _imageRepo.GetAllAsync();
            if (all == null) return 0;

            string webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
            }
            string uploadRoot = Path.Combine(webRoot, "uploads", "products");
            Directory.CreateDirectory(uploadRoot);

            var changed = 0;
            foreach (var img in all)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(img.Url)) continue;
                    var url = img.Url.Trim();

                    // if contains an embedded data:image anywhere, try extract and save
                    int idx = -1;
                    if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && url.Contains(";base64,"))
                        idx = 0;
                    else if (url.IndexOf("data:image/", StringComparison.OrdinalIgnoreCase) >= 0)
                        idx = url.IndexOf("data:image/", StringComparison.OrdinalIgnoreCase);

                    if (idx < 0) continue; // skip non-data images

                    var dataPart = url.Substring(idx);
                    var headerEnd = dataPart.IndexOf(',');
                    if (headerEnd < 0) continue;
                    var header = dataPart.Substring(0, headerEnd);
                    var base64Part = dataPart[(headerEnd + 1)..];

                    string extension = "jpg";
                    if (header.Contains("image/png", StringComparison.OrdinalIgnoreCase)) extension = "png";
                    else if (header.Contains("image/webp", StringComparison.OrdinalIgnoreCase)) extension = "webp";
                    else if (header.Contains("image/jpeg", StringComparison.OrdinalIgnoreCase) || header.Contains("image/jpg", StringComparison.OrdinalIgnoreCase)) extension = "jpg";

                    var fileName = $"{Guid.NewGuid():N}.{extension}";
                    var filePath = Path.Combine(uploadRoot, fileName);

                    byte[] bytes;
                    try
                    {
                        bytes = Convert.FromBase64String(base64Part);
                    }
                    catch
                    {
                        // invalid base64
                        continue;
                    }

                    await File.WriteAllBytesAsync(filePath, bytes);

                    // update URL to the relative path
                    img.Url = $"/uploads/products/{fileName}";
                    img.UpdatedAt = DateTime.UtcNow;

                    // Update via repository (assumes UpdateAsync exists)
                    await _imageRepo.UpdateAsync(img);
                    changed++;
                }
                catch
                {
                    // ignore and continue
                    continue;
                }
            }

            return changed;
        }
    }
}
