using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task UploadProductImageAsync_AllowedExtension_IsSavedWithoutSignatureValidation()
    {
        var webRoot = Path.Combine(Path.GetTempPath(), $"motorbike-upload-test-{Guid.NewGuid():N}");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var context = new ApplicationDbContext(options);
            context.Brands.Add(new Brand { Id = 1, Name = "Test Brand" });
            context.Products.Add(new Product
            {
                Id = 1,
                Name = "Test Motorbike",
                Description = "Test motorbike description",
                Price = 50_000_000,
                StockQuantity = 1,
                Color = "Red",
                Status = "Available",
                BrandId = 1
            });
            await context.SaveChangesAsync();
            var environment = new TestWebHostEnvironment { WebRootPath = webRoot };
            var service = new ProductService(context, environment);
            await using var content = new MemoryStream("This is not a JPEG image"u8.ToArray());
            var file = new FormFile(content, 0, content.Length, "file", "fake.jpg");

            var result = await service.UploadProductImageAsync(1, file);

            Assert.True(result.Succeeded);
            var imageUrl = (await context.Products.FindAsync(1))!.ImageUrl;
            Assert.EndsWith(".jpg", imageUrl);
            Assert.True(File.Exists(Path.Combine(webRoot, imageUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
        }
        finally
        {
            if (Directory.Exists(webRoot))
            {
                Directory.Delete(webRoot, recursive: true);
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
