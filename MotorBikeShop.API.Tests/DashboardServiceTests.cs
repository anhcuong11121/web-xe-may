using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Tests;

public class DashboardServiceTests
{
    [Fact]
    public async Task InventoryPurchaseAndInterestStatistics_ReturnStoredBusinessData()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.AddIdentityCore<AppUser>().AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<ApplicationDbContext>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        var customer = new AppUser { Id = Guid.NewGuid(), UserName = "business-stats@example.com", FullName = "Customer" };
        var product = new Product
        {
            Name = "Low Stock Bike", Description = "Test product", Price = 500m,
            StockQuantity = 3, Color = "Red", Status = "Available",
            Brand = new Brand { Name = "Test Brand" }
        };
        var order = new Order
        {
            User = customer, OrderDate = new DateTime(2026, 7, 15), Status = "Completed", TotalAmount = 1000m,
            OrderItems = { new OrderItem { Product = product, Quantity = 2, UnitPrice = 500m } }
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        context.ProductInterests.AddRange(
            new ProductInterest { ProductId = product.Id, ViewedAt = new DateTime(2026, 7, 15, 10, 0, 0) },
            new ProductInterest { ProductId = product.Id, ViewedAt = new DateTime(2026, 7, 16, 10, 0, 0) });
        await context.SaveChangesAsync();
        var service = new DashboardService(context, scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>());

        var inventory = Assert.Single(await service.GetInventoryStatisticsAsync());
        var purchase = Assert.Single(await service.GetPurchaseStatisticsAsync(new DateTime(2026, 7, 15), new DateTime(2026, 7, 15)));
        var interest = Assert.Single(await service.GetProductInterestStatisticsAsync(10, new DateTime(2026, 7, 15), new DateTime(2026, 7, 15)));

        Assert.Equal("LowStock", inventory.Status);
        Assert.Equal(2, purchase.TotalVehicles);
        Assert.Equal(1, interest.ViewCount);
        Assert.Equal(2, interest.TotalQuantitySold);
    }

    [Fact]
    public async Task Statistics_DateRange_FiltersOrdersAndRevenueInclusively()
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
        var customer = new AppUser { Id = Guid.NewGuid(), UserName = "stats@example.com", FullName = "Stats Customer" };
        context.Users.Add(customer);
        context.Orders.AddRange(
            new Order { UserId = customer.Id, OrderDate = new DateTime(2026, 7, 10, 23, 30, 0), Status = "Completed", TotalAmount = 100m },
            new Order { UserId = customer.Id, OrderDate = new DateTime(2026, 7, 11, 0, 0, 0), Status = "Completed", TotalAmount = 200m });
        await context.SaveChangesAsync();
        var service = new DashboardService(context, scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>());

        var revenue = await service.GetRevenueStatisticsAsync(new DateTime(2026, 7, 10), new DateTime(2026, 7, 10));
        var orders = await service.GetOrderStatisticsAsync(new DateTime(2026, 7, 10), new DateTime(2026, 7, 10));

        Assert.Equal(100m, Assert.Single(revenue).TotalRevenue);
        Assert.Equal(1, Assert.Single(orders).Count);
    }
}
