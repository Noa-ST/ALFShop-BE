using eCommerceApp.Aplication.DTOs.GlobalCategory;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

[Authorize(Roles = "Admin")]
[Route("api/Admin/[controller]")]
[ApiController]
public class GlobalCategoryController : ControllerBase
{
    private readonly IGlobalCategoryService _globalCategoryService;

    public GlobalCategoryController(IGlobalCategoryService globalCategoryService)
    {
        _globalCategoryService = globalCategoryService;
    }

    [HttpPost("add")]
    [ProducesResponseType(typeof(ServiceResponse<GetGlobalCategory>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateGlobalCategory dto)
    {
        var response = await _globalCategoryService.CreateGlobalCategoryAsync(dto);

        if (response.Succeeded)
        {
            return Ok(response);
        }
        return BadRequest(response);
    }

    [HttpPut("update/{id:guid}")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateGlobalCategory dto)
    {
        var response = await _globalCategoryService.UpdateGlobalCategoryAsync(id, dto);

        if (response.Succeeded)
        {
            return Ok(response);
        }
        return BadRequest(response);
    }

    [HttpDelete("delete/{id:guid}")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var response = await _globalCategoryService.DeleteGlobalCategoryAsync(id);

        if (response.Succeeded)
        {
            return Ok(response);
        }
        return BadRequest(response);
    }


    [HttpGet("all")]
    [ProducesResponseType(typeof(ServiceResponse<IEnumerable<GetGlobalCategory>>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var response = await _globalCategoryService.GetAllGlobalCategoriesAsync(includeChildren: true);
            return Ok(response);
        }
        catch (Exception ex)
        {
            // In ra console để xem lỗi trong Terminal / Output
            Console.WriteLine($"[ERROR] {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return StatusCode(500, $"Server Error: {ex.Message}");
        }
    }
}