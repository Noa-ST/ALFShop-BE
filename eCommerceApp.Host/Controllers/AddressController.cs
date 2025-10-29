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

    // Hàm tiện ích lấy User ID (string) từ JWT Claim
    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    // POST /api/Address/create
    [HttpPost("create")]
    [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAddress dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var response = await _addressService.CreateAddressAsync(GetUserId(), dto);

        return StatusCode((int)response.StatusCode, response);
    }

    // PUT /api/Address/update/{id}
    [HttpPut("update/{id:guid}")]
    [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAddress dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var response = await _addressService.UpdateAddressAsync(GetUserId(), id, dto);

        return StatusCode((int)response.StatusCode, response);
    }

    // DELETE /api/Address/delete/{id}
    [HttpDelete("delete/{id:guid}")]
    [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var response = await _addressService.DeleteAddressAsync(GetUserId(), id);

        return StatusCode((int)response.StatusCode, response);
    }

    // GET /api/Address/list
    [HttpGet("list")]
    [ProducesResponseType(typeof(ServiceResponse<IEnumerable<GetAddressDto>>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetList()
    {
        var response = await _addressService.GetUserAddressesAsync(GetUserId());

        return StatusCode((int)response.StatusCode, response);
    }
}