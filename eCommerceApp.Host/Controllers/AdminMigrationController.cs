using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using eCommerceApp.Aplication.Services.Implementations;
using eCommerceApp.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;

namespace eCommerceApp.Host.Controllers
{
    [ApiController]
    [Route("api/admin/migrate")]
    [Authorize(Roles = "Admin")]
    public class AdminMigrationController : ControllerBase
    {
        private readonly IProductImageRepository _imageRepo;
        private readonly IWebHostEnvironment _env;

        public AdminMigrationController(IProductImageRepository imageRepo, IWebHostEnvironment env)
        {
            _imageRepo = imageRepo;
            _env = env;
        }

        [HttpPost("images")]
        public async Task<IActionResult> MigrateImages()
        {
            var migrator = new ProductImageMigrator(_imageRepo, _env);
            var changed = await migrator.MigrateAsync();
            return Ok(new { migrated = changed });
        }
    }
}
