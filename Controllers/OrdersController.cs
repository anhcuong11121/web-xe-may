using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Khách hàng đặt mua xe (UC09). Mỗi Order chứa một hoặc nhiều OrderItem;
    /// OrderItem lưu ProductId, số lượng và đơn giá tại thời điểm đặt hàng.
    /// </summary>
    [Authorize(Roles = "Customer")]
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] OrderCreateRequest request)
    {
        var result = await _orderService.CreateOrderAsync(this.GetUserId(), request);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetOrderById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Customer xem đơn hàng của mình, Employee/Admin xem tất cả (UC15).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetOrders()
    {
        var orders = await _orderService.GetOrdersAsync(this.GetUserId(), this.GetUserRole());
        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id, this.GetUserId(), this.GetUserRole());
        if (order == null)
        {
            return NotFound(new { error = "Không tìm thấy đơn hàng." });
        }

        return Ok(order);
    }

    /// <summary>
    /// Nhân viên cập nhật trạng thái xử lý đơn hàng (UC16).
    /// </summary>
    [Authorize(Roles = "Employee,Admin")]
    [HttpPut("status")]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus([FromBody] OrderStatusUpdateRequest request)
    {
        var result = await _orderService.UpdateOrderStatusAsync(this.GetUserId(), request);
        if (!result.Succeeded)
        {
            return result.Error == "Không tìm thấy đơn hàng."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}
