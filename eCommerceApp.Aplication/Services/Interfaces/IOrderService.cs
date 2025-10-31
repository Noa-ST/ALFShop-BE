using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Application.DTOs.Order;

namespace eCommerceApp.Application.Services.Interfaces
{
    public interface IOrderService
    {
        Task<ServiceResponse<List<OrderResponseDTO>>> CreateOrderAsync(OrderCreateDTO dto);
        Task<ServiceResponse<List<OrderResponseDTO>>> GetMyOrdersAsync(Guid customerId);
        Task<ServiceResponse<List<OrderResponseDTO>>> GetShopOrdersAsync(Guid shopId);
        Task<ServiceResponse<List<OrderResponseDTO>>> GetAllOrdersAsync();
        Task<ServiceResponse<bool>> UpdateStatusAsync(Guid id, OrderUpdateStatusDTO dto);
    }
}
