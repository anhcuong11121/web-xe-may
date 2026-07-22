using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Đăng ký tài khoản mới. Role mặc định luôn là Customer.
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting("Auth")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Đăng nhập bằng Email + Password, trả về JWT.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("Auth")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (!result.Succeeded)
        {
            return Unauthorized(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("Token")]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshAsync(request);
        if (!result.Succeeded)
        {
            return Unauthorized(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Đăng xuất bằng cách thu hồi refresh token. Access token hiện tại vẫn tự hết hạn
    /// theo thời gian sống ngắn đã cấu hình.
    /// </summary>
    [HttpPost("logout")]
    [EnableRateLimiting("Token")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
        return Ok(new { message = "Đăng xuất thành công." });
    }

    /// <summary>
    /// Lấy thông tin tài khoản đang đăng nhập, đọc từ Claims trong JWT.
    /// </summary>
    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<ProfileResponse>> Profile()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var profile = await _authService.GetProfileAsync(userId);
        if (profile == null)
        {
            return NotFound();
        }

        return Ok(profile);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<ProfileResponse>> UpdateProfile(
        [FromBody] ProfileUpdateRequest request)
    {
        var result = await _authService.UpdateProfileAsync(this.GetUserId(), request);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    [Authorize]
    [HttpPost("change-password")]
    [EnableRateLimiting("Auth")]
    public async Task<ActionResult<ChangePasswordResponse>> ChangePassword(
        [FromBody] ChangePasswordRequest request)
    {
        var result = await _authService.ChangePasswordAsync(this.GetUserId(), request);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }
}
