using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class DepositService : IDepositService
{
    private readonly ApplicationDbContext _context;

    public DepositService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DepositDto?> GetDepositByOrderIdAsync(int orderId, Guid currentUserId, string currentUserRole)
    {
        var order = await _context.Orders
            .Include(o => o.Deposit)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order?.Deposit == null)
        {
            return null;
        }

        if (currentUserRole is not ("Employee" or "Admin") && order.UserId != currentUserId)
        {
            return null;
        }

        return MapToDto(order.Deposit);
    }

    private static DepositDto MapToDto(Deposit deposit)
    {
        return new DepositDto
        {
            Id = deposit.Id,
            OrderId = deposit.OrderId,
            Amount = deposit.Amount,
            DepositDate = deposit.DepositDate,
            PaymentMethod = deposit.PaymentMethod,
            TransactionCode = deposit.TransactionCode,
            Status = deposit.Status,
            PaidAt = deposit.PaidAt
        };
    }
}
