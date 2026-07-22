using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Tests;

public class SupplierServiceTests
{
    [Fact]
    public async Task CreateSupplierAsync_DuplicateEmailIgnoringCaseAndSpaces_IsRejected()
    {
        await using var context = CreateContext();
        context.Suppliers.Add(CreateSupplier(1, "supplier@example.com"));
        await context.SaveChangesAsync();
        var service = new SupplierService(context);

        var result = await service.CreateSupplierAsync(CreateRequest(" SUPPLIER@example.com "));

        Assert.False(result.Succeeded);
        Assert.Single(context.Suppliers);
    }

    [Fact]
    public async Task UpdateSupplierAsync_DuplicateEmail_IsRejectedWithoutChangingSupplier()
    {
        await using var context = CreateContext();
        context.Suppliers.AddRange(
            CreateSupplier(1, "first@example.com"),
            CreateSupplier(2, "second@example.com"));
        await context.SaveChangesAsync();
        var service = new SupplierService(context);

        var result = await service.UpdateSupplierAsync(2, new SupplierUpdateRequest
        {
            Name = "Changed Supplier",
            ContactPerson = "Changed Contact",
            Phone = "0911111111",
            Email = "FIRST@EXAMPLE.COM",
            Status = "Active"
        });

        Assert.False(result.Succeeded);
        var supplier = await context.Suppliers.FindAsync(2);
        Assert.Equal("Supplier 2", supplier!.Name);
        Assert.Equal("second@example.com", supplier.Email);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Supplier CreateSupplier(int id, string email) => new()
    {
        Id = id,
        Name = $"Supplier {id}",
        ContactPerson = $"Contact {id}",
        Phone = $"09876543{id:00}",
        Email = email,
        Status = "Active"
    };

    private static SupplierCreateRequest CreateRequest(string email) => new()
    {
        Name = "New Supplier",
        ContactPerson = "New Contact",
        Phone = "0988888888",
        Email = email,
        Status = "Active"
    };
}
