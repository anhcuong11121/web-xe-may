using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    /// <summary>
    /// Endpoint công khai - không cần JWT
    /// </summary>
    [HttpGet("public")]
    public ActionResult<object> GetPublicData()
    {
        return Ok(new
        {
            message = "✅ Đây là endpoint công khai - không cần JWT",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Endpoint cần JWT - bất kỳ user nào đã đăng nhập
    /// </summary>
    [Authorize]
    [HttpGet("protected")]
    public ActionResult<object> GetProtectedData()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new
        {
            message = "✅ Endpoint được bảo vệ - cần JWT hợp lệ",
            userEmail = email,
            userRole = role,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Endpoint chỉ Admin truy cập được
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only")]
    public ActionResult<object> GetAdminData()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new
        {
            message = "✅ Endpoint Admin - chỉ Admin truy cập được",
            userEmail = email,
            userRole = role,
            adminFeatures = new[]
            {
                "Quản lý người dùng",
                "Xem báo cáo",
                "Xóa dữ liệu"
            },
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Endpoint cho cả Admin và Employee
    /// </summary>
    [Authorize(Roles = "Admin,Employee")]
    [HttpGet("staff-only")]
    public ActionResult<object> GetStaffData()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new
        {
            message = "✅ Endpoint Staff - chỉ Admin và Employee truy cập được",
            userEmail = email,
            userRole = role,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Endpoint test [Authorize] - bất kỳ user đã đăng nhập nào (Giai đoạn 3)
    /// </summary>
    [Authorize]
    [HttpGet("profile")]
    public ActionResult<object> GetTestProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new
        {
            message = "✅ [Authorize] - đã đăng nhập hợp lệ",
            userId,
            email,
            role,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Endpoint test [Authorize(Roles="Admin")] (Giai đoạn 3)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public ActionResult<object> GetAdmin()
    {
        return Ok(new
        {
            message = "✅ [Authorize(Roles=\"Admin\")] - chỉ Admin truy cập được",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Endpoint test [Authorize(Roles="Employee")] (Giai đoạn 3)
    /// </summary>
    [Authorize(Roles = "Employee")]
    [HttpGet("employee")]
    public ActionResult<object> GetEmployee()
    {
        return Ok(new
        {
            message = "✅ [Authorize(Roles=\"Employee\")] - chỉ Employee truy cập được",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Endpoint test [Authorize(Roles="Customer")] (Giai đoạn 3)
    /// </summary>
    [Authorize(Roles = "Customer")]
    [HttpGet("customer")]
    public ActionResult<object> GetCustomer()
    {
        return Ok(new
        {
            message = "✅ [Authorize(Roles=\"Customer\")] - chỉ Customer truy cập được",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Endpoint cho tất cả authenticated user
    /// </summary>
    [Authorize(Roles = "Admin,Employee,Customer")]
    [HttpGet("user-profile")]
    public ActionResult<object> GetUserProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new
        {
            message = "✅ Lấy profile người dùng thành công",
            userId = userId,
            email = email,
            role = role,
            timestamp = DateTime.UtcNow
        });
    }
}
