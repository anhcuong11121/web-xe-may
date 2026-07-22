using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Tests;

public class PaymentConfirmationTests
{
    [Fact]
    public async Task ConfirmFakeAsync_ValidAttempt_CreatesDepositAndIsIdempotent()
    {
        await using var database = await CreateDatabaseAsync("Fake");
        var service = new PaymentService(database.Context);

        var first = await service.ConfirmFakeAsync(database.AttemptId, database.CustomerId);
        var second = await service.ConfirmFakeAsync(database.AttemptId, database.CustomerId);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(first.Data!.Deposit.Id, second.Data!.Deposit.Id);
        Assert.Equal(PaymentAttemptStatuses.Succeeded, first.Data.PaymentAttempt.Status);
        Assert.True(first.Data.PaymentAttempt.IsDemo);
        Assert.Equal("Simulated", first.Data.PaymentAttempt.ProcessingMode);
        Assert.Equal("Deposited", (await database.Context.Orders.FindAsync(1))!.Status);
        Assert.Single(database.Context.Deposits);
    }

    [Fact]
    public async Task ConfirmFakeAsync_BankTransferAttempt_IsRejectedWithoutDeposit()
    {
        await using var database = await CreateDatabaseAsync("BankTransfer");
        var service = new PaymentService(database.Context);

        var result = await service.ConfirmFakeAsync(database.AttemptId, database.CustomerId);

        Assert.False(result.Succeeded);
        Assert.Empty(database.Context.Deposits);
        Assert.Equal(PaymentAttemptStatuses.Pending,
            (await database.Context.PaymentAttempts.FindAsync(database.AttemptId))!.Status);
        Assert.Equal("Pending", (await database.Context.Orders.FindAsync(1))!.Status);
    }

    [Fact]
    public async Task CompleteManualAsync_BankTransfer_RecordsProcessorAndDeposit()
    {
        await using var database = await CreateDatabaseAsync("BankTransfer");
        var service = new PaymentService(database.Context);

        var result = await service.CompleteManualAsync(database.AttemptId, database.EmployeeId);

        Assert.True(result.Succeeded);
        Assert.Equal(database.EmployeeId, result.Data!.PaymentAttempt.ProcessedByUserId);
        Assert.Equal("Nhân viên", result.Data.PaymentAttempt.ProcessedByName);
        Assert.Equal("BankTransfer", result.Data.Deposit.PaymentMethod);
        Assert.False(result.Data.PaymentAttempt.IsDemo);
        Assert.Equal("ManualConfirmation", result.Data.PaymentAttempt.ProcessingMode);
        Assert.Equal("Deposited", (await database.Context.Orders.FindAsync(1))!.Status);
        Assert.Single(database.Context.Deposits);
    }

    private static async Task<TestDatabase> CreateDatabaseAsync(string paymentMethod)
    {
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var customerId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        context.Users.AddRange(
            new AppUser { Id = customerId, UserName = "customer@example.com", FullName = "Khách hàng" },
            new AppUser { Id = employeeId, UserName = "employee@example.com", FullName = "Nhân viên" });
        context.Orders.Add(new Order
        {
            Id = 1,
            UserId = customerId,
            Status = "Pending",
            TotalAmount = 50_000_000,
            OrderDate = DateTime.UtcNow
        });
        var attempt = new PaymentAttempt
        {
            OrderId = 1,
            Amount = 2_000_000,
            PaymentMethod = paymentMethod,
            TransactionCode = $"TEST-{Guid.NewGuid():N}",
            Status = PaymentAttemptStatuses.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        context.PaymentAttempts.Add(attempt);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return new TestDatabase(connection, context, customerId, employeeId, attempt.Id);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public TestDatabase(
            SqliteConnection connection,
            ApplicationDbContext context,
            Guid customerId,
            Guid employeeId,
            Guid attemptId)
        {
            _connection = connection;
            Context = context;
            CustomerId = customerId;
            EmployeeId = employeeId;
            AttemptId = attemptId;
        }

        public ApplicationDbContext Context { get; }
        public Guid CustomerId { get; }
        public Guid EmployeeId { get; }
        public Guid AttemptId { get; }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
