using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Tests;

public class DepositServiceTests
{
    [Theory]
    [InlineData("")]
    [InlineData("UnexpectedRole")]
    public async Task GetDepositByOrderIdAsync_UnprivilegedRole_CannotReadAnotherCustomersDeposit(
        string role)
    {
        await using var context = CreateContext();
        var ownerId = await SeedDepositAsync(context);
        var service = new DepositService(context);

        var result = await service.GetDepositByOrderIdAsync(1, Guid.NewGuid(), role);
        var ownerResult = await service.GetDepositByOrderIdAsync(1, ownerId, role);

        Assert.Null(result);
        Assert.NotNull(ownerResult);
        Assert.Equal(1, ownerResult.OrderId);
    }

    [Fact]
    public async Task GetDepositByOrderIdAsync_Employee_CanReadAnotherCustomersDeposit()
    {
        await using var context = CreateContext();
        await SeedDepositAsync(context);
        var service = new DepositService(context);

        var result = await service.GetDepositByOrderIdAsync(1, Guid.NewGuid(), "Employee");

        Assert.NotNull(result);
        Assert.Equal(10_000_000m, result.Amount);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<Guid> SeedDepositAsync(ApplicationDbContext context)
    {
        var ownerId = Guid.NewGuid();
        context.Users.Add(new AppUser
        {
            Id = ownerId,
            UserName = "deposit-owner@example.com",
            FullName = "Deposit Owner"
        });
        context.Orders.Add(new Order
        {
            Id = 1,
            UserId = ownerId,
            Status = "Deposited",
            TotalAmount = 50_000_000,
            OrderDate = DateTime.UtcNow,
            Deposit = new Deposit
            {
                Id = 1,
                Amount = 10_000_000,
                DepositDate = DateTime.UtcNow,
                PaymentMethod = "Fake",
                TransactionCode = "PAY-DEPOSIT-TEST",
                Status = "Completed",
                PaidAt = DateTime.UtcNow
            }
        });
        await context.SaveChangesAsync();
        return ownerId;
    }
}
