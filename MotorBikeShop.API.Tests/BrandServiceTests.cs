using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Tests;

public class BrandServiceTests
{
    [Fact]
    public async Task CreateBrandAsync_DuplicateTrimmedName_IsRejected()
    {
        await using var context = CreateContext();
        context.Brands.Add(new Brand { Id = 1, Name = "Honda" });
        await context.SaveChangesAsync();
        var service = new BrandService(context);

        var result = await service.CreateBrandAsync(new BrandCreateRequest
        {
            Name = "  HONDA  "
        });

        Assert.False(result.Succeeded);
        Assert.Single(context.Brands);
    }

    [Fact]
    public async Task UpdateBrandAsync_WhitespaceName_IsRejectedWithoutChangingBrand()
    {
        await using var context = CreateContext();
        context.Brands.Add(new Brand { Id = 1, Name = "Yamaha", Country = "Japan" });
        await context.SaveChangesAsync();
        var service = new BrandService(context);

        var result = await service.UpdateBrandAsync(1, new BrandUpdateRequest
        {
            Name = "   ",
            Country = "Changed"
        });

        Assert.False(result.Succeeded);
        var brand = await context.Brands.FindAsync(1);
        Assert.Equal("Yamaha", brand!.Name);
        Assert.Equal("Japan", brand.Country);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}
