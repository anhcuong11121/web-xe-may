using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Tests;

public class ArchitectureConsistencyTests
{
    [Fact]
    public void OrderItemModel_EnforcesOneProductLinePerOrder()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        var entity = context.Model.FindEntityType(typeof(OrderItem))!;
        var index = Assert.Single(entity.GetIndexes(), candidate =>
            candidate.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(OrderItem.OrderId), nameof(OrderItem.ProductId) }));

        Assert.True(index.IsUnique);
        Assert.NotNull(context.Model.FindEntityType(typeof(Order))!
            .FindNavigation(nameof(Order.OrderItems)));
    }
}
