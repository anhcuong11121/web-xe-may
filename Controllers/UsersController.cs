using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    /// <summary>
    /// Admin xem danh sách tài khoản (UC18), có phân trang.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        return Ok(await _userManagementService.GetUsersAsync(pageNumber, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetUserById(Guid id)
    {
        var user = await _userManagementService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { error = "Không tìm thấy tài khoản." });
        }

        return Ok(user);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UserUpdateRequest request)
    {
        var result = await _userManagementService.UpdateUserAsync(id, request);
        if (!result.Succeeded)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Admin đổi Role tài khoản (UC19).
    /// </summary>
    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<UserDto>> UpdateUserRole(Guid id, [FromBody] UserRoleUpdateRequest request)
    {
        var result = await _userManagementService.UpdateUserRoleAsync(id, this.GetUserId(), request);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Admin khóa tài khoản.
    /// </summary>
    [HttpPut("{id:guid}/lock")]
    public async Task<ActionResult<UserDto>> LockUser(Guid id)
    {
        var result = await _userManagementService.LockUserAsync(id, this.GetUserId());
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Admin mở khóa tài khoản.
    /// </summary>
    [HttpPut("{id:guid}/unlock")]
    public async Task<ActionResult<UserDto>> UnlockUser(Guid id)
    {
        var result = await _userManagementService.UnlockUserAsync(id);
        if (!result.Succeeded)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}
