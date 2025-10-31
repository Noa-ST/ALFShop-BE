using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Order;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IOrderService
    {
        Task<ServiceResponse<List<OrderResponseDTO>>> CreateOrderAsync(OrderCreateDTO dto);
        Task<ServiceResponse<List<OrderResponseDTO>>> GetMyOrdersAsync(string customerId);
        Task<ServiceResponse<List<OrderResponseDTO>>> GetShopOrdersAsync(Guid shopId);
        Task<ServiceResponse<List<OrderResponseDTO>>> GetAllOrdersAsync();
        Task<ServiceResponse<bool>> UpdateStatusAsync(Guid id, OrderUpdateStatusDTO dto);
    }
}
