using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Tests;

public class ImportReceiptServiceTests
{
    [Fact]
    public async Task CancelAsync_CompletedReceipt_ReversesStockAndKeepsReceiptHistory()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new SqliteTestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var employeeId = await SeedCatalogAsync(context);
        context.ChangeTracker.Clear();
        var service = new ImportReceiptService(context);
        var created = await service.CreateAsync(employeeId, CreateRequest(quantity: 2));

        var cancelled = await service.CancelAsync(created.Data!.Id);

        Assert.True(cancelled.Succeeded);
        Assert.Equal("Cancelled", cancelled.Data!.Status);
        Assert.Equal(3, (await context.ProductSkus.AsNoTracking().SingleAsync()).StockQuantity);
        Assert.Single(await context.ImportReceipts.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task CancelAsync_InsufficientStock_DoesNotCancelOrPartiallyChangeData()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new SqliteTestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var employeeId = await SeedCatalogAsync(context);
        context.ChangeTracker.Clear();
        var service = new ImportReceiptService(context);
        var created = await service.CreateAsync(employeeId, CreateRequest(quantity: 2));
        await context.ProductSkus.ExecuteUpdateAsync(setters =>
            setters.SetProperty(sku => sku.StockQuantity, 1));

        var result = await service.CancelAsync(created.Data!.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(1, (await context.ProductSkus.AsNoTracking().SingleAsync()).StockQuantity);
        Assert.Equal("Completed", (await context.ImportReceipts.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task CreateAsync_DuplicateExplicitReceiptNumber_IsRejectedWithoutChangingStockAgain()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        var employeeId = await SeedCatalogAsync(context);
        var service = new ImportReceiptService(context);
        var request = CreateRequest(quantity: 2, receiptNumber: "PN-DUPLICATE");

        var first = await service.CreateAsync(employeeId, request);
        var second = await service.CreateAsync(employeeId, request);

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Single(context.ImportReceipts);
        Assert.Equal(5, (await context.ProductSkus.FindAsync(1))!.StockQuantity);
    }

    [Fact]
    public async Task CreateAsync_TwoAutomaticReceiptNumbers_AreDistinct()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        var employeeId = await SeedCatalogAsync(context);
        var service = new ImportReceiptService(context);

        var first = await service.CreateAsync(employeeId, CreateRequest(quantity: 1, receiptNumber: null));
        var second = await service.CreateAsync(employeeId, CreateRequest(quantity: 1, receiptNumber: null));

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.NotEqual(first.Data!.ReceiptNumber, second.Data!.ReceiptNumber);
        Assert.StartsWith("PN", first.Data.ReceiptNumber);
        Assert.StartsWith("PN", second.Data.ReceiptNumber);
    }

    [Fact]
    public async Task CreateAsync_RelationalDatabase_AtomicallyIncrementsStock()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new SqliteTestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var employeeId = await SeedCatalogAsync(context);
        context.ChangeTracker.Clear();
        var service = new ImportReceiptService(context);

        var result = await service.CreateAsync(employeeId, CreateRequest(quantity: 2));

        Assert.True(result.Succeeded);
        Assert.Equal(5, (await context.ProductSkus.AsNoTracking().SingleAsync()).StockQuantity);
        Assert.Single(await context.ImportReceipts.AsNoTracking().ToListAsync());
        Assert.Equal(20_000_000m, result.Data!.TotalAmount);
        Assert.Equal("SKU-IMPORT-RED", result.Data.Details.Single().SkuCode);
    }

    [Fact]
    public async Task CreateAsync_OnlyIncrementsSelectedSku()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        var employeeId = await SeedCatalogAsync(context);
        var variant = await context.ProductVariants
            .Include(item => item.Product)
            .SingleAsync();
        context.ProductSkus.Add(new ProductSku
        {
            Id = 2,
            ProductVariantId = variant.Id,
            SkuCode = "SKU-IMPORT-BLACK",
            ColorName = "Black",
            Price = 51_000_000,
            StockQuantity = 3,
            Status = CatalogStatuses.Active,
            RowVersion = BitConverter.GetBytes(2L)
        });
        await context.SaveChangesAsync();
        var service = new ImportReceiptService(context);

        var result = await service.CreateAsync(employeeId, CreateRequest(quantity: 2));

        Assert.True(result.Succeeded);
        Assert.Equal(5, (await context.ProductSkus.FindAsync(1))!.StockQuantity);
        Assert.Equal(3, (await context.ProductSkus.FindAsync(2))!.StockQuantity);
    }

    [Fact]
    public async Task CreateAsync_InactiveSkuIsRejectedWithoutChangingStock()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        var employeeId = await SeedCatalogAsync(context);
        (await context.ProductSkus.FindAsync(1))!.Status = CatalogStatuses.Inactive;
        await context.SaveChangesAsync();
        var service = new ImportReceiptService(context);

        var result = await service.CreateAsync(employeeId, CreateRequest(quantity: 2));

        Assert.False(result.Succeeded);
        Assert.Empty(context.ImportReceipts);
        Assert.Equal(3, (await context.ProductSkus.FindAsync(1))!.StockQuantity);
    }

    [Fact]
    public async Task CreateAsync_SaveFailureRollsBackSkuStock()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new SqliteTestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedCatalogAsync(context);
        context.ChangeTracker.Clear();
        var service = new ImportReceiptService(context);

        var result = await service.CreateAsync(Guid.NewGuid(), CreateRequest(quantity: 2));

        Assert.False(result.Succeeded);
        Assert.Empty(await context.ImportReceipts.AsNoTracking().ToListAsync());
        Assert.Equal(3, (await context.ProductSkus.AsNoTracking().SingleAsync()).StockQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_NonPositiveQuantity_FailsWithoutChangingStock(int quantity)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        var employeeId = await SeedCatalogAsync(context);
        var service = new ImportReceiptService(context);

        var result = await service.CreateAsync(employeeId, CreateRequest(quantity));

        Assert.False(result.Succeeded);
        Assert.Empty(context.ImportReceipts);
        Assert.Equal(3, (await context.ProductSkus.FindAsync(1))!.StockQuantity);
    }

    private static ImportReceiptCreateRequest CreateRequest(
        int quantity,
        string? receiptNumber = "PN-TEST") => new()
    {
        ReceiptNumber = receiptNumber == null ? null : $"{receiptNumber}-{Guid.NewGuid():N}",
        SupplierId = 1,
        Details = new List<ImportReceiptDetailCreateRequest>
        {
            new() { ProductSkuId = 1, Quantity = quantity, UnitCost = 10_000_000 }
        }
    };

    private static async Task<Guid> SeedCatalogAsync(ApplicationDbContext context)
    {
        var employeeId = Guid.NewGuid();
        context.Users.Add(new AppUser
        {
            Id = employeeId,
            UserName = "import-employee@example.com",
            FullName = "Import Employee"
        });
        context.Suppliers.Add(new Supplier
        {
            Id = 1,
            Name = "Test Supplier",
            ContactPerson = "Contact",
            Phone = "0987654321",
            Email = "supplier@example.com",
            Status = "Active"
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
        product.Variants.Add(new ProductVariant
        {
            Id = 1,
            ProductId = 1,
            Name = "Bản tiêu chuẩn",
            VersionCode = "STANDARD",
            Status = CatalogStatuses.Active,
            Skus =
            {
                new ProductSku
                {
                    Id = 1,
                    ProductVariantId = 1,
                    SkuCode = "SKU-IMPORT-RED",
                    ColorName = "Red",
                    Price = 50_000_000,
                    StockQuantity = 3,
                    Status = CatalogStatuses.Active,
                    RowVersion = BitConverter.GetBytes(1L)
                }
            }
        });
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return employeeId;
    }

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
