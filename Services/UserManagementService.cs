using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class UserManagementService : IUserManagementService
{
    private static readonly string[] AllowedRoles = { "Customer", "Employee", "Admin" };

    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ApplicationDbContext _context;

    public UserManagementService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(int pageNumber, int pageSize)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 100);

        var totalCount = await _userManager.Users.CountAsync();

        var users = await _userManager.Users
            .OrderBy(u => u.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<UserDto>();
        foreach (var user in users)
        {
            items.Add(await MapToDtoAsync(user));
        }

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        return user == null ? null : await MapToDtoAsync(user);
    }

    public async Task<PagedResult<CustomerDto>> GetCustomersAsync(int pageNumber, int pageSize)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 100);

        var customerRole = await _roleManager.FindByNameAsync("Customer");
        if (customerRole == null)
        {
            return new PagedResult<CustomerDto> { PageNumber = pageNumber, PageSize = pageSize };
        }

        var query = _context.Users.AsNoTracking().Where(user =>
            _context.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RoleId == customerRole.Id));
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(user => user.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new CustomerDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.CustomerProfile != null ? user.CustomerProfile.FullName : user.FullName,
                PhoneNumber = user.CustomerProfile != null ? user.CustomerProfile.PhoneNumber : user.PhoneNumber ?? string.Empty,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                TotalOrders = user.Orders.Count
            })
            .ToListAsync();

        return new PagedResult<CustomerDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id)
    {
        var customerRole = await _roleManager.FindByNameAsync("Customer");
        if (customerRole == null) return null;

        return await _context.Users.AsNoTracking()
            .Where(user => user.Id == id && _context.UserRoles.Any(userRole =>
                userRole.UserId == user.Id && userRole.RoleId == customerRole.Id))
            .Select(user => new CustomerDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.CustomerProfile != null ? user.CustomerProfile.FullName : user.FullName,
                PhoneNumber = user.CustomerProfile != null ? user.CustomerProfile.PhoneNumber : user.PhoneNumber ?? string.Empty,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                TotalOrders = user.Orders.Count
            })
            .SingleOrDefaultAsync();
    }

    public async Task<PagedResult<EmployeeDto>> GetEmployeesAsync(int pageNumber, int pageSize)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 100);
        var employeeRole = await _roleManager.FindByNameAsync("Employee");
        if (employeeRole == null)
        {
            return new PagedResult<EmployeeDto> { PageNumber = pageNumber, PageSize = pageSize };
        }

        var query = EmployeeQuery(employeeRole.Id);
        var totalCount = await query.CountAsync();
        var items = await query.OrderBy(employee => employee.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PagedResult<EmployeeDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(Guid id)
    {
        var employeeRole = await _roleManager.FindByNameAsync("Employee");
        return employeeRole == null
            ? null
            : await EmployeeQuery(employeeRole.Id).SingleOrDefaultAsync(employee => employee.Id == id);
    }

    public async Task<ServiceResult<EmployeeDto>> CreateEmployeeAsync(EmployeeCreateRequest request)
    {
        var email = request.Email.Trim();
        var fullName = request.FullName.Trim();
        var phoneNumber = request.PhoneNumber.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phoneNumber))
        {
            return ServiceResult<EmployeeDto>.Fail("Thông tin nhân viên không được để trống.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        if (!await _roleManager.RoleExistsAsync("Employee"))
        {
            var roleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>("Employee"));
            if (!roleResult.Succeeded) return ServiceResult<EmployeeDto>.Fail(roleResult.Errors.First().Description);
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            IsActive = true
        };
        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded) return ServiceResult<EmployeeDto>.Fail(createResult.Errors.First().Description);

        var roleAssignment = await _userManager.AddToRoleAsync(user, "Employee");
        if (!roleAssignment.Succeeded) return ServiceResult<EmployeeDto>.Fail(roleAssignment.Errors.First().Description);

        _context.EmployeeProfiles.Add(new EmployeeProfile
        {
            UserId = user.Id,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Email = email
        });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return ServiceResult<EmployeeDto>.Success(MapEmployee(user, fullName, phoneNumber));
    }

    public async Task<ServiceResult<EmployeeDto>> UpdateEmployeeAsync(Guid id, EmployeeUpdateRequest request)
    {
        var employee = await GetEmployeeByIdAsync(id);
        if (employee == null) return ServiceResult<EmployeeDto>.Fail("Không tìm thấy nhân viên.");

        var fullName = request.FullName.Trim();
        var phoneNumber = request.PhoneNumber.Trim();
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phoneNumber))
        {
            return ServiceResult<EmployeeDto>.Fail("Thông tin nhân viên không được để trống.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var user = await _context.Users.Include(candidate => candidate.EmployeeProfile)
            .SingleAsync(candidate => candidate.Id == id);
        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;
        user.EmployeeProfile ??= new EmployeeProfile { UserId = id, Email = user.Email ?? string.Empty };
        user.EmployeeProfile.FullName = fullName;
        user.EmployeeProfile.PhoneNumber = phoneNumber;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded) return ServiceResult<EmployeeDto>.Fail(updateResult.Errors.First().Description);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return ServiceResult<EmployeeDto>.Success(MapEmployee(user, fullName, phoneNumber));
    }

    private IQueryable<EmployeeDto> EmployeeQuery(Guid employeeRoleId)
    {
        return _context.Users.AsNoTracking()
            .Where(user => _context.UserRoles.Any(userRole =>
                userRole.UserId == user.Id && userRole.RoleId == employeeRoleId))
            .Select(user => new EmployeeDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.EmployeeProfile != null ? user.EmployeeProfile.FullName : user.FullName,
                PhoneNumber = user.EmployeeProfile != null ? user.EmployeeProfile.PhoneNumber : user.PhoneNumber ?? string.Empty,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
    }

    private static EmployeeDto MapEmployee(AppUser user, string fullName, string phoneNumber) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        FullName = fullName,
        PhoneNumber = phoneNumber,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };

    public async Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserUpdateRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return ServiceResult<UserDto>.Fail("Không tìm thấy tài khoản.");
        }

        user.FullName = request.FullName;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ServiceResult<UserDto>.Fail(updateResult.Errors.First().Description);
        }

        return ServiceResult<UserDto>.Success(await MapToDtoAsync(user));
    }

    public async Task<ServiceResult<UserDto>> UpdateUserRoleAsync(
        Guid id,
        Guid currentAdminId,
        UserRoleUpdateRequest request)
    {
        if (!AllowedRoles.Contains(request.Role))
        {
            return ServiceResult<UserDto>.Fail($"Role không hợp lệ. Cho phép: {string.Join(", ", AllowedRoles)}.");
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return ServiceResult<UserDto>.Fail("Không tìm thấy tài khoản.");
        }

        if (id == currentAdminId && request.Role != "Admin")
        {
            return ServiceResult<UserDto>.Fail("Admin không thể tự hạ quyền của chính mình.");
        }

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>(request.Role));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (user.IsActive && currentRoles.Contains("Admin") && request.Role != "Admin")
        {
            var activeAdmins = await _userManager.GetUsersInRoleAsync("Admin");
            if (activeAdmins.Count(u => u.IsActive) <= 1)
            {
                return ServiceResult<UserDto>.Fail("Không thể hạ quyền Admin đang hoạt động cuối cùng.");
            }
        }

        if (currentRoles.Count == 1 && currentRoles[0] == request.Role)
        {
            await transaction.CommitAsync();
            return ServiceResult<UserDto>.Success(await MapToDtoAsync(user));
        }

        if (currentRoles.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                return ServiceResult<UserDto>.Fail(removeResult.Errors.First().Description);
            }
        }

        var addResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!addResult.Succeeded)
        {
            return ServiceResult<UserDto>.Fail(addResult.Errors.First().Description);
        }

        await RevokeActiveRefreshTokensAsync(user.Id);
        await transaction.CommitAsync();

        return ServiceResult<UserDto>.Success(await MapToDtoAsync(user));
    }

    public async Task<ServiceResult<UserDto>> LockUserAsync(Guid id, Guid currentAdminId)
    {
        if (id == currentAdminId)
        {
            return ServiceResult<UserDto>.Fail("Admin không thể tự khóa tài khoản của chính mình.");
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return ServiceResult<UserDto>.Fail("Không tìm thấy tài khoản.");
        }

        if (!user.IsActive)
        {
            return ServiceResult<UserDto>.Success(await MapToDtoAsync(user));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            var activeAdmins = await _userManager.GetUsersInRoleAsync("Admin");
            if (activeAdmins.Count(u => u.IsActive) <= 1)
            {
                return ServiceResult<UserDto>.Fail("Không thể khóa Admin đang hoạt động cuối cùng.");
            }
        }

        user.IsActive = false;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ServiceResult<UserDto>.Fail(updateResult.Errors.First().Description);
        }

        var securityStampResult = await _userManager.UpdateSecurityStampAsync(user);
        if (!securityStampResult.Succeeded)
        {
            return ServiceResult<UserDto>.Fail(securityStampResult.Errors.First().Description);
        }

        await RevokeActiveRefreshTokensAsync(user.Id);
        await transaction.CommitAsync();

        return ServiceResult<UserDto>.Success(await MapToDtoAsync(user));
    }

    public async Task<ServiceResult<UserDto>> UnlockUserAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return ServiceResult<UserDto>.Fail("Không tìm thấy tài khoản.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        user.IsActive = true;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ServiceResult<UserDto>.Fail(updateResult.Errors.First().Description);
        }

        var lockoutResult = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!lockoutResult.Succeeded)
        {
            return ServiceResult<UserDto>.Fail(lockoutResult.Errors.First().Description);
        }

        var resetResult = await _userManager.ResetAccessFailedCountAsync(user);
        if (!resetResult.Succeeded)
        {
            return ServiceResult<UserDto>.Fail(resetResult.Errors.First().Description);
        }

        await transaction.CommitAsync();

        return ServiceResult<UserDto>.Success(await MapToDtoAsync(user));
    }

    private async Task<UserDto> MapToDtoAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Role = roles.FirstOrDefault() ?? "Customer",
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    private Task RevokeActiveRefreshTokensAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        return _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, now));
    }
}
