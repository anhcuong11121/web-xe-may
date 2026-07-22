using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Tổng quan hệ thống: doanh thu, số đơn hàng, số khách hàng, số sản phẩm.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        return Ok(await _dashboardService.GetDashboardAsync());
    }
}
