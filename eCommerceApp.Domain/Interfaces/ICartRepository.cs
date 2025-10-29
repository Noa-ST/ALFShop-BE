using eCommerceApp.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace eCommerceApp.Domain.Interfaces
{
    public interface ICartRepository : IGeneric<Cart>
    {
        // Lấy Cart của User hiện tại (cần Include CartItem, Product, Shop)
        Task<Cart?> GetCartByUserIdAsync(string userId);

        // Lấy CartItem cụ thể trong Cart
        Task<CartItem?> GetCartItemAsync(Guid cartId, Guid productId);
    }
}