using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Tests;

public class ArchitectureConsistencyTests
{
    [Fact]
    public void OrderItemModel_EnforcesOneSkuLinePerOrder()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        var entity = context.Model.FindEntityType(typeof(OrderItem))!;
        var index = Assert.Single(entity.GetIndexes(), candidate =>
            candidate.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(OrderItem.OrderId), nameof(OrderItem.ProductSkuId) }));

        Assert.True(index.IsUnique);
        Assert.NotNull(context.Model.FindEntityType(typeof(Order))!
            .FindNavigation(nameof(Order.OrderItems)));
    }

    [Fact]
    public void ProductCatalogModel_ConfiguresVariantSkuAndImageIntegrity()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);

        var variant = context.Model.FindEntityType(typeof(ProductVariant))!;
        var sku = context.Model.FindEntityType(typeof(ProductSku))!;
        var image = context.Model.FindEntityType(typeof(ProductImage))!;

        Assert.True(variant.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
                new[] { nameof(ProductVariant.ProductId), nameof(ProductVariant.VersionCode) })).IsUnique);

        Assert.True(sku.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
                new[] { nameof(ProductSku.SkuCode) })).IsUnique);

        Assert.True(sku.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
                new[] { nameof(ProductSku.ProductVariantId), nameof(ProductSku.ColorName) })).IsUnique);

        var primaryImageIndex = image.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
                new[] { nameof(ProductImage.ProductSkuId) }) &&
            index.IsUnique);
        Assert.Equal("[IsPrimary] = 1", primaryImageIndex.GetFilter());

        Assert.True(sku.FindProperty(nameof(ProductSku.RowVersion))!.IsConcurrencyToken);
        Assert.NotNull(context.Model.FindEntityType(typeof(Product))!
            .FindNavigation(nameof(Product.Variants)));
        Assert.NotNull(variant.FindNavigation(nameof(ProductVariant.Specification)));
        Assert.NotNull(variant.FindNavigation(nameof(ProductVariant.Skus)));
        Assert.NotNull(sku.FindNavigation(nameof(ProductSku.Images)));
    }

    [Fact]
    public void CleanupModel_RemovesLegacyProductFieldsAndRequiresSkuReferences()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);

        var orderItem = context.Model.FindEntityType(typeof(OrderItem))!;
        var importDetail = context.Model.FindEntityType(typeof(ImportReceiptDetail))!;

        var product = context.Model.FindEntityType(typeof(Product))!;
        Assert.Null(product.FindProperty("Price"));
        Assert.Null(product.FindProperty("StockQuantity"));
        Assert.Null(product.FindProperty("Color"));
        Assert.Null(product.FindProperty("ImageUrl"));
        Assert.Null(context.Model.FindEntityType("MotorBikeShop.API.Models.Specification"));

        Assert.Null(orderItem.FindProperty("ProductId"));
        Assert.False(orderItem.FindProperty(nameof(OrderItem.ProductSkuId))!.IsNullable);
        Assert.Equal(
            DeleteBehavior.NoAction,
            orderItem.GetForeignKeys().Single(foreignKey =>
                foreignKey.Properties.Single().Name == nameof(OrderItem.ProductSkuId)).DeleteBehavior);

        Assert.Null(importDetail.FindProperty("ProductId"));
        Assert.False(importDetail.FindProperty(nameof(ImportReceiptDetail.ProductSkuId))!.IsNullable);
        Assert.Equal(
            DeleteBehavior.NoAction,
            importDetail.GetForeignKeys().Single(foreignKey =>
                foreignKey.Properties.Single().Name == nameof(ImportReceiptDetail.ProductSkuId)).DeleteBehavior);

        Assert.Equal(
            new[] { nameof(ImportReceiptDetail.ImportReceiptId), nameof(ImportReceiptDetail.ProductSkuId) },
            importDetail.FindPrimaryKey()!.Properties.Select(property => property.Name));

        Assert.All(
            new[]
            {
                nameof(OrderItem.ProductNameSnapshot),
                nameof(OrderItem.VariantNameSnapshot),
                nameof(OrderItem.ColorNameSnapshot),
                nameof(OrderItem.SkuCodeSnapshot)
            },
            propertyName => Assert.False(orderItem.FindProperty(propertyName)!.IsNullable));
    }
}
