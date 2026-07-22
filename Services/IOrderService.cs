using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Services;

public interface IOrderService
{
    Task<ServiceResult<OrderDto>> CreateOrderAsync(Guid userId, OrderCreateRequest request);
    Task<List<OrderDto>> GetOrdersAsync(Guid currentUserId, string currentUserRole);
    Task<OrderDto?> GetOrderByIdAsync(int id, Guid currentUserId, string currentUserRole);
    Task<ServiceResult<OrderDto>> UpdateOrderStatusAsync(Guid processedByUserId, OrderStatusUpdateRequest request);
}
