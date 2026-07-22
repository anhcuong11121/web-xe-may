using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Tests;

public class SupportRequestServiceTests
{
    [Fact]
    public async Task UpdateAsync_OpenToResolved_IsRejected()
    {
        await using var context = CreateContext();
        var (_, employeeId) = await SeedRequestAsync(context, "Open");
        var service = new SupportRequestService(context);

        var result = await service.UpdateAsync(1, employeeId, new SupportRequestUpdateRequest
        {
            Status = "Resolved",
            Response = "Đã xử lý."
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Open", (await context.SupportRequests.FindAsync(1))!.Status);
    }

    [Fact]
    public async Task UpdateAsync_ClosedToOpen_IsRejected()
    {
        await using var context = CreateContext();
        var (_, employeeId) = await SeedRequestAsync(context, "Closed");
        var service = new SupportRequestService(context);

        var result = await service.UpdateAsync(1, employeeId, new SupportRequestUpdateRequest
        {
            Status = "Open"
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Closed", (await context.SupportRequests.FindAsync(1))!.Status);
    }

    [Fact]
    public async Task UpdateAsync_ValidWorkflow_ReachesClosed()
    {
        await using var context = CreateContext();
        var (_, employeeId) = await SeedRequestAsync(context, "Open");
        var service = new SupportRequestService(context);

        var inProgress = await service.UpdateAsync(1, employeeId,
            new SupportRequestUpdateRequest { Status = "InProgress" });
        var resolved = await service.UpdateAsync(1, employeeId,
            new SupportRequestUpdateRequest { Status = "Resolved", Response = "Đã xử lý." });
        var closed = await service.UpdateAsync(1, employeeId,
            new SupportRequestUpdateRequest { Status = "Closed" });

        Assert.True(inProgress.Succeeded);
        Assert.True(resolved.Succeeded);
        Assert.True(closed.Succeeded);
        Assert.Equal("Closed", closed.Data!.Status);
        Assert.Equal("Đã xử lý.", closed.Data.Response);
        Assert.NotNull(closed.Data.RespondedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("UnexpectedRole")]
    public async Task ReadAsync_UnprivilegedRole_CanOnlyReadOwnRequests(string role)
    {
        await using var context = CreateContext();
        var (customerId, _) = await SeedRequestAsync(context, "Open");
        var otherCustomerId = Guid.NewGuid();
        context.Users.Add(new AppUser
        {
            Id = otherCustomerId,
            UserName = "other-support-customer@example.com",
            FullName = "Other Customer"
        });
        context.SupportRequests.Add(new SupportRequest
        {
            Id = 2,
            UserId = otherCustomerId,
            SupportType = "General",
            Subject = "Other request",
            Message = "Other message",
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var service = new SupportRequestService(context);

        var list = await service.GetRequestsAsync(customerId, role);
        var otherRequest = await service.GetByIdAsync(2, customerId, role);

        Assert.Single(list);
        Assert.Equal(customerId, list[0].UserId);
        Assert.Null(otherRequest);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<(Guid CustomerId, Guid EmployeeId)> SeedRequestAsync(
        ApplicationDbContext context,
        string status)
    {
        var customerId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        context.Users.AddRange(
            new AppUser { Id = customerId, UserName = "support-customer@example.com", FullName = "Customer" },
            new AppUser { Id = employeeId, UserName = "support-employee@example.com", FullName = "Employee" });
        context.SupportRequests.Add(new SupportRequest
        {
            Id = 1,
            UserId = customerId,
            SupportType = "General",
            Subject = "Cần hỗ trợ",
            Message = "Nội dung yêu cầu",
            Status = status,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        return (customerId, employeeId);
    }
}
