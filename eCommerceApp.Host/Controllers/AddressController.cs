// File: eCommerceApp.Host/Controllers/AddressController.cs

using eCommerceApp.Aplication.DTOs.Address;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace eCommerceApp.Host.Controllers
{
    [Authorize] // Bắt buộc phải đăng nhập (Customer)
    [Route("api/[controller]")] // Route: /api/Address
    [ApiController]
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        // ✅ Hàm tiện ích lấy User ID (string) từ JWT Claim
        private string GetUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("Không thể xác định người dùng. Vui lòng đăng nhập lại.");
            }
            return userId;
        }

        // POST /api/Address/create
        [HttpPost("create")]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateAddress dto)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var response = await _addressService.CreateAddressAsync(userId, dto);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // PUT /api/Address/update/{id}
        [HttpPut("update/{id:guid}")]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAddress dto)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            // ✅ Validate route id với dto.Id
            if (id != dto.Id)
            {
                return BadRequest(new { message = "ID trong route không khớp với ID trong body." });
            }

            try
            {
                var userId = GetUserId();
                var response = await _addressService.UpdateAddressAsync(userId, id, dto);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // DELETE /api/Address/delete/{id}
        [HttpDelete("delete/{id:guid}")]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var response = await _addressService.DeleteAddressAsync(userId, id);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // GET /api/Address/list
        [HttpGet("list")]
        [ProducesResponseType(typeof(ServiceResponse<IEnumerable<GetAddressDto>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetList()
        {
            try
            {
                var userId = GetUserId();
                var response = await _addressService.GetUserAddressesAsync(userId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ GET /api/Address/{id}
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ServiceResponse<GetAddressDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var response = await _addressService.GetAddressByIdAsync(userId, id);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ New: PUT /api/Address/{id}/set-default
        [HttpPut("{id:guid}/set-default")]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> SetDefault(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var response = await _addressService.SetDefaultAddressAsync(userId, id);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ New: GET /api/Address/default
        [HttpGet("default")]
        [ProducesResponseType(typeof(ServiceResponse<GetAddressDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetDefault()
        {
            try
            {
                var userId = GetUserId();
                var response = await _addressService.GetDefaultAddressAsync(userId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}