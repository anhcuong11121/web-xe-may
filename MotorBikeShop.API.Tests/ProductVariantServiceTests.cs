using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Controllers;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Tests;

public class ProductVariantServiceTests
{
    [Fact]
    public async Task CreateAsync_NormalizesVersionCodeStatusAndText()
    {
        await using var context = CreateContext();
        await SeedProductAsync(context);
        var service = new ProductVariantService(context);

        var result = await service.CreateAsync(1, CreateRequest(" 125-std ", " active "));

        Assert.True(result.Succeeded);
        Assert.Equal("125-STD", result.Data!.VersionCode);
        Assert.Equal(CatalogStatuses.Active, result.Data.Status);
        Assert.Equal("125 Tiêu chuẩn", result.Data.Name);
        Assert.Equal("4 kỳ", result.Data.Specification!.EngineType);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNormalizedVersionCode_IsRejected()
    {
        await using var context = CreateContext();
        await SeedProductAsync(context);
        var service = new ProductVariantService(context);
        Assert.True((await service.CreateAsync(1, CreateRequest("125-STD"))).Succeeded);

        var duplicate = await service.CreateAsync(1, CreateRequest(" 125-std "));

        Assert.False(duplicate.Succeeded);
        Assert.Contains("đã tồn tại", duplicate.Error);
        Assert.Equal(1, await context.ProductVariants.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_KeepsVersionCodeImmutableAndUpdatesSpecification()
    {
        await using var context = CreateContext();
        await SeedProductAsync(context);
        var service = new ProductVariantService(context);
        var created = await service.CreateAsync(1, CreateRequest("125-STD"));

        var result = await service.UpdateAsync(1, created.Data!.Id, new ProductVariantUpdateRequest
        {
            Name = "125 Cao cấp",
            Status = "inactive",
            Specification = ValidSpecification(160)
        });

        Assert.True(result.Succeeded);
        Assert.Equal("125-STD", result.Data!.VersionCode);
        Assert.Equal("125 Cao cấp", result.Data.Name);
        Assert.Equal(CatalogStatuses.Inactive, result.Data.Status);
        Assert.Equal(160, result.Data.Specification!.EngineCapacityCc);
    }

    [Fact]
    public async Task DeleteAsync_ReferencedVariant_IsDeactivatedWithItsSkus()
    {
        await using var context = CreateContext();
        await SeedProductWithVariantAsync(context, stockQuantity: 2);
        context.OrderItems.Add(new OrderItem
        {
            Id = 1,
            OrderId = 1,
            ProductSkuId = 1,
            Quantity = 1,
            UnitPrice = 10_000_000
        });
        await context.SaveChangesAsync();
        var service = new ProductVariantService(context);

        var result = await service.DeleteAsync(1, 1);

        Assert.True(result.Succeeded);
        Assert.Equal("Deactivated", result.Data!.Action);
        Assert.Equal(
            CatalogStatuses.Inactive,
            (await context.ProductVariants.FindAsync(1))!.Status);
        Assert.Equal(
            CatalogStatuses.Inactive,
            (await context.ProductSkus.FindAsync(1))!.Status);
    }

    [Fact]
    public async Task DeleteAsync_UnreferencedVariantWithStock_IsRejected()
    {
        await using var context = CreateContext();
        await SeedProductWithVariantAsync(context, stockQuantity: 1);
        var service = new ProductVariantService(context);

        var result = await service.DeleteAsync(1, 1);

        Assert.False(result.Succeeded);
        Assert.Contains("còn tồn kho", result.Error);
        Assert.NotNull(await context.ProductVariants.FindAsync(1));
    }

    [Fact]
    public async Task DeleteAsync_UnreferencedVariantWithoutStock_IsHardDeleted()
    {
        await using var context = CreateContext();
        await SeedProductWithVariantAsync(context, stockQuantity: 0);
        var service = new ProductVariantService(context);

        var result = await service.DeleteAsync(1, 1);

        Assert.True(result.Succeeded);
        Assert.Equal("Deleted", result.Data!.Action);
        Assert.Null(await context.ProductVariants.FindAsync(1));
        Assert.Null(await context.ProductSkus.FindAsync(1));
    }

    [Theory]
    [InlineData(nameof(ProductVariantsController.CreateVariant))]
    [InlineData(nameof(ProductVariantsController.UpdateVariant))]
    [InlineData(nameof(ProductVariantsController.UpdateSpecification))]
    [InlineData(nameof(ProductVariantsController.DeleteVariant))]
    [InlineData(nameof(ProductVariantsController.GetManagedVariants))]
    public void ManagementActions_RequireEmployeeOrAdmin(string methodName)
    {
        var method = typeof(ProductVariantsController).GetMethods()
            .Single(candidate => candidate.Name == methodName);
        var authorize = Assert.Single(
            method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal("Employee,Admin", authorize.Roles);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SeedProductAsync(ApplicationDbContext context)
    {
        context.Products.Add(new Product
        {
            Id = 1,
            Name = "Honda Test",
            Description = "Product for variant tests",
            Status = "Available",
            BrandId = 1
        });
        await context.SaveChangesAsync();
    }

    private static async Task SeedProductWithVariantAsync(
        ApplicationDbContext context,
        int stockQuantity)
    {
        await SeedProductAsync(context);
        context.ProductVariants.Add(new ProductVariant
        {
            Id = 1,
            ProductId = 1,
            Name = "125 Tiêu chuẩn",
            VersionCode = "LEGACY-V1",
            Status = CatalogStatuses.Active,
            Specification = new VariantSpecification
            {
                ProductVariantId = 1,
                EngineType = "4 kỳ",
                FuelType = "Xăng",
                EngineCapacityCc = 125,
                HorsePower = 10
            },
            Skus =
            {
                new ProductSku
                {
                    Id = 1,
                    ProductVariantId = 1,
                    SkuCode = "LEGACY-P0000000001-S01",
                    ColorName = "Đỏ",
                    Price = 10_000_000,
                    StockQuantity = stockQuantity,
                    Status = CatalogStatuses.Active
                }
            }
        });
        await context.SaveChangesAsync();
    }

    private static ProductVariantCreateRequest CreateRequest(
        string versionCode,
        string status = CatalogStatuses.Active)
    {
        return new ProductVariantCreateRequest
        {
            Name = " 125 Tiêu chuẩn ",
            VersionCode = versionCode,
            Status = status,
            Specification = ValidSpecification(125)
        };
    }

    private static VariantSpecificationRequest ValidSpecification(int engineCapacityCc)
    {
        return new VariantSpecificationRequest
        {
            EngineType = " 4 kỳ ",
            FuelType = " Xăng ",
            EngineCapacityCc = engineCapacityCc,
            HorsePower = 10
        };
    }
}
