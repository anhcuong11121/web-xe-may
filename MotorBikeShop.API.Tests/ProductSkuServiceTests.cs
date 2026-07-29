using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Controllers;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Tests;

public class ProductSkuServiceTests
{
    [Fact]
    public async Task CreateAsync_NormalizesValuesAndStartsWithZeroStock()
    {
        await using var context = CreateContext();
        await SeedVariantAsync(context);
        var service = new ProductSkuService(context);

        var result = await service.CreateAsync(1, 1, new ProductSkuCreateRequest
        {
            SkuCode = " honda-ab-125-red ",
            ColorName = " Đỏ đen ",
            ColorHexCode = " #ff0011 ",
            Price = 45_500_000,
            Status = " active "
        });

        Assert.True(result.Succeeded);
        Assert.Equal("HONDA-AB-125-RED", result.Data!.SkuCode);
        Assert.Equal("Đỏ đen", result.Data.ColorName);
        Assert.Equal("#FF0011", result.Data.ColorHexCode);
        Assert.Equal(CatalogStatuses.Active, result.Data.Status);
        Assert.Equal(0, result.Data.StockQuantity);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSkuCodeAcrossVariants_IsRejected()
    {
        await using var context = CreateContext();
        await SeedVariantAsync(context);
        context.ProductVariants.Add(new ProductVariant
        {
            Id = 2,
            ProductId = 1,
            Name = "160 Cao cấp",
            VersionCode = "160-PREMIUM",
            Status = CatalogStatuses.Active,
            Specification = ValidSpecification(160, productVariantId: 2)
        });
        context.ProductSkus.Add(CreateSku(1, 1, "HONDA-AB-125-RED", "Đỏ"));
        await context.SaveChangesAsync();
        var service = new ProductSkuService(context);

        var result = await service.CreateAsync(1, 2, CreateRequest(
            " honda-ab-125-red ",
            "Đen"));

        Assert.False(result.Succeeded);
        Assert.Contains("đã tồn tại", result.Error);
        Assert.Equal(1, await context.ProductSkus.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_DuplicateColorWithinVariant_IsRejectedCaseInsensitively()
    {
        await using var context = CreateContext();
        await SeedVariantAsync(context);
        context.ProductSkus.Add(CreateSku(1, 1, "HONDA-AB-125-RED", "Đỏ đen"));
        await context.SaveChangesAsync();
        var service = new ProductSkuService(context);

        var result = await service.CreateAsync(1, 1, CreateRequest(
            "HONDA-AB-125-RED-2",
            "  ĐỎ ĐEN  "));

        Assert.False(result.Succeeded);
        Assert.Contains("Màu đã tồn tại", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_KeepsSkuCodeAndStockImmutable()
    {
        await using var context = CreateContext();
        await SeedVariantAsync(context);
        context.ProductSkus.Add(CreateSku(
            1,
            1,
            "HONDA-AB-125-RED",
            "Đỏ",
            stockQuantity: 7));
        await context.SaveChangesAsync();
        var service = new ProductSkuService(context);

        var result = await service.UpdateAsync(1, 1, 1, new ProductSkuUpdateRequest
        {
            ColorName = "Đen",
            ColorHexCode = "#000000",
            Price = 46_000_000,
            Status = CatalogStatuses.Inactive,
            RowVersion = Convert.ToBase64String((await context.ProductSkus.FindAsync(1))!.RowVersion)
        });

        Assert.True(result.Succeeded);
        Assert.Equal("HONDA-AB-125-RED", result.Data!.SkuCode);
        Assert.Equal(7, result.Data.StockQuantity);
        Assert.Equal("Đen", result.Data.ColorName);
        Assert.Equal(46_000_000, result.Data.Price);
        Assert.Equal(CatalogStatuses.Inactive, result.Data.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64")]
    [InlineData("AQID")]
    public async Task UpdateAsync_InvalidRowVersion_IsRejected(string rowVersion)
    {
        await using var context = CreateContext();
        await SeedVariantAsync(context);
        context.ProductSkus.Add(CreateSku(1, 1, "HONDA-AB-125-RED", "Đỏ"));
        await context.SaveChangesAsync();
        var service = new ProductSkuService(context);

        var result = await service.UpdateAsync(1, 1, 1, new ProductSkuUpdateRequest
        {
            ColorName = "Đen",
            Price = 46_000_000,
            Status = CatalogStatuses.Active,
            RowVersion = rowVersion
        });

        Assert.False(result.Succeeded);
        Assert.Contains("RowVersion không hợp lệ", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_StaleRowVersion_ReturnsConcurrencyFailure()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var seedContext = CreateContext(databaseName))
        {
            await SeedVariantAsync(seedContext);
            seedContext.ProductSkus.Add(CreateSku(1, 1, "HONDA-AB-125-RED", "Đỏ"));
            await seedContext.SaveChangesAsync();
        }

        await using (var competingContext = CreateContext(databaseName))
        {
            var sku = (await competingContext.ProductSkus.FindAsync(1))!;
            sku.RowVersion = BitConverter.GetBytes(2L);
            await competingContext.SaveChangesAsync();
        }

        await using var context = CreateContext(databaseName);
        var service = new ProductSkuService(context);
        var result = await service.UpdateAsync(1, 1, 1, new ProductSkuUpdateRequest
        {
            ColorName = "Đen",
            Price = 46_000_000,
            Status = CatalogStatuses.Active,
            RowVersion = Convert.ToBase64String(BitConverter.GetBytes(1L))
        });

        Assert.False(result.Succeeded);
        Assert.Contains("được cập nhật bởi yêu cầu khác", result.Error);
    }

    [Fact]
    public async Task DeleteAsync_ReferencedSku_IsDeactivated()
    {
        await using var context = CreateContext();
        await SeedVariantAsync(context);
        context.ProductSkus.Add(CreateSku(1, 1, "HONDA-AB-125-RED", "Đỏ"));
        context.OrderItems.Add(new OrderItem
        {
            Id = 1,
            OrderId = 1,
            ProductSkuId = 1,
            Quantity = 1,
            UnitPrice = 45_500_000
        });
        await context.SaveChangesAsync();
        var service = new ProductSkuService(context);

        var result = await service.DeleteAsync(1, 1, 1);

        Assert.True(result.Succeeded);
        Assert.Equal("Deactivated", result.Data!.Action);
        Assert.Equal(
            CatalogStatuses.Inactive,
            (await context.ProductSkus.FindAsync(1))!.Status);
    }

    [Fact]
    public async Task DeleteAsync_UnreferencedSkuWithStock_IsRejected()
    {
        await using var context = CreateContext();
        await SeedVariantAsync(context);
        context.ProductSkus.Add(CreateSku(
            1,
            1,
            "HONDA-AB-125-RED",
            "Đỏ",
            stockQuantity: 1));
        await context.SaveChangesAsync();
        var service = new ProductSkuService(context);

        var result = await service.DeleteAsync(1, 1, 1);

        Assert.False(result.Succeeded);
        Assert.Contains("còn tồn kho", result.Error);
        Assert.NotNull(await context.ProductSkus.FindAsync(1));
    }

    [Fact]
    public async Task DeleteAsync_UnreferencedSkuWithoutStock_IsHardDeleted()
    {
        await using var context = CreateContext();
        await SeedVariantAsync(context);
        context.ProductSkus.Add(CreateSku(1, 1, "HONDA-AB-125-RED", "Đỏ"));
        await context.SaveChangesAsync();
        var service = new ProductSkuService(context);

        var result = await service.DeleteAsync(1, 1, 1);

        Assert.True(result.Succeeded);
        Assert.Equal("Deleted", result.Data!.Action);
        Assert.Null(await context.ProductSkus.FindAsync(1));
    }

    [Theory]
    [InlineData(nameof(ProductSkusController.GetManagedSkus))]
    [InlineData(nameof(ProductSkusController.CreateSku))]
    [InlineData(nameof(ProductSkusController.UpdateSku))]
    [InlineData(nameof(ProductSkusController.DeleteSku))]
    public void ManagementActions_RequireEmployeeOrAdmin(string methodName)
    {
        var method = typeof(ProductSkusController).GetMethods()
            .Single(candidate => candidate.Name == methodName);
        var authorize = Assert.Single(
            method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal("Employee,Admin", authorize.Roles);
    }

    private static ApplicationDbContext CreateContext()
    {
        return CreateContext(Guid.NewGuid().ToString());
    }

    private static ApplicationDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SeedVariantAsync(ApplicationDbContext context)
    {
        context.Products.Add(new Product
        {
            Id = 1,
            Name = "Honda Air Blade",
            Description = "Product for SKU tests",
            Status = "Available",
            BrandId = 1
        });
        context.ProductVariants.Add(new ProductVariant
        {
            Id = 1,
            ProductId = 1,
            Name = "125 Tiêu chuẩn",
            VersionCode = "125-STD",
            Status = CatalogStatuses.Active,
            Specification = ValidSpecification(125)
        });
        await context.SaveChangesAsync();
    }

    private static VariantSpecification ValidSpecification(
        int capacity,
        int productVariantId = 1)
    {
        return new VariantSpecification
        {
            ProductVariantId = productVariantId,
            EngineType = "4 kỳ",
            FuelType = "Xăng",
            EngineCapacityCc = capacity,
            HorsePower = 10
        };
    }

    private static ProductSku CreateSku(
        int id,
        int variantId,
        string code,
        string color,
        int stockQuantity = 0)
    {
        return new ProductSku
        {
            Id = id,
            ProductVariantId = variantId,
            SkuCode = code,
            ColorName = color,
            Price = 45_500_000,
            StockQuantity = stockQuantity,
            Status = CatalogStatuses.Active,
            RowVersion = BitConverter.GetBytes((long)id)
        };
    }

    private static ProductSkuCreateRequest CreateRequest(string code, string color)
    {
        return new ProductSkuCreateRequest
        {
            SkuCode = code,
            ColorName = color,
            Price = 45_500_000,
            Status = CatalogStatuses.Active
        };
    }
}
