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
        await using var context = new SqliteTestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var customerId = await SeedProductForCreationAsync(context, stockQuantity: 5);
        context.ChangeTracker.Clear();
        var service = new OrderService(context);

        var result = await service.CreateOrderAsync(customerId, CreateOrderRequest(
            new OrderItemCreateRequest { ProductSkuId = 1, Quantity = 3 }));

        Assert.True(result.Succeeded);
        Assert.Equal(2, (await context.ProductSkus.AsNoTracking().SingleAsync()).StockQuantity);
        Assert.Single(await context.Orders.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task CreateOrderAsync_DuplicateProductLines_AreAggregated()
    {
        await using var context = CreateContext();
        var customerId = await SeedProductForCreationAsync(context, stockQuantity: 5);
        var service = new OrderService(context);
        var request = CreateOrderRequest(
            new OrderItemCreateRequest { ProductSkuId = 1, Quantity = 2 },
            new OrderItemCreateRequest { ProductSkuId = 1, Quantity = 1 });

        var result = await service.CreateOrderAsync(customerId, request);

        Assert.True(result.Succeeded);
        Assert.Single(result.Data!.Items);
        Assert.Equal(3, result.Data.Items[0].Quantity);
        Assert.Equal(150_000_000m, result.Data.TotalAmount);
        Assert.Equal(2, (await context.ProductSkus.FindAsync(1))!.StockQuantity);
        Assert.Equal("SKU-TEST-RED", result.Data.Items[0].SkuCode);
        Assert.Equal("Bản tiêu chuẩn", result.Data.Items[0].VariantName);
        Assert.Equal("Red", result.Data.Items[0].ColorName);
    }

    [Fact]
    public async Task CreateOrderAsync_DuplicateLinesExceedCombinedStock_IsRejected()
    {
        await using var context = CreateContext();
        var customerId = await SeedProductForCreationAsync(context, stockQuantity: 3);
        var service = new OrderService(context);
        var request = CreateOrderRequest(
            new OrderItemCreateRequest { ProductSkuId = 1, Quantity = 2 },
            new OrderItemCreateRequest { ProductSkuId = 1, Quantity = 2 });

        var result = await service.CreateOrderAsync(customerId, request);

        Assert.False(result.Succeeded);
        Assert.Empty(context.Orders);
        Assert.Equal(3, (await context.ProductSkus.FindAsync(1))!.StockQuantity);
    }

    [Fact]
    public async Task CreateOrderAsync_InactiveSkuIsRejectedWithoutChangingStock()
    {
        await using var context = CreateContext();
        var customerId = await SeedProductForCreationAsync(context, stockQuantity: 3);
        (await context.ProductSkus.FindAsync(1))!.Status = CatalogStatuses.Inactive;
        await context.SaveChangesAsync();
        var service = new OrderService(context);

        var result = await service.CreateOrderAsync(
            customerId,
            CreateOrderRequest(new OrderItemCreateRequest
            {
                ProductSkuId = 1,
                Quantity = 1
            }));

        Assert.False(result.Succeeded);
        Assert.Empty(context.Orders);
        Assert.Equal(3, (await context.ProductSkus.FindAsync(1))!.StockQuantity);
    }

    [Fact]
    public async Task CreateOrderAsync_ServerPriceAndSnapshotsRemainStableAfterCatalogChanges()
    {
        await using var context = CreateContext();
        var customerId = await SeedProductForCreationAsync(context, stockQuantity: 3);
        var service = new OrderService(context);
        var created = await service.CreateOrderAsync(
            customerId,
            CreateOrderRequest(new OrderItemCreateRequest
            {
                ProductSkuId = 1,
                Quantity = 1
            }));

        var sku = (await context.ProductSkus.FindAsync(1))!;
        sku.Price = 99_000_000;
        sku.ColorName = "Black";
        sku.ProductVariant.Name = "Bản đổi tên";
        sku.ProductVariant.Product.Name = "Sản phẩm đổi tên";
        await context.SaveChangesAsync();
        var readBack = await service.GetOrderByIdAsync(
            created.Data!.Id,
            customerId,
            "Customer");

        Assert.NotNull(readBack);
        var item = Assert.Single(readBack.Items);
        Assert.Equal(50_000_000, item.UnitPrice);
        Assert.Equal("Test Motorbike", item.ProductName);
        Assert.Equal("Bản tiêu chuẩn", item.VariantName);
        Assert.Equal("Red", item.ColorName);
        Assert.Equal("SKU-TEST-RED", item.SkuCode);
    }

    [Fact]
    public async Task CreateOrderAsync_SaveFailureRollsBackSkuStock()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new SqliteTestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedProductForCreationAsync(context, stockQuantity: 2);
        context.ChangeTracker.Clear();
        var service = new OrderService(context);

        var result = await service.CreateOrderAsync(
            Guid.NewGuid(),
            CreateOrderRequest(new OrderItemCreateRequest
            {
                ProductSkuId = 1,
                Quantity = 1
            }));

        Assert.False(result.Succeeded);
        Assert.Empty(await context.Orders.AsNoTracking().ToListAsync());
        Assert.Equal(2, (await context.ProductSkus.AsNoTracking().SingleAsync()).StockQuantity);
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
        Assert.Equal(3, (await context.ProductSkus.FindAsync(1))!.StockQuantity);
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
        Assert.Equal(5, (await context.ProductSkus.FindAsync(1))!.StockQuantity);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_RelationalCancellation_RestoresStockOnce()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new SqliteTestDbContext(options);
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
        Assert.Equal(5, (await context.ProductSkus.AsNoTracking().SingleAsync()).StockQuantity);
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
        Assert.Equal(3, (await context.ProductSkus.FindAsync(1))!.StockQuantity);
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
        Assert.Equal(3, (await context.ProductSkus.FindAsync(1))!.StockQuantity);
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
            Status = "Available",
            BrandId = 1
        };
        var variant = AddSkuCatalog(product, stockQuantity);
        var order = new Order
        {
            Id = 1,
            UserId = customerId,
            Status = status,
            TotalAmount = 100_000_000,
            OrderDate = DateTime.UtcNow,
            OrderItems = new List<OrderItem>
            {
                new()
                {
                    Id = 1,
                    ProductSkuId = 1,
                    ProductSku = variant.Skus.Single(),
                    ProductNameSnapshot = product.Name,
                    VariantNameSnapshot = variant.Name,
                    ColorNameSnapshot = variant.Skus.Single().ColorName,
                    SkuCodeSnapshot = variant.Skus.Single().SkuCode,
                    Quantity = 2,
                    UnitPrice = 50_000_000
                }
            }
        };
        context.Products.Add(product);
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
        var product = new Product
        {
            Id = 1,
            Name = "Test Motorbike",
            Description = "Test motorbike description",
            Status = "Available",
            BrandId = 1
        };
        AddSkuCatalog(product, stockQuantity);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return customerId;
    }

    private static ProductVariant AddSkuCatalog(Product product, int stockQuantity)
    {
        var variant = new ProductVariant
        {
            Id = 1,
            ProductId = product.Id,
            Name = "Bản tiêu chuẩn",
            VersionCode = "STANDARD",
            Status = CatalogStatuses.Active,
            Skus =
            {
                new ProductSku
                {
                    Id = 1,
                    ProductVariantId = 1,
                    SkuCode = "SKU-TEST-RED",
                    ColorName = "Red",
                    Price = 50_000_000,
                    StockQuantity = stockQuantity,
                    Status = CatalogStatuses.Active,
                    RowVersion = BitConverter.GetBytes(1L)
                }
            }
        };
        product.Variants.Add(variant);
        return variant;
    }

    private static OrderCreateRequest CreateOrderRequest(params OrderItemCreateRequest[] items) => new()
    {
        ReceiverName = "Người nhận",
        ReceiverPhone = "0987654321",
        DeliveryAddress = "Hà Nội",
        ExpectedDeliveryDate = DateTime.UtcNow.Date.AddDays(1),
        Items = items.ToList()
    };

    private sealed class SqliteTestDbContext : ApplicationDbContext
    {
        public SqliteTestDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ProductSku>()
                .Property(sku => sku.RowVersion)
                .ValueGeneratedNever();
        }
    }
}
