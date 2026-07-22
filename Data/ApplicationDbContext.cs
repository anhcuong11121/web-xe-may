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
    public DbSet<Specification> Specifications => Set<Specification>();
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

        modelBuilder.Entity<Specification>()
            .HasOne(s => s.Product)
            .WithOne(p => p.Specification)
            .HasForeignKey<Specification>(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Specification>().Property(s => s.CurbWeightKg).HasPrecision(8, 2);
        modelBuilder.Entity<Specification>().Property(s => s.FuelTankCapacityLiters).HasPrecision(8, 2);
        modelBuilder.Entity<Specification>().Property(s => s.FuelConsumptionLitersPer100Km).HasPrecision(8, 2);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Một đơn có nhiều dòng hàng; mỗi sản phẩm chỉ xuất hiện một lần trong cùng đơn.
        modelBuilder.Entity<OrderItem>()
            .HasIndex(item => new { item.OrderId, item.ProductId })
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
            .HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

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
            .HasKey(ird => new { ird.ImportReceiptId, ird.ProductId });

        modelBuilder.Entity<ImportReceiptDetail>()
            .HasOne(ird => ird.ImportReceipt)
            .WithMany(ir => ir.ImportReceiptDetails)
            .HasForeignKey(ird => ird.ImportReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ImportReceiptDetail>()
            .HasOne(ird => ird.Product)
            .WithMany(p => p.ImportReceiptDetails)
            .HasForeignKey(ird => ird.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
