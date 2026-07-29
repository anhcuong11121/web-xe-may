using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MotorBikeShop.API.Controllers;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Tests;

public class ProductImageServiceTests
{
    [Fact]
    public async Task UploadAsync_FirstImageBecomesPrimaryAndCreatesManagedFile()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new ProductImageService(fixture.Context, fixture.Environment);

        var result = await service.UploadAsync(1, 1, 1, UploadRequest(
            "airblade.png",
            isPrimary: false,
            displayOrder: 2));

        Assert.True(result.Succeeded);
        Assert.True(result.Data!.IsPrimary);
        Assert.Equal(2, result.Data.DisplayOrder);
        Assert.StartsWith("/uploads/products/skus/", result.Data.Url);
        Assert.True(File.Exists(ToPhysicalPath(fixture.WebRoot, result.Data.Url)));
    }

    [Fact]
    public async Task UploadAsync_NewPrimaryDemotesPreviousPrimary()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new ProductImageService(fixture.Context, fixture.Environment);
        var first = await service.UploadAsync(
            1,
            1,
            1,
            UploadRequest("red.jpg", isPrimary: true, displayOrder: 1));

        var second = await service.UploadAsync(
            1,
            1,
            1,
            UploadRequest("black.webp", isPrimary: true, displayOrder: 2));

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        var images = await fixture.Context.ProductImages
            .OrderBy(image => image.Id)
            .ToListAsync();
        Assert.Equal(2, images.Count);
        Assert.False(images[0].IsPrimary);
        Assert.True(images[1].IsPrimary);
        Assert.Single(images, image => image.IsPrimary);
    }

    [Fact]
    public async Task UpdateAsync_SelectingPrimaryLeavesExactlyOnePrimary()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new ProductImageService(fixture.Context, fixture.Environment);
        var first = await service.UploadAsync(
            1,
            1,
            1,
            UploadRequest("red.jpg", isPrimary: false, displayOrder: 1));
        var second = await service.UploadAsync(
            1,
            1,
            1,
            UploadRequest("black.png", isPrimary: false, displayOrder: 2));

        var result = await service.UpdateAsync(1, 1, 1, second.Data!.Id, new ProductImageUpdateRequest
        {
            AltText = " Air Blade màu đen ",
            DisplayOrder = 0,
            IsPrimary = true
        });

        Assert.True(result.Succeeded);
        Assert.Equal("Air Blade màu đen", result.Data!.AltText);
        Assert.True(result.Data.IsPrimary);
        Assert.Equal(
            second.Data.Id,
            (await fixture.Context.ProductImages.SingleAsync(image => image.IsPrimary)).Id);
        Assert.False((await fixture.Context.ProductImages.FindAsync(first.Data!.Id))!.IsPrimary);
    }

    [Fact]
    public async Task DeleteAsync_PrimaryImagePromotesNextAndDeletesManagedFile()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new ProductImageService(fixture.Context, fixture.Environment);
        var first = await service.UploadAsync(
            1,
            1,
            1,
            UploadRequest("red.jpg", isPrimary: true, displayOrder: 1));
        var second = await service.UploadAsync(
            1,
            1,
            1,
            UploadRequest("black.png", isPrimary: false, displayOrder: 2));
        var firstPath = ToPhysicalPath(fixture.WebRoot, first.Data!.Url);

        var result = await service.DeleteAsync(1, 1, 1, first.Data.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(second.Data!.Id, result.Data!.PromotedImageId);
        Assert.False(File.Exists(firstPath));
        Assert.True((await fixture.Context.ProductImages.FindAsync(second.Data.Id))!.IsPrimary);
    }

    [Theory]
    [InlineData("document.pdf")]
    [InlineData("photo.jpeg")]
    [InlineData("program.exe")]
    public async Task UploadAsync_UnsupportedExtensionCreatesNeitherRowNorFile(string fileName)
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new ProductImageService(fixture.Context, fixture.Environment);

        var result = await service.UploadAsync(
            1,
            1,
            1,
            UploadRequest(fileName, isPrimary: false, displayOrder: 0));

        Assert.False(result.Succeeded);
        Assert.Empty(await fixture.Context.ProductImages.ToListAsync());
        var uploadFolder = Path.Combine(
            fixture.WebRoot,
            "uploads",
            "products",
            "skus");
        Assert.False(Directory.Exists(uploadFolder));
    }

    [Fact]
    public async Task DeleteSkuAsync_WithImagesRequiresImagesToBeRemovedFirst()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.Context.ProductImages.Add(new ProductImage
        {
            Id = 1,
            ProductSkuId = 1,
            Url = "/assets/legacy.jpg",
            IsPrimary = true
        });
        await fixture.Context.SaveChangesAsync();
        var skuService = new ProductSkuService(fixture.Context);

        var result = await skuService.DeleteAsync(1, 1, 1);

        Assert.False(result.Succeeded);
        Assert.Contains("xóa toàn bộ ảnh", result.Error);
        Assert.NotNull(await fixture.Context.ProductSkus.FindAsync(1));
    }

    [Theory]
    [InlineData(nameof(ProductImagesController.GetManagedImages))]
    [InlineData(nameof(ProductImagesController.UploadImage))]
    [InlineData(nameof(ProductImagesController.UpdateImage))]
    [InlineData(nameof(ProductImagesController.DeleteImage))]
    public void ManagementActions_RequireEmployeeOrAdmin(string methodName)
    {
        var method = typeof(ProductImagesController).GetMethods()
            .Single(candidate => candidate.Name == methodName);
        var authorize = Assert.Single(
            method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal("Employee,Admin", authorize.Roles);
    }

    private static async Task<ImageFixture> CreateFixtureAsync()
    {
        var webRoot = Path.Combine(
            Path.GetTempPath(),
            $"motorbike-sku-image-test-{Guid.NewGuid():N}");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);
        context.Products.Add(new Product
        {
            Id = 1,
            Name = "Honda Air Blade",
            Description = "Image service test product",
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
                    SkuCode = "HONDA-AB-125-RED",
                    ColorName = "Đỏ",
                    Price = 45_500_000,
                    StockQuantity = 0,
                    Status = CatalogStatuses.Active,
                    RowVersion = BitConverter.GetBytes(1L)
                }
            }
        });
        await context.SaveChangesAsync();

        return new ImageFixture(
            context,
            new TestWebHostEnvironment
            {
                WebRootPath = webRoot,
                ContentRootPath = webRoot
            },
            webRoot);
    }

    private static ProductImageUploadRequest UploadRequest(
        string fileName,
        bool isPrimary,
        int displayOrder)
    {
        var content = new MemoryStream("test image content"u8.ToArray());
        return new ProductImageUploadRequest
        {
            File = new FormFile(content, 0, content.Length, "File", fileName),
            AltText = "Air Blade",
            DisplayOrder = displayOrder,
            IsPrimary = isPrimary
        };
    }

    private static string ToPhysicalPath(string webRoot, string url)
    {
        return Path.Combine(
            webRoot,
            url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
    }

    private sealed class ImageFixture : IAsyncDisposable
    {
        public ImageFixture(
            ApplicationDbContext context,
            TestWebHostEnvironment environment,
            string webRoot)
        {
            Context = context;
            Environment = environment;
            WebRoot = webRoot;
        }

        public ApplicationDbContext Context { get; }
        public TestWebHostEnvironment Environment { get; }
        public string WebRoot { get; }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            if (Directory.Exists(WebRoot))
            {
                Directory.Delete(WebRoot, recursive: true);
            }
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "MotorBikeShop.API.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
