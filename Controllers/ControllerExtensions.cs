using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace MotorBikeShop.API.Controllers;

/// <summary>
/// Helper dùng chung để đọc UserId/Role từ JWT Claims trong các Controller cần phân quyền theo chủ sở hữu dữ liệu.
/// </summary>
public static class ControllerExtensions
{
    public static Guid GetUserId(this ControllerBase controller)
    {
        var claim = controller.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var userId) ? userId : Guid.Empty;
    }

    public static string GetUserRole(this ControllerBase controller)
    {
        return controller.User.FindFirstValue(ClaimTypes.Role) ?? "Customer";
    }
}
