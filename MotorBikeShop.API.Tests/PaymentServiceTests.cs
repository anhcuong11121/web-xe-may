using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Tests;

public class PaymentServiceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InitiateAsync_NonPositiveAmount_FailsWithoutCreatingAttempt(decimal amount)
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        context.Orders.Add(CreatePendingOrder(ownerId));
        await context.SaveChangesAsync();
        var service = new PaymentService(context);

        var result = await service.InitiateAsync(ownerId, new PaymentInitiateRequest
        {
            OrderId = 1,
            Amount = amount,
            PaymentMethod = "Fake"
        });

        Assert.False(result.Succeeded);
        Assert.Empty(context.PaymentAttempts);
    }

    [Fact]
    public async Task InitiateAsync_OrderBelongsToAnotherCustomer_FailsWithoutLeakingOrder()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        context.Orders.Add(CreatePendingOrder(ownerId));
        await context.SaveChangesAsync();
        var service = new PaymentService(context);

        var result = await service.InitiateAsync(Guid.NewGuid(), new PaymentInitiateRequest
        {
            OrderId = 1,
            Amount = 2_000_000,
            PaymentMethod = "Fake"
        });

        Assert.False(result.Succeeded);
        Assert.Empty(context.PaymentAttempts);
    }

    [Fact]
    public async Task InitiateAsync_AmountExceedsOrderTotal_Fails()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        context.Orders.Add(CreatePendingOrder(ownerId));
        await context.SaveChangesAsync();
        var service = new PaymentService(context);

        var result = await service.InitiateAsync(ownerId, new PaymentInitiateRequest
        {
            OrderId = 1,
            Amount = 51_000_000,
            PaymentMethod = "Fake"
        });

        Assert.False(result.Succeeded);
        Assert.Empty(context.PaymentAttempts);
    }

    [Fact]
    public async Task InitiateAsync_UnsupportedPaymentMethod_Fails()
    {
        await using var context = CreateContext();
        var service = new PaymentService(context);

        var result = await service.InitiateAsync(Guid.NewGuid(), new PaymentInitiateRequest
        {
            OrderId = 1,
            Amount = 2_000_000,
            PaymentMethod = "Crypto"
        });

        Assert.False(result.Succeeded);
        Assert.Empty(context.PaymentAttempts);
    }

    [Fact]
    public async Task InitiateAsync_SameActiveRequest_ReturnsExistingAttempt()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        context.Orders.Add(CreatePendingOrder(ownerId));
        await context.SaveChangesAsync();
        var service = new PaymentService(context);
        var request = new PaymentInitiateRequest
        {
            OrderId = 1,
            Amount = 2_000_000,
            PaymentMethod = "BankTransfer"
        };

        var first = await service.InitiateAsync(ownerId, request);
        var second = await service.InitiateAsync(ownerId, request);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(first.Data!.Id, second.Data!.Id);
        Assert.Equal(PaymentAttemptStatuses.Pending, first.Data.Status);
        Assert.Single(context.PaymentAttempts);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Order CreatePendingOrder(Guid ownerId) => new()
    {
        Id = 1,
        UserId = ownerId,
        Status = "Pending",
        TotalAmount = 50_000_000,
        OrderDate = DateTime.UtcNow
    };
}
