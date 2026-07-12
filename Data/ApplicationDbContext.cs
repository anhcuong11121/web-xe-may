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
    public DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();
    public DbSet<News> News => Set<News>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ImportReceipt> ImportReceipts => Set<ImportReceipt>();
    public DbSet<ImportReceiptDetail> ImportReceiptDetails => Set<ImportReceiptDetail>();
    public DbSet<ProductSupplier> ProductSuppliers => Set<ProductSupplier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Specification>()
            .HasOne(s => s.Product)
            .WithOne(p => p.Specification)
            .HasForeignKey<Specification>(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
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
            .WithMany(o => o.Deposits)
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SupportRequest>()
            .HasOne(s => s.User)
            .WithMany(u => u.SupportRequests)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

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

        modelBuilder.Entity<ProductSupplier>()
            .HasKey(ps => new { ps.ProductId, ps.SupplierId });

        modelBuilder.Entity<ProductSupplier>()
            .HasOne(ps => ps.Product)
            .WithMany(p => p.ProductSuppliers)
            .HasForeignKey(ps => ps.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductSupplier>()
            .HasOne(ps => ps.Supplier)
            .WithMany(s => s.ProductSuppliers)
            .HasForeignKey(ps => ps.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
