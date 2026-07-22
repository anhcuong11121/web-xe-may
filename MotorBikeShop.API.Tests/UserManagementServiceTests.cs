using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.Controllers;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Tests;

public class UserManagementServiceTests
{
    [Fact]
    public void CustomersController_AllowsOnlyEmployeeAndAdminRoles()
    {
        var authorize = Assert.Single(typeof(CustomersController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>());

        Assert.Equal("Employee,Admin", authorize.Roles);
    }

    [Fact]
    public void EmployeesController_AllowsOnlyAdminRole()
    {
        var authorize = Assert.Single(typeof(EmployeesController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>());

        Assert.Equal("Admin", authorize.Roles);
    }

    [Fact]
    public async Task CreateAndUpdateEmployeeAsync_SynchronizesIdentityAndEmployeeProfile()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.AddIdentityCore<AppUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var service = new UserManagementService(userManager, roleManager, context);

        var created = await service.CreateEmployeeAsync(new EmployeeCreateRequest
        {
            Email = "new-employee@example.com",
            FullName = "New Employee",
            PhoneNumber = "0901111111",
            Password = "StrongPass1!"
        });
        var updated = await service.UpdateEmployeeAsync(created.Data!.Id, new EmployeeUpdateRequest
        {
            FullName = "Updated Employee",
            PhoneNumber = "0902222222"
        });

        Assert.True(created.Succeeded);
        Assert.True(updated.Succeeded);
        Assert.True(await userManager.IsInRoleAsync(
            (await userManager.FindByIdAsync(created.Data.Id.ToString()))!, "Employee"));
        context.ChangeTracker.Clear();
        var user = await context.Users.AsNoTracking().SingleAsync(candidate => candidate.Id == created.Data.Id);
        var profile = await context.EmployeeProfiles.AsNoTracking().SingleAsync(candidate => candidate.UserId == created.Data.Id);
        Assert.Equal("Updated Employee", user.FullName);
        Assert.Equal("0902222222", user.PhoneNumber);
        Assert.Equal(user.FullName, profile.FullName);
        Assert.Equal(user.PhoneNumber, profile.PhoneNumber);
    }

    [Fact]
    public async Task GetCustomersAsync_ReturnsOnlyCustomerRoleWithProfileData()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.AddIdentityCore<AppUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        Assert.True((await roleManager.CreateAsync(new IdentityRole<Guid>("Customer"))).Succeeded);
        Assert.True((await roleManager.CreateAsync(new IdentityRole<Guid>("Employee"))).Succeeded);

        var customer = new AppUser
        {
            UserName = "customer-list@example.com",
            Email = "customer-list@example.com",
            FullName = "Identity Customer"
        };
        var employee = new AppUser
        {
            UserName = "employee-list@example.com",
            Email = "employee-list@example.com",
            FullName = "Employee"
        };
        Assert.True((await userManager.CreateAsync(customer, "StrongPass1!")).Succeeded);
        Assert.True((await userManager.CreateAsync(employee, "StrongPass1!")).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(customer, "Customer")).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(employee, "Employee")).Succeeded);
        context.CustomerProfiles.Add(new CustomerProfile
        {
            UserId = customer.Id,
            FullName = "Profile Customer",
            PhoneNumber = "0901234567",
            Email = customer.Email!
        });
        await context.SaveChangesAsync();
        var service = new UserManagementService(userManager, roleManager, context);

        var result = await service.GetCustomersAsync(1, 20);
        var detail = await service.GetCustomerByIdAsync(customer.Id);
        var employeeDetail = await service.GetCustomerByIdAsync(employee.Id);

        var item = Assert.Single(result.Items);
        Assert.Equal(customer.Id, item.Id);
        Assert.Equal("Profile Customer", item.FullName);
        Assert.Equal("0901234567", item.PhoneNumber);
        Assert.Equal(0, item.TotalOrders);
        Assert.Equal(item.Id, detail!.Id);
        Assert.Null(employeeDetail);
    }

    [Fact]
    public async Task LockUserAsync_ChangesSecurityStampAndRevokesRefreshTokens()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.AddIdentityCore<AppUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        await roleManager.CreateAsync(new IdentityRole<Guid>("Customer"));
        var user = new AppUser
        {
            UserName = "lock-user@example.com",
            Email = "lock-user@example.com",
            FullName = "Lock User",
            IsActive = true
        };
        Assert.True((await userManager.CreateAsync(user, "StrongPass1!")).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, "Customer")).Succeeded);
        var originalSecurityStamp = user.SecurityStamp;
        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = new string('A', 64),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await context.SaveChangesAsync();
        var service = new UserManagementService(userManager, roleManager, context);

        var result = await service.LockUserAsync(user.Id, Guid.NewGuid());

        Assert.True(result.Succeeded);
        context.ChangeTracker.Clear();
        var lockedUser = await context.Users.SingleAsync(candidate => candidate.Id == user.Id);
        var refreshToken = await context.RefreshTokens.AsNoTracking().SingleAsync();
        Assert.False(lockedUser.IsActive);
        Assert.NotEqual(originalSecurityStamp, lockedUser.SecurityStamp);
        Assert.NotNull(refreshToken.RevokedAt);
    }

    [Fact]
    public async Task UpdateUserAsync_IdentityRejectsUpdate_ReturnsFailure()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<AppUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<IUserValidator<AppUser>, RejectingUserValidator>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userId = Guid.NewGuid();
        context.Users.Add(new AppUser
        {
            Id = userId,
            UserName = "managed-user@example.com",
            NormalizedUserName = "MANAGED-USER@EXAMPLE.COM",
            Email = "managed-user@example.com",
            NormalizedEmail = "MANAGED-USER@EXAMPLE.COM",
            FullName = "Original Name",
            SecurityStamp = Guid.NewGuid().ToString()
        });
        await context.SaveChangesAsync();
        var service = new UserManagementService(
            scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(),
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
            context);

        var result = await service.UpdateUserAsync(userId, new UserUpdateRequest
        {
            FullName = "Changed Name"
        });

        Assert.False(result.Succeeded);
        Assert.Equal("User update rejected for test.", result.Error);
        context.ChangeTracker.Clear();
        Assert.Equal("Original Name", (await context.Users.FindAsync(userId))!.FullName);
    }

    private sealed class RejectingUserValidator : IUserValidator<AppUser>
    {
        public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user)
        {
            return Task.FromResult(IdentityResult.Failed(
                new IdentityError
                {
                    Code = "RejectedForTest",
                    Description = "User update rejected for test."
                }));
        }
    }
}
