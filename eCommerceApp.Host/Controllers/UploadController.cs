using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.IO;
using System.Collections.Generic;

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

        public class UploadFilesForm
        {
            public IFormFile[] Images { get; set; } = Array.Empty<IFormFile>();
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

        // ✅ New: Upload nhiều ảnh dạng multipart/form-data
        [HttpPost("batch")]
        [Authorize(Roles = "Admin,Seller")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ServiceResponse<List<string>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UploadBatch([FromForm] UploadFilesForm request, CancellationToken ct)
        {
            const int MaxFiles = 10;
            const long MaxFileSize = 5 * 1024 * 1024; // 5MB
            var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (request == null || request.Images == null || request.Images.Length == 0)
            {
                return BadRequest(ServiceResponse<List<string>>.Fail("Thiếu file ảnh.", HttpStatusCode.BadRequest));
            }

            if (request.Images.Length > MaxFiles)
            {
                return BadRequest(ServiceResponse<List<string>>.Fail($"Quá số lượng file cho phép (tối đa {MaxFiles}).", HttpStatusCode.BadRequest));
            }

            // Validate tất cả files trước
            for (int i = 0; i < request.Images.Length; i++)
            {
                var file = request.Images[i];
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ServiceResponse<List<string>>.Fail($"File tại vị trí {i} rỗng.", HttpStatusCode.BadRequest));
                }
                if (!allowedTypes.Contains(file.ContentType))
                {
                    return BadRequest(ServiceResponse<List<string>>.Fail($"Loại MIME không hỗ trợ tại vị trí {i}: {file.ContentType}.", HttpStatusCode.BadRequest));
                }
                if (file.Length > MaxFileSize)
                {
                    return BadRequest(ServiceResponse<List<string>>.Fail($"Kích thước file vượt quá 5MB tại vị trí {i}.", HttpStatusCode.BadRequest));
                }
            }

            try
            {
                var urls = new List<string>(request.Images.Length);
                foreach (var file in request.Images)
                {
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms, ct);
                    var base64 = Convert.ToBase64String(ms.ToArray());
                    var dataUrl = $"data:{file.ContentType};base64,{base64}";
                    var url = await _imageStorage.UploadBase64Async(dataUrl, request.Folder, ct);
                    urls.Add(url);
                }

                return Ok(ServiceResponse<List<string>>.Success(urls, "Upload thành công"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ServiceResponse<List<string>>.Fail(ex.Message, HttpStatusCode.BadRequest));
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ServiceResponse<List<string>>.Fail($"Upload thất bại: {ex.Message}", HttpStatusCode.InternalServerError));
            }
        }
    }
}