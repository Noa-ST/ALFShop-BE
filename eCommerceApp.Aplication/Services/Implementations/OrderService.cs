using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Application.DTOs.Order;
using eCommerceApp.Application.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Repositories;

namespace eCommerceApp.Application.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IMapper _mapper;

        public OrderService(IOrderRepository orderRepo, IMapper mapper)
        {
            _orderRepo = orderRepo;
            _mapper = mapper;
        }

        public async Task<ServiceResponse<List<OrderResponseDTO>>> CreateOrderAsync(OrderCreateDTO dto)
        {
            // Logic tạo order từ cart
            var newOrder = new Domain.Entities.Order
            {
                Id = Guid.NewGuid(),
                ShopId = dto.ShopId,
                CreatedAt = DateTime.UtcNow
            };

            // Gán enum sau khi tạo object
            newOrder.Status = Enum.Parse<OrderStatus>("Pending");
            await _orderRepo.CreateOrderAsync(newOrder);
            return new ServiceResponse<List<OrderResponseDTO>> { Data = new List<OrderResponseDTO> { _mapper.Map<OrderResponseDTO>(newOrder) } };
        }

        public async Task<ServiceResponse<List<OrderResponseDTO>>> GetMyOrdersAsync(Guid customerId)
        {
            var orders = await _orderRepo.GetOrdersByCustomerIdAsync(customerId);
            return new ServiceResponse<List<OrderResponseDTO>> { Data = _mapper.Map<List<OrderResponseDTO>>(orders) };
        }

        public async Task<ServiceResponse<List<OrderResponseDTO>>> GetShopOrdersAsync(Guid shopId)
        {
            var orders = await _orderRepo.GetOrdersByShopIdAsync(shopId);
            return new ServiceResponse<List<OrderResponseDTO>> { Data = _mapper.Map<List<OrderResponseDTO>>(orders) };
        }

        public async Task<ServiceResponse<List<OrderResponseDTO>>> GetAllOrdersAsync()
        {
            var orders = await _orderRepo.GetAllOrdersAsync();
            return new ServiceResponse<List<OrderResponseDTO>> { Data = _mapper.Map<List<OrderResponseDTO>>(orders) };
        }

        public async Task<ServiceResponse<bool>> UpdateStatusAsync(Guid id, OrderUpdateStatusDTO dto)
        {
            await _orderRepo.UpdateStatusAsync(id, dto.Status);
            return new ServiceResponse<bool> { Data = true };
        }
    }
}
