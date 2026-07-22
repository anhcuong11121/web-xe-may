using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task UpdateProfileAsync_Customer_UpdatesIdentityAndCustomerProfile()
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
        var user = new AppUser
        {
            UserName = "profile-customer@example.com",
            Email = "profile-customer@example.com",
            FullName = "Old Name",
            PhoneNumber = "0900000000",
            IsActive = true
        };
        Assert.True((await userManager.CreateAsync(user, "StrongPass1!")).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, "Customer")).Succeeded);
        context.CustomerProfiles.Add(new CustomerProfile
        {
            UserId = user.Id,
            FullName = "Old Name",
            PhoneNumber = "0900000000",
            Email = user.Email
        });
        await context.SaveChangesAsync();
        var service = CreateService(scope.ServiceProvider, context);

        var result = await service.UpdateProfileAsync(user.Id, new ProfileUpdateRequest
        {
            FullName = "  New Name  ",
            PhoneNumber = " 0987654321 "
        });

        Assert.True(result.Succeeded);
        Assert.Equal("New Name", result.Data!.FullName);
        Assert.Equal("0987654321", result.Data.PhoneNumber);
        context.ChangeTracker.Clear();
        var storedUser = await context.Users.SingleAsync(candidate => candidate.Id == user.Id);
        var storedProfile = await context.CustomerProfiles.SingleAsync(profile => profile.UserId == user.Id);
        Assert.Equal("New Name", storedUser.FullName);
        Assert.Equal("0987654321", storedUser.PhoneNumber);
        Assert.Equal("New Name", storedProfile.FullName);
        Assert.Equal("0987654321", storedProfile.PhoneNumber);
    }

    [Fact]
    public async Task LoginAsync_ResetAccessFailedCountFails_DoesNotIssueTokens()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<AppUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<IUserValidator<AppUser>, RejectingSecondUserValidation>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser
        {
            UserName = "login-reset-failure@example.com",
            Email = "login-reset-failure@example.com",
            FullName = "Login Test",
            IsActive = true
        };
        var createResult = await userManager.CreateAsync(user, "StrongPass1!");
        Assert.True(createResult.Succeeded);
        user.AccessFailedCount = 1;
        await context.SaveChangesAsync();
        var service = CreateService(scope.ServiceProvider, context);

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "StrongPass1!"
        });

        Assert.False(result.Succeeded);
        Assert.Null(result.Data);
        Assert.Empty(context.RefreshTokens);
    }

    [Fact]
    public async Task RegisterAsync_RelationalRoleCreationFails_RollsBackCreatedUser()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.AddIdentityCore<AppUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<IRoleValidator<IdentityRole<Guid>>, RejectingRoleValidator>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        var service = CreateService(scope.ServiceProvider, context);

        var result = await service.RegisterAsync(CreateRegisterRequest("relational-role-failure@example.com"));

        Assert.False(result.Succeeded);
        context.ChangeTracker.Clear();
        Assert.Empty(await context.Users.AsNoTracking().ToListAsync());
        Assert.Empty(await context.CustomerProfiles.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task RegisterAsync_RoleCreationFails_RemovesCreatedUser()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<AppUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<IRoleValidator<IdentityRole<Guid>>, RejectingRoleValidator>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = CreateService(scope.ServiceProvider, context);

        var result = await service.RegisterAsync(CreateRegisterRequest("register-role-failure@example.com"));

        Assert.False(result.Succeeded);
        Assert.Contains("Role creation rejected for test.", result.Errors);
        Assert.Empty(await context.Users.AsNoTracking().ToListAsync());
        Assert.Empty(await context.CustomerProfiles.AsNoTracking().ToListAsync());
    }

    private static AuthService CreateService(IServiceProvider provider, ApplicationDbContext context) =>
        new(
            provider.GetRequiredService<UserManager<AppUser>>(),
            provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
            context,
            Options.Create(new JwtSettings
            {
                SecretKey = "test-secret-key-at-least-32-bytes-long",
                Issuer = "Test",
                Audience = "Test",
                ExpiryMinutes = 15,
                RefreshTokenExpiryDays = 7
            }));

    private static RegisterRequest CreateRegisterRequest(string email) => new()
    {
        Email = email,
        Password = "StrongPass1!",
        ConfirmPassword = "StrongPass1!",
        FullName = "Registration Test",
        PhoneNumber = "0987654321"
    };

    private sealed class RejectingRoleValidator : IRoleValidator<IdentityRole<Guid>>
    {
        public Task<IdentityResult> ValidateAsync(
            RoleManager<IdentityRole<Guid>> manager,
            IdentityRole<Guid> role)
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "RejectedForTest",
                Description = "Role creation rejected for test."
            }));
        }
    }

    private sealed class RejectingSecondUserValidation : IUserValidator<AppUser>
    {
        private int _validationCount;

        public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user)
        {
            _validationCount++;
            return Task.FromResult(_validationCount == 1
                ? IdentityResult.Success
                : IdentityResult.Failed(new IdentityError
                {
                    Code = "ResetRejectedForTest",
                    Description = "Reset rejected for test."
                }));
        }
    }
}
