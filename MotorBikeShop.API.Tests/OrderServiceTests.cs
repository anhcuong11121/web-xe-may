using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Tests;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_RelationalDatabase_AtomicallyDecrementsStock()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var customerId = await SeedProductForCreationAsync(context, stockQuantity: 5);
        context.ChangeTracker.Clear();
        var service = new OrderService(context);

        var result = await service.CreateOrderAsync(customerId, CreateOrderRequest(
            new OrderItemCreateRequest { ProductId = 1, Quantity = 3 }));

        Assert.True(result.Succeeded);
        Assert.Equal(2, (await context.Products.AsNoTracking().SingleAsync()).StockQuantity);
        Assert.Single(await context.Orders.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task CreateOrderAsync_DuplicateProductLines_AreAggregated()
    {
        await using var context = CreateContext();
        var customerId = await SeedProductForCreationAsync(context, stockQuantity: 5);
        var service = new OrderService(context);
        var request = CreateOrderRequest(
            new OrderItemCreateRequest { ProductId = 1, Quantity = 2 },
            new OrderItemCreateRequest { ProductId = 1, Quantity = 1 });

        var result = await service.CreateOrderAsync(customerId, request);

        Assert.True(result.Succeeded);
        Assert.Single(result.Data!.Items);
        Assert.Equal(3, result.Data.Items[0].Quantity);
        Assert.Equal(150_000_000m, result.Data.TotalAmount);
        Assert.Equal(2, (await context.Products.FindAsync(1))!.StockQuantity);
    }

    [Fact]
    public async Task CreateOrderAsync_DuplicateLinesExceedCombinedStock_IsRejected()
    {
        await using var context = CreateContext();
        var customerId = await SeedProductForCreationAsync(context, stockQuantity: 3);
        var service = new OrderService(context);
        var request = CreateOrderRequest(
            new OrderItemCreateRequest { ProductId = 1, Quantity = 2 },
            new OrderItemCreateRequest { ProductId = 1, Quantity = 2 });

        var result = await service.CreateOrderAsync(customerId, request);

        Assert.False(result.Succeeded);
        Assert.Empty(context.Orders);
        Assert.Equal(3, (await context.Products.FindAsync(1))!.StockQuantity);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_PendingToConfirmed_IsRejected()
    {
        await using var context = CreateContext();
        var (_, processorId) = await SeedOrderAsync(context, "Pending", stockQuantity: 3);
        var service = new OrderService(context);

        var result = await service.UpdateOrderStatusAsync(processorId, new OrderStatusUpdateRequest
        {
            OrderId = 1,
            Status = "Confirmed"
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Pending", (await context.Orders.FindAsync(1))!.Status);
        Assert.Equal(3, (await context.Products.FindAsync(1))!.StockQuantity);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_CancelPendingOrder_RestoresStockOnce()
    {
        await using var context = CreateContext();
        var (_, processorId) = await SeedOrderAsync(context, "Pending", stockQuantity: 3);
        var service = new OrderService(context);
        var request = new OrderStatusUpdateRequest { OrderId = 1, Status = "Cancelled" };

        var first = await service.UpdateOrderStatusAsync(processorId, request);
        var second = await service.UpdateOrderStatusAsync(processorId, request);

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal("Cancelled", first.Data!.Status);
        Assert.Equal(5, (await context.Products.FindAsync(1))!.StockQuantity);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_RelationalCancellation_RestoresStockOnce()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var (customerId, processorId) = await SeedOrderAsync(
            context,
            "Pending",
            stockQuantity: 3,
            includeBrand: true);
        var paymentAttempt = new PaymentAttempt
        {
            OrderId = 1,
            Amount = 10_000_000,
            PaymentMethod = "Fake",
            TransactionCode = "PAY-CANCEL-TEST",
            Status = PaymentAttemptStatuses.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
        context.PaymentAttempts.Add(paymentAttempt);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = new OrderService(context);
        var request = new OrderStatusUpdateRequest { OrderId = 1, Status = "Cancelled" };

        var first = await service.UpdateOrderStatusAsync(processorId, request);
        var second = await service.UpdateOrderStatusAsync(processorId, request);

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal(customerId, first.Data!.UserId);
        Assert.Equal("Cancelled", first.Data.Status);
        Assert.Equal(5, (await context.Products.AsNoTracking().SingleAsync()).StockQuantity);
        Assert.Equal("Cancelled", (await context.Orders.AsNoTracking().SingleAsync()).Status);
        var cancelledAttempt = await context.PaymentAttempts.AsNoTracking().SingleAsync();
        Assert.Equal(PaymentAttemptStatuses.Failed, cancelledAttempt.Status);
        Assert.NotNull(cancelledAttempt.CompletedAt);
        Assert.Equal("Đơn hàng đã bị hủy.", cancelledAttempt.FailureReason);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_DepositedToConfirmed_Succeeds()
    {
        await using var context = CreateContext();
        var (_, processorId) = await SeedOrderAsync(context, "Deposited", stockQuantity: 3);
        var service = new OrderService(context);

        var result = await service.UpdateOrderStatusAsync(processorId, new OrderStatusUpdateRequest
        {
            OrderId = 1,
            Status = "Confirmed"
        });

        Assert.True(result.Succeeded);
        Assert.Equal("Confirmed", result.Data!.Status);
        Assert.Equal(processorId, result.Data.ProcessedByUserId);
        Assert.Equal(3, (await context.Products.FindAsync(1))!.StockQuantity);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_CompletedOrder_IsTerminal()
    {
        await using var context = CreateContext();
        var (_, processorId) = await SeedOrderAsync(context, "Completed", stockQuantity: 3);
        var service = new OrderService(context);

        var result = await service.UpdateOrderStatusAsync(processorId, new OrderStatusUpdateRequest
        {
            OrderId = 1,
            Status = "Cancelled"
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Completed", (await context.Orders.FindAsync(1))!.Status);
        Assert.Equal(3, (await context.Products.FindAsync(1))!.StockQuantity);
    }

    [Theory]
    [InlineData("")]
    [InlineData("UnexpectedRole")]
    public async Task ReadAsync_UnprivilegedRole_CanOnlyReadOwnOrders(string role)
    {
        await using var context = CreateContext();
        var (customerId, _) = await SeedOrderAsync(context, "Pending", stockQuantity: 3);
        var otherCustomerId = Guid.NewGuid();
        context.Users.Add(new AppUser
        {
            Id = otherCustomerId,
            UserName = "other-order-customer@example.com",
            FullName = "Other Customer"
        });
        context.Orders.Add(new Order
        {
            Id = 2,
            UserId = otherCustomerId,
            Status = "Pending",
            TotalAmount = 20_000_000,
            OrderDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var service = new OrderService(context);

        var list = await service.GetOrdersAsync(customerId, role);
        var otherOrder = await service.GetOrderByIdAsync(2, customerId, role);

        Assert.Single(list);
        Assert.Equal(customerId, list[0].UserId);
        Assert.Null(otherOrder);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<(Guid CustomerId, Guid ProcessorId)> SeedOrderAsync(
        ApplicationDbContext context,
        string status,
        int stockQuantity,
        bool includeBrand = false)
    {
        var customerId = Guid.NewGuid();
        var processorId = Guid.NewGuid();
        context.Users.AddRange(
            new AppUser { Id = customerId, UserName = "customer@example.com", FullName = "Customer" },
            new AppUser { Id = processorId, UserName = "staff@example.com", FullName = "Staff" });

        if (includeBrand)
        {
            context.Brands.Add(new Brand { Id = 1, Name = "Test Brand" });
        }

        var product = new Product
        {
            Id = 1,
            Name = "Test Motorbike",
            Description = "Test motorbike description",
            Price = 50_000_000,
            StockQuantity = stockQuantity,
            Color = "Red",
            Status = "Available",
            BrandId = 1
        };
        var order = new Order
        {
            Id = 1,
            UserId = customerId,
            Status = status,
            TotalAmount = 100_000_000,
            OrderDate = DateTime.UtcNow,
            OrderItems = new List<OrderItem>
            {
                new() { Id = 1, ProductId = 1, Product = product, Quantity = 2, UnitPrice = 50_000_000 }
            }
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return (customerId, processorId);
    }

    private static async Task<Guid> SeedProductForCreationAsync(
        ApplicationDbContext context,
        int stockQuantity)
    {
        var customerId = Guid.NewGuid();
        context.Users.Add(new AppUser
        {
            Id = customerId,
            UserName = "create-customer@example.com",
            FullName = "Customer"
        });
        context.Brands.Add(new Brand { Id = 1, Name = "Test Brand" });
        context.Products.Add(new Product
        {
            Id = 1,
            Name = "Test Motorbike",
            Description = "Test motorbike description",
            Price = 50_000_000,
            StockQuantity = stockQuantity,
            Color = "Red",
            Status = "Available",
            BrandId = 1
        });
        await context.SaveChangesAsync();
        return customerId;
    }

    private static OrderCreateRequest CreateOrderRequest(params OrderItemCreateRequest[] items) => new()
    {
        ReceiverName = "Người nhận",
        ReceiverPhone = "0987654321",
        DeliveryAddress = "Hà Nội",
        ExpectedDeliveryDate = DateTime.UtcNow.Date.AddDays(1),
        Items = items.ToList()
    };
}
