using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Deposit> Deposits => Set<Deposit>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();
    public DbSet<News> News => Set<News>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ImportReceipt> ImportReceipts => Set<ImportReceipt>();
    public DbSet<ImportReceiptDetail> ImportReceiptDetails => Set<ImportReceiptDetail>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();
    public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ProductInterest> ProductInterests => Set<ProductInterest>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<VariantSpecification> VariantSpecifications => Set<VariantSpecification>();
    public DbSet<ProductSku> ProductSkus => Set<ProductSku>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CustomerProfile>()
            .HasOne(p => p.User)
            .WithOne(u => u.CustomerProfile)
            .HasForeignKey<CustomerProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => new { t.UserId, t.ExpiresAt });

        modelBuilder.Entity<EmployeeProfile>()
            .HasOne(p => p.User)
            .WithOne(u => u.EmployeeProfile)
            .HasForeignKey<EmployeeProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductInterest>()
            .HasOne(interest => interest.Product)
            .WithMany(product => product.Interests)
            .HasForeignKey(interest => interest.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductInterest>()
            .HasIndex(interest => new { interest.ProductId, interest.ViewedAt });

        modelBuilder.Entity<Product>()
            .HasOne(p => p.VehicleType)
            .WithMany(v => v.Products)
            .HasForeignKey(p => p.VehicleTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductVariant>()
            .HasOne(variant => variant.Product)
            .WithMany(product => product.Variants)
            .HasForeignKey(variant => variant.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductVariant>()
            .HasIndex(variant => new { variant.ProductId, variant.VersionCode })
            .IsUnique();

        modelBuilder.Entity<ProductVariant>()
            .HasIndex(variant => new { variant.ProductId, variant.Status });

        modelBuilder.Entity<ProductVariant>()
            .Property(variant => variant.VersionCode)
            .IsUnicode(false);

        modelBuilder.Entity<ProductVariant>()
            .Property(variant => variant.Status)
            .HasDefaultValue(CatalogStatuses.Active);

        modelBuilder.Entity<ProductVariant>()
            .ToTable(table => table.HasCheckConstraint(
                "CK_ProductVariants_Status",
                "[Status] IN ('Active', 'Inactive', 'Discontinued')"));

        modelBuilder.Entity<VariantSpecification>()
            .HasOne(specification => specification.ProductVariant)
            .WithOne(variant => variant.Specification)
            .HasForeignKey<VariantSpecification>(specification => specification.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VariantSpecification>()
            .Property(specification => specification.CurbWeightKg)
            .HasPrecision(8, 2);

        modelBuilder.Entity<VariantSpecification>()
            .Property(specification => specification.FuelTankCapacityLiters)
            .HasPrecision(8, 2);

        modelBuilder.Entity<VariantSpecification>()
            .Property(specification => specification.FuelConsumptionLitersPer100Km)
            .HasPrecision(8, 2);

        modelBuilder.Entity<ProductSku>()
            .HasOne(sku => sku.ProductVariant)
            .WithMany(variant => variant.Skus)
            .HasForeignKey(sku => sku.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductSku>()
            .HasIndex(sku => sku.SkuCode)
            .IsUnique();

        modelBuilder.Entity<ProductSku>()
            .HasIndex(sku => new { sku.ProductVariantId, sku.ColorName })
            .IsUnique();

        modelBuilder.Entity<ProductSku>()
            .HasIndex(sku => new { sku.ProductVariantId, sku.Status });

        modelBuilder.Entity<ProductSku>()
            .Property(sku => sku.SkuCode)
            .IsUnicode(false);

        modelBuilder.Entity<ProductSku>()
            .Property(sku => sku.ColorHexCode)
            .IsUnicode(false);

        modelBuilder.Entity<ProductSku>()
            .Property(sku => sku.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<ProductSku>()
            .Property(sku => sku.Status)
            .HasDefaultValue(CatalogStatuses.Active);

        modelBuilder.Entity<ProductSku>()
            .Property(sku => sku.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<ProductSku>()
            .ToTable(table =>
            {
                table.HasCheckConstraint("CK_ProductSkus_Price_NonNegative", "[Price] >= 0");
                table.HasCheckConstraint("CK_ProductSkus_StockQuantity_NonNegative", "[StockQuantity] >= 0");
                table.HasCheckConstraint(
                    "CK_ProductSkus_Status",
                    "[Status] IN ('Active', 'Inactive', 'Discontinued')");
            });

        modelBuilder.Entity<ProductImage>()
            .HasOne(image => image.ProductSku)
            .WithMany(sku => sku.Images)
            .HasForeignKey(image => image.ProductSkuId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductImage>()
            .HasIndex(image => new { image.ProductSkuId, image.DisplayOrder });

        modelBuilder.Entity<ProductImage>()
            .HasIndex(image => image.ProductSkuId)
            .IsUnique()
            .HasFilter("[IsPrimary] = 1");

        modelBuilder.Entity<ProductImage>()
            .ToTable(table => table.HasCheckConstraint(
                "CK_ProductImages_DisplayOrder_NonNegative",
                "[DisplayOrder] >= 0"));

        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Một đơn có nhiều dòng hàng; mỗi SKU chỉ xuất hiện một lần trong cùng đơn.
        modelBuilder.Entity<OrderItem>()
            .HasIndex(item => new { item.OrderId, item.ProductSkuId })
            .IsUnique();

        modelBuilder.Entity<OrderItem>()
            .ToTable(table =>
            {
                table.HasCheckConstraint("CK_OrderItems_Quantity_Positive", "[Quantity] > 0");
                table.HasCheckConstraint("CK_OrderItems_UnitPrice_NonNegative", "[UnitPrice] >= 0");
            });

        modelBuilder.Entity<Order>()
            .ToTable(table => table.HasCheckConstraint("CK_Orders_TotalAmount_NonNegative", "[TotalAmount] >= 0"));

        modelBuilder.Entity<Order>()
            .HasOne(o => o.ProcessedBy)
            .WithMany()
            .HasForeignKey(o => o.ProcessedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(item => item.ProductSku)
            .WithMany(sku => sku.OrderItems)
            .HasForeignKey(item => item.ProductSkuId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Deposit>()
            .HasOne(d => d.Order)
            .WithOne(o => o.Deposit)
            .HasForeignKey<Deposit>(d => d.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Deposit>()
            .HasIndex(d => d.TransactionCode)
            .IsUnique();

        modelBuilder.Entity<PaymentAttempt>()
            .HasOne(p => p.Order)
            .WithMany(o => o.PaymentAttempts)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PaymentAttempt>()
            .HasIndex(p => p.TransactionCode)
            .IsUnique();

        modelBuilder.Entity<PaymentAttempt>()
            .HasOne(p => p.ProcessedBy)
            .WithMany(u => u.ProcessedPaymentAttempts)
            .HasForeignKey(p => p.ProcessedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PaymentAttempt>()
            .HasIndex(p => new { p.OrderId, p.Status });

        modelBuilder.Entity<PaymentAttempt>()
            .HasIndex(p => p.OrderId)
            .IsUnique()
            .HasFilter("[Status] = 'Pending'");

        modelBuilder.Entity<PaymentAttempt>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SupportRequest>()
            .HasOne(s => s.User)
            .WithMany(u => u.SupportRequests)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SupportRequest>()
            .HasOne(s => s.AssignedEmployee)
            .WithMany(u => u.AssignedSupportRequests)
            .HasForeignKey(s => s.AssignedEmployeeUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<News>()
            .HasOne(n => n.Author)
            .WithMany(u => u.NewsArticles)
            .HasForeignKey(n => n.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportReceipt>()
            .HasOne(ir => ir.Supplier)
            .WithMany(s => s.ImportReceipts)
            .HasForeignKey(ir => ir.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportReceipt>()
            .HasOne(ir => ir.CreatedBy)
            .WithMany()
            .HasForeignKey(ir => ir.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportReceiptDetail>()
            .HasKey(ird => new { ird.ImportReceiptId, ird.ProductSkuId });

        modelBuilder.Entity<ImportReceiptDetail>()
            .HasOne(ird => ird.ImportReceipt)
            .WithMany(ir => ir.ImportReceiptDetails)
            .HasForeignKey(ird => ird.ImportReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ImportReceiptDetail>()
            .HasOne(detail => detail.ProductSku)
            .WithMany(sku => sku.ImportReceiptDetails)
            .HasForeignKey(detail => detail.ProductSkuId)
            .OnDelete(DeleteBehavior.NoAction);

    }
}
