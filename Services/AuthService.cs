using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly JwtSettings _jwtSettings;
    private readonly ApplicationDbContext _context;

    public AuthService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _jwtSettings = jwtSettings.Value;
    }

    private const string DefaultRole = "Customer";
    private const string LoginFailureMessage = "Email hoặc mật khẩu không đúng, hoặc tài khoản tạm thời bị khóa.";

    public async Task<AuthResult<RegisterResponse>> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return AuthResult<RegisterResponse>.Fail("Email đã tồn tại.");
        }

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CustomerProfile = new CustomerProfile
            {
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email
            }
        };

        IDbContextTransaction? transaction = null;
        if (_context.Database.IsRelational())
        {
            transaction = await _context.Database.BeginTransactionAsync();
        }

        await using var transactionScope = transaction;

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return AuthResult<RegisterResponse>.Fail(result.Errors.Select(e => e.Description).ToArray());
        }

        // Mọi tài khoản tự đăng ký đều được gán Role mặc định là Customer.
        // Không cho phép client tự chọn Role (tránh leo thang đặc quyền).
        if (!await _roleManager.RoleExistsAsync(DefaultRole))
        {
            var createRoleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(DefaultRole));
            if (!createRoleResult.Succeeded)
            {
                await RollbackRegistrationAsync(user, transaction);
                return AuthResult<RegisterResponse>.Fail(
                    createRoleResult.Errors.Select(error => error.Description).ToArray());
            }
        }

        var addToRoleResult = await _userManager.AddToRoleAsync(user, DefaultRole);
        if (!addToRoleResult.Succeeded)
        {
            await RollbackRegistrationAsync(user, transaction);
            return AuthResult<RegisterResponse>.Fail(
                addToRoleResult.Errors.Select(error => error.Description).ToArray());
        }

        if (transaction != null)
        {
            await transaction.CommitAsync();
        }

        return AuthResult<RegisterResponse>.Success(new RegisterResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Role = DefaultRole,
            CreatedAt = user.CreatedAt
        });
    }

    public async Task<AuthResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            return AuthResult<LoginResponse>.Fail(LoginFailureMessage);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return AuthResult<LoginResponse>.Fail(LoginFailureMessage);
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            if (await _userManager.IsLockedOutAsync(user))
            {
                return AuthResult<LoginResponse>.Fail(LoginFailureMessage);
            }

            return AuthResult<LoginResponse>.Fail(LoginFailureMessage);
        }

        var resetAccessFailedResult = await _userManager.ResetAccessFailedCountAsync(user);
        if (!resetAccessFailedResult.Succeeded)
        {
            return AuthResult<LoginResponse>.Fail(LoginFailureMessage);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? DefaultRole;

        var (token, expiresAt) = GenerateJwtToken(user, role);
        var (refreshToken, refreshTokenEntity) = GenerateRefreshToken(user.Id);
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return AuthResult<LoginResponse>.Success(CreateLoginResponse(
            user, role, token, expiresAt, refreshToken, refreshTokenEntity.ExpiresAt));
    }

    public async Task<AuthResult<LoginResponse>> RefreshAsync(RefreshTokenRequest request)
    {
        var tokenHash = HashRefreshToken(request.RefreshToken);
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var storedToken = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        var now = DateTime.UtcNow;
        if (storedToken == null ||
            storedToken.RevokedAt != null ||
            storedToken.ExpiresAt <= now ||
            !storedToken.User.IsActive ||
            await _userManager.IsLockedOutAsync(storedToken.User))
        {
            return AuthResult<LoginResponse>.Fail("Refresh token không hợp lệ hoặc đã hết hạn.");
        }

        var revoked = await _context.RefreshTokens
            .Where(t => t.Id == storedToken.Id && t.RevokedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, now));
        if (revoked == 0)
        {
            return AuthResult<LoginResponse>.Fail("Refresh token đã được sử dụng hoặc thu hồi.");
        }

        var roles = await _userManager.GetRolesAsync(storedToken.User);
        var role = roles.FirstOrDefault() ?? DefaultRole;
        var (accessToken, accessTokenExpiresAt) = GenerateJwtToken(storedToken.User, role);
        var (newRefreshToken, newRefreshTokenEntity) = GenerateRefreshToken(storedToken.UserId);
        _context.RefreshTokens.Add(newRefreshTokenEntity);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return AuthResult<LoginResponse>.Success(CreateLoginResponse(
            storedToken.User,
            role,
            accessToken,
            accessTokenExpiresAt,
            newRefreshToken,
            newRefreshTokenEntity.ExpiresAt));
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var now = DateTime.UtcNow;
        await _context.RefreshTokens
            .Where(t => t.TokenHash == tokenHash && t.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, now));
    }

    public async Task<AuthResult<ChangePasswordResponse>> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsActive)
        {
            return AuthResult<ChangePasswordResponse>.Fail("Không tìm thấy tài khoản đang hoạt động.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var changeResult = await _userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);
        if (!changeResult.Succeeded)
        {
            return AuthResult<ChangePasswordResponse>.Fail(
                changeResult.Errors.Select(error => error.Description).ToArray());
        }

        var now = DateTime.UtcNow;
        await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, now));
        await transaction.CommitAsync();

        return AuthResult<ChangePasswordResponse>.Success(new ChangePasswordResponse
        {
            Message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại."
        });
    }

    public async Task<ProfileResponse?> GetProfileAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? DefaultRole;

        return new ProfileResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Role = role,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<AuthResult<ProfileResponse>> UpdateProfileAsync(
        Guid userId,
        ProfileUpdateRequest request)
    {
        var fullName = request.FullName.Trim();
        var phoneNumber = request.PhoneNumber.Trim();
        if (fullName.Length == 0 || phoneNumber.Length == 0)
        {
            return AuthResult<ProfileResponse>.Fail("Họ tên và số điện thoại không được để trống.");
        }

        var user = await _context.Users
            .Include(candidate => candidate.CustomerProfile)
            .Include(candidate => candidate.EmployeeProfile)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId);
        if (user == null || !user.IsActive)
        {
            return AuthResult<ProfileResponse>.Fail("Không tìm thấy tài khoản đang hoạt động.");
        }

        IDbContextTransaction? transaction = null;
        if (_context.Database.IsRelational())
        {
            transaction = await _context.Database.BeginTransactionAsync();
        }

        await using var transactionScope = transaction;
        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return AuthResult<ProfileResponse>.Fail(
                updateResult.Errors.Select(error => error.Description).ToArray());
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? DefaultRole;
        if (role == "Customer")
        {
            user.CustomerProfile ??= new CustomerProfile
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty
            };
            user.CustomerProfile.FullName = fullName;
            user.CustomerProfile.PhoneNumber = phoneNumber;
        }
        else if (role == "Employee")
        {
            user.EmployeeProfile ??= new EmployeeProfile
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty
            };
            user.EmployeeProfile.FullName = fullName;
            user.EmployeeProfile.PhoneNumber = phoneNumber;
        }

        await _context.SaveChangesAsync();
        if (transaction != null)
        {
            await transaction.CommitAsync();
        }

        return AuthResult<ProfileResponse>.Success(new ProfileResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Role = role,
            CreatedAt = user.CreatedAt
        });
    }

    private (string Token, DateTime ExpiresAt) GenerateJwtToken(AppUser user, string role)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, role),
            new("FullName", user.FullName),
            new("SecurityStamp", user.SecurityStamp ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private (string RawToken, RefreshToken Entity) GenerateRefreshToken(Guid userId)
    {
        var rawToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        return (rawToken, new RefreshToken
        {
            UserId = userId,
            TokenHash = HashRefreshToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
        });
    }

    private static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private async Task RollbackRegistrationAsync(AppUser user, IDbContextTransaction? transaction)
    {
        if (transaction != null)
        {
            await transaction.RollbackAsync();
            return;
        }

        await _userManager.DeleteAsync(user);
    }

    private static LoginResponse CreateLoginResponse(
        AppUser user,
        string role,
        string accessToken,
        DateTime accessTokenExpiresAt,
        string refreshToken,
        DateTime refreshTokenExpiresAt) => new()
    {
        Token = accessToken,
        UserId = user.Id,
        Email = user.Email ?? string.Empty,
        FullName = user.FullName,
        Role = role,
        ExpiresAt = accessTokenExpiresAt,
        RefreshToken = refreshToken,
        RefreshTokenExpiresAt = refreshTokenExpiresAt
    };
}
