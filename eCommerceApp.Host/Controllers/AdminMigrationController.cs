using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using eCommerceApp.Aplication.Services.Implementations;
using eCommerceApp.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using eCommerceApp.Aplication.Services.Interfaces; // NEW

namespace eCommerceApp.Host.Controllers
{
    [ApiController]
    [Route("api/admin/migrate")]
    [Authorize(Roles = "Admin")]
    public class AdminMigrationController : ControllerBase
    {
        private readonly IProductImageRepository _imageRepo;
        private readonly IWebHostEnvironment _env;
        private readonly IShopRepository _shopRepo; // NEW
        private readonly IImageStorageService _imageStorage; // NEW
        private readonly IUnitOfWork _unitOfWork; // NEW

        public AdminMigrationController(
            IProductImageRepository imageRepo, 
            IWebHostEnvironment env,
            IShopRepository shopRepo, // NEW
            IImageStorageService imageStorage, // NEW
            IUnitOfWork unitOfWork // NEW
        )
        {
            _imageRepo = imageRepo;
            _env = env;
            _shopRepo = shopRepo; // NEW
            _imageStorage = imageStorage; // NEW
            _unitOfWork = unitOfWork; // NEW
        }

        [HttpPost("images")]
        public async Task<IActionResult> MigrateImages()
        {
            var migrator = new ProductImageMigrator(_imageRepo, _env);
            var changed = await migrator.MigrateAsync();
            return Ok(new { migrated = changed });
        }

        [HttpPost("shop-logos")] // NEW
        public async Task<IActionResult> MigrateShopLogos()
        {
            var migrator = new ShopLogoMigrator(_shopRepo, _imageStorage, _env, _unitOfWork);
            var changed = await migrator.MigrateAsync();
            return Ok(new { migrated = changed });
        }
    }
}
