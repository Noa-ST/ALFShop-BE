using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace eCommerceApp.Host.Controllers
{
    [ApiController]
    [Route("api/upload")]
    public class UploadController : ControllerBase
    {
        private readonly IImageStorageService _imageStorage;

        public UploadController(IImageStorageService imageStorage)
        {
            _imageStorage = imageStorage;
        }

        public class UploadBase64Request
        {
            public string Base64 { get; set; } = null!;
            public string? Folder { get; set; }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Seller")]
        [ProducesResponseType(typeof(ServiceResponse<string>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Upload([FromBody] UploadBase64Request request, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Base64))
            {
                return BadRequest(ServiceResponse<string>.Fail("Thiếu dữ liệu ảnh base64.", HttpStatusCode.BadRequest));
            }

            try
            {
                var url = await _imageStorage.UploadBase64Async(request.Base64, request.Folder, ct);
                return Ok(ServiceResponse<string>.Success(url, "Upload thành công"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ServiceResponse<string>.Fail(ex.Message, HttpStatusCode.BadRequest));
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ServiceResponse<string>.Fail($"Upload thất bại: {ex.Message}", HttpStatusCode.InternalServerError));
            }
        }
    }
}