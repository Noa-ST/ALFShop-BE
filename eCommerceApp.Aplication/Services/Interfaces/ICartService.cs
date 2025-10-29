using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Cart;
using System;
using System.Threading.Tasks;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface ICartService
    {
        // POST /api/Cart/add
        Task<ServiceResponse> AddItemToCartAsync(string userId, AddCartItem dto);

        // PUT /api/Cart/update
        Task<ServiceResponse> UpdateCartItemQuantityAsync(string userId, UpdateCartItem dto);

        // DELETE /api/Cart/deleteItem/{productId}
        Task<ServiceResponse> RemoveItemFromCartAsync(string userId, Guid productId);

        // GET /api/Cart
        Task<ServiceResponse<GetCartDto>> GetCurrentCartAsync(string userId);
    }
}