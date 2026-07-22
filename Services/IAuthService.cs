using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Services;

public interface IAuthService
{
    Task<AuthResult<RegisterResponse>> RegisterAsync(RegisterRequest request);
    Task<AuthResult<LoginResponse>> LoginAsync(LoginRequest request);
    Task<AuthResult<LoginResponse>> RefreshAsync(RefreshTokenRequest request);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task<AuthResult<ChangePasswordResponse>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<ProfileResponse?> GetProfileAsync(Guid userId);
    Task<AuthResult<ProfileResponse>> UpdateProfileAsync(Guid userId, ProfileUpdateRequest request);
}
