using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/deposit")]
[Authorize]
public class DepositController : ControllerBase
{
    private readonly IDepositService _depositService;

    public DepositController(IDepositService depositService)
    {
        _depositService = depositService;
    }

    [HttpGet("{orderId:int}")]
    public async Task<ActionResult<DepositDto>> GetDepositByOrderId(int orderId)
    {
        var deposit = await _depositService.GetDepositByOrderIdAsync(orderId, this.GetUserId(), this.GetUserRole());
        if (deposit == null)
        {
            return NotFound(new { error = "Không tìm thấy thông tin đặt cọc." });
        }

        return Ok(deposit);
    }
}
