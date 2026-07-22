using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Services;

public interface IDepositService
{
    Task<DepositDto?> GetDepositByOrderIdAsync(int orderId, Guid currentUserId, string currentUserRole);
}
