// File: eCommerceApp.Aplication/Services/Interfaces/IShopCategoryService.cs

using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.ShopCategory;
using System;
using System.Collections.Generic;
namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IShopCategoryService
    {
        Task<ServiceResponse<GetShopCategory>> CreateShopCategoryAsync(Guid shopId, CreateShopCategory dto);
        Task<ServiceResponse<bool>> UpdateShopCategoryAsync(Guid shopId, Guid categoryId, UpdateShopCategory dto);
        Task<ServiceResponse<bool>> DeleteShopCategoryAsync(Guid shopId, Guid categoryId);
        Task<ServiceResponse<IEnumerable<GetShopCategory>>> GetShopCategoriesByShopIdAsync(Guid shopId);
        Task<ServiceResponse<GetShopCategory>> GetShopCategoryDetailAsync(Guid shopId, Guid categoryId);
    }
}