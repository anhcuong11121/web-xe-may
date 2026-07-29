using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetCatalogProductsAsync_AggregatesOnlyActiveSkus()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        context.Products.Add(CreateCatalogProduct());
        await context.SaveChangesAsync();
        var service = new ProductService(context);

        var result = await service.GetCatalogProductsAsync(new ProductQueryParameters
        {
            MinPrice = 11_000_000,
            MaxPrice = 13_000_000
        });

        var product = Assert.Single(result.Items);
        Assert.Equal(10_000_000, product.MinimumPrice);
        Assert.Equal(12_000_000, product.MaximumPrice);
        Assert.Equal(125, product.MinimumEngineCapacityCc);
        Assert.Equal(125, product.MaximumEngineCapacityCc);
        Assert.Equal(2, product.TotalStock);
        Assert.Equal(1, product.AvailableSkuCount);
        Assert.Equal("/uploads/products/primary.jpg", product.PrimaryImageUrl);
    }

    [Fact]
    public async Task GetProductCatalogByIdAsync_ReturnsVariantSpecificationSkusAndOrderedImages()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        context.Products.Add(CreateCatalogProduct());
        await context.SaveChangesAsync();
        var service = new ProductService(context);

        var result = await service.GetProductCatalogByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(125, result.MinimumEngineCapacityCc);
        Assert.Equal(125, result.MaximumEngineCapacityCc);
        var variant = Assert.Single(result.Variants);
        Assert.Equal("125-STD", variant.VersionCode);
        Assert.Equal(125, variant.Specification!.EngineCapacityCc);
        Assert.Equal(2, variant.Skus.Count);
        Assert.Equal("SKU-RED", variant.Skus[0].SkuCode);
        Assert.True(variant.Skus[0].Images[0].IsPrimary);
        Assert.Equal("/uploads/products/primary.jpg", variant.Skus[0].Images[0].Url);
        Assert.DoesNotContain(result.Variants, item => item.VersionCode == "INACTIVE");
    }

    [Fact]
    public async Task GetProductCatalogByIdAsync_ProductWithoutSku_ReturnsEmptyCatalog()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        context.Brands.Add(new Brand { Id = 1, Name = "Honda" });
        context.Products.Add(new Product
        {
            Id = 1,
            Name = "Product without SKU",
            Description = "Legacy product",
            Status = "Available",
            BrandId = 1
        });
        await context.SaveChangesAsync();
        var service = new ProductService(context);

        var result = await service.GetProductCatalogByIdAsync(1);

        Assert.NotNull(result);
        Assert.Null(result.MinimumPrice);
        Assert.Null(result.MaximumPrice);
        Assert.Equal(0, result.TotalStock);
        Assert.Null(result.PrimaryImageUrl);
        Assert.Empty(result.Variants);
    }

    [Fact]
    public async Task DeleteProductAsync_SkuHasStock_ReturnsFailureAndKeepsProduct()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        context.Products.Add(CreateCatalogProduct());
        await context.SaveChangesAsync();
        var service = new ProductService(context);

        var result = await service.DeleteProductAsync(1);

        Assert.False(result.Succeeded);
        Assert.Contains("tồn kho", result.Error);
        Assert.NotNull(await context.Products.FindAsync(1));
    }

    [Fact]
    public async Task DeleteProductAsync_SkuHasImage_ReturnsFailureAndKeepsProduct()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        var product = CreateCatalogProduct();
        foreach (var sku in product.Variants.SelectMany(variant => variant.Skus))
        {
            sku.StockQuantity = 0;
        }
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var service = new ProductService(context);

        var result = await service.DeleteProductAsync(1);

        Assert.False(result.Succeeded);
        Assert.Contains("ảnh", result.Error);
        Assert.NotNull(await context.Products.FindAsync(1));
    }

    private static Product CreateCatalogProduct()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Honda Test",
            Description = "Catalog test product",
            Status = "Available",
            BrandId = 1,
            Brand = new Brand { Id = 1, Name = "Honda" }
        };
        product.Variants.Add(new ProductVariant
        {
            Id = 1,
            ProductId = product.Id,
            Name = "125 Tiêu chuẩn",
            VersionCode = "125-STD",
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
                    SkuCode = "SKU-RED",
                    ColorName = "Đỏ",
                    Price = 10_000_000,
                    StockQuantity = 2,
                    Status = CatalogStatuses.Active,
                    Images =
                    {
                        new ProductImage
                        {
                            Id = 1,
                            ProductSkuId = 1,
                            Url = "/uploads/products/secondary.jpg",
                            DisplayOrder = 0,
                            IsPrimary = false
                        },
                        new ProductImage
                        {
                            Id = 2,
                            ProductSkuId = 1,
                            Url = "/uploads/products/primary.jpg",
                            DisplayOrder = 1,
                            IsPrimary = true
                        }
                    }
                },
                new ProductSku
                {
                    Id = 2,
                    ProductVariantId = 1,
                    SkuCode = "SKU-BLUE",
                    ColorName = "Xanh",
                    Price = 12_000_000,
                    StockQuantity = 0,
                    Status = CatalogStatuses.Active
                },
                new ProductSku
                {
                    Id = 3,
                    ProductVariantId = 1,
                    SkuCode = "SKU-HIDDEN",
                    ColorName = "Ẩn",
                    Price = 1,
                    StockQuantity = 99,
                    Status = CatalogStatuses.Inactive
                }
            }
        });
        product.Variants.Add(new ProductVariant
        {
            Id = 2,
            ProductId = product.Id,
            Name = "Inactive variant",
            VersionCode = "INACTIVE",
            Status = CatalogStatuses.Inactive,
            Skus =
            {
                new ProductSku
                {
                    Id = 4,
                    ProductVariantId = 2,
                    SkuCode = "SKU-INACTIVE-VARIANT",
                    ColorName = "Black",
                    Price = 2,
                    StockQuantity = 99,
                    Status = CatalogStatuses.Active
                }
            }
        });

        return product;
    }
}
