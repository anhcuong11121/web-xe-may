using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/statistics")]
[Authorize(Roles = "Admin")]
public class StatisticsController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public StatisticsController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Thống kê doanh thu theo tháng (UC20).
    /// </summary>
    [HttpGet("revenue")]
    public async Task<ActionResult<List<RevenueStatisticDto>>> GetRevenue([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        if (!ValidRange(from, to)) return BadRequest(new { error = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc." });
        return Ok(await _dashboardService.GetRevenueStatisticsAsync(from, to));
    }

    /// <summary>
    /// Thống kê số lượng đơn đặt mua theo trạng thái (UC21).
    /// </summary>
    [HttpGet("order")]
    public async Task<ActionResult<List<OrderStatisticDto>>> GetOrderStatistics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        if (!ValidRange(from, to)) return BadRequest(new { error = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc." });
        return Ok(await _dashboardService.GetOrderStatisticsAsync(from, to));
    }

    /// <summary>
    /// Thống kê số lượng khách hàng (UC22).
    /// </summary>
    [HttpGet("customer")]
    public async Task<ActionResult<CustomerStatisticDto>> GetCustomerStatistics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        if (!ValidRange(from, to)) return BadRequest(new { error = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc." });
        return Ok(await _dashboardService.GetCustomerStatisticsAsync(from, to));
    }

    /// <summary>
    /// Sản phẩm được quan tâm nhiều nhất, dựa trên số lượng đã bán (UC23).
    /// </summary>
    [HttpGet("product")]
    public async Task<ActionResult<List<ProductStatisticDto>>> GetProductStatistics(
        [FromQuery] int top = 10, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        if (!ValidRange(from, to)) return BadRequest(new { error = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc." });
        return Ok(await _dashboardService.GetProductStatisticsAsync(top, from, to));
    }

    [HttpGet("inventory")]
    public async Task<ActionResult<List<InventoryStatisticDto>>> GetInventoryStatistics() =>
        Ok(await _dashboardService.GetInventoryStatisticsAsync());

    [HttpGet("purchases")]
    public async Task<ActionResult<List<PurchaseStatisticDto>>> GetPurchaseStatistics(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        if (!ValidRange(from, to)) return BadRequest(new { error = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc." });
        return Ok(await _dashboardService.GetPurchaseStatisticsAsync(from, to));
    }

    [HttpGet("interests")]
    public async Task<ActionResult<List<ProductInterestStatisticDto>>> GetInterestStatistics(
        [FromQuery] int top = 10, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        if (!ValidRange(from, to)) return BadRequest(new { error = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc." });
        return Ok(await _dashboardService.GetProductInterestStatisticsAsync(top, from, to));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int top = 10)
    {
        if (!ValidRange(from, to)) return BadRequest(new { error = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc." });

        var revenue = await _dashboardService.GetRevenueStatisticsAsync(from, to);
        var orders = await _dashboardService.GetOrderStatisticsAsync(from, to);
        var customers = await _dashboardService.GetCustomerStatisticsAsync(from, to);
        var products = await _dashboardService.GetProductStatisticsAsync(top, from, to);
        var inventory = await _dashboardService.GetInventoryStatisticsAsync();
        var purchases = await _dashboardService.GetPurchaseStatisticsAsync(from, to);
        var interests = await _dashboardService.GetProductInterestStatisticsAsync(top, from, to);
        var csv = new StringBuilder("sep=,\r\n");
        csv.AppendLine("THONG KE DOANH THU");
        csv.AppendLine("Ky,Doanh thu");
        foreach (var item in revenue) csv.AppendLine($"{Csv(item.Period)},{item.TotalRevenue}");
        csv.AppendLine().AppendLine("THONG KE DON HANG").AppendLine("Trang thai,So luong");
        foreach (var item in orders) csv.AppendLine($"{Csv(item.Status)},{item.Count}");
        csv.AppendLine().AppendLine("THONG KE KHACH HANG").AppendLine("Tong khach hang,Khach moi thang nay");
        csv.AppendLine($"{customers.TotalCustomers},{customers.NewCustomersThisMonth}");
        csv.AppendLine().AppendLine("TOP SAN PHAM").AppendLine("Ma san pham,Ten san pham,So luong da ban");
        foreach (var item in products) csv.AppendLine($"{item.ProductId},{Csv(item.ProductName)},{item.TotalQuantitySold}");
        csv.AppendLine().AppendLine("TON KHO").AppendLine("Ma san pham,Ten san pham,So luong,Trang thai");
        foreach (var item in inventory) csv.AppendLine($"{item.ProductId},{Csv(item.ProductName)},{item.StockQuantity},{item.Status}");
        csv.AppendLine().AppendLine("TINH HINH MUA XE").AppendLine("Ky,So don,So xe,Tong gia tri");
        foreach (var item in purchases) csv.AppendLine($"{Csv(item.Period)},{item.TotalOrders},{item.TotalVehicles},{item.TotalValue}");
        csv.AppendLine().AppendLine("LUOT QUAN TAM").AppendLine("Ma san pham,Ten san pham,Luot xem,So luong da ban");
        foreach (var item in interests) csv.AppendLine($"{item.ProductId},{Csv(item.ProductName)},{item.ViewCount},{item.TotalQuantitySold}");

        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"thong-ke-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    private static bool ValidRange(DateTime? from, DateTime? to) => !from.HasValue || !to.HasValue || from.Value.Date <= to.Value.Date;

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
