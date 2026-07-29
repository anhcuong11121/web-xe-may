BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    EXEC(N'
    IF EXISTS (
        SELECT 1
        FROM OrderItems AS item
        LEFT JOIN ProductSkus AS sku ON sku.Id = item.ProductSkuId
        LEFT JOIN ProductVariants AS variant ON variant.Id = sku.ProductVariantId
        WHERE sku.Id IS NULL OR variant.ProductId <> item.ProductId
    )
        THROW 51000, ''Cleanup blocked: OrderItems contain missing or mismatched SKU links.'', 1;

    IF EXISTS (
        SELECT 1
        FROM ImportReceiptDetails AS detail
        LEFT JOIN ProductSkus AS sku ON sku.Id = detail.ProductSkuId
        LEFT JOIN ProductVariants AS variant ON variant.Id = sku.ProductVariantId
        WHERE sku.Id IS NULL OR variant.ProductId <> detail.ProductId
    )
        THROW 51000, ''Cleanup blocked: ImportReceiptDetails contain missing or mismatched SKU links.'', 1;

    IF EXISTS (
        SELECT 1
        FROM OrderItems
        WHERE NULLIF(LTRIM(RTRIM(ProductNameSnapshot)), '''') IS NULL
           OR NULLIF(LTRIM(RTRIM(VariantNameSnapshot)), '''') IS NULL
           OR NULLIF(LTRIM(RTRIM(ColorNameSnapshot)), '''') IS NULL
           OR NULLIF(LTRIM(RTRIM(SkuCodeSnapshot)), '''') IS NULL
    )
        THROW 51000, ''Cleanup blocked: OrderItem snapshots are incomplete.'', 1;

    IF EXISTS (
        SELECT 1
        FROM Products AS product
        WHERE NOT EXISTS (
            SELECT 1
            FROM ProductVariants AS variant
            INNER JOIN ProductSkus AS sku ON sku.ProductVariantId = variant.Id
            WHERE variant.ProductId = product.Id
        )
    )
        THROW 51000, ''Cleanup blocked: at least one Product has no SKU.'', 1;

    IF EXISTS (
        SELECT 1
        FROM ProductSkus
        WHERE Price < 0 OR StockQuantity < 0
    )
        THROW 51000, ''Cleanup blocked: SKU price or stock is negative.'', 1;

    IF EXISTS (
        SELECT 1
        FROM Products AS product
        OUTER APPLY (
            SELECT SUM(CAST(sku.StockQuantity AS bigint)) AS SkuStock
            FROM ProductVariants AS variant
            INNER JOIN ProductSkus AS sku ON sku.ProductVariantId = variant.Id
            WHERE variant.ProductId = product.Id
        ) AS aggregateStock
        WHERE CAST(product.StockQuantity AS bigint) <> COALESCE(aggregateStock.SkuStock, 0)
    )
        THROW 51000, ''Cleanup blocked: Product stock and aggregate SKU stock differ.'', 1;

    IF EXISTS (
        SELECT 1
        FROM Specifications AS legacySpecification
        WHERE NOT EXISTS (
            SELECT 1
            FROM ProductVariants AS variant
            INNER JOIN VariantSpecifications AS specification
                ON specification.ProductVariantId = variant.Id
            WHERE variant.ProductId = legacySpecification.ProductId
        )
    )
        THROW 51000, ''Cleanup blocked: a legacy Specification was not migrated.'', 1;

    IF EXISTS (
        SELECT 1
        FROM Products AS product
        WHERE NULLIF(LTRIM(RTRIM(product.ImageUrl)), '''') IS NOT NULL
          AND NOT EXISTS (
              SELECT 1
              FROM ProductVariants AS variant
              INNER JOIN ProductSkus AS sku ON sku.ProductVariantId = variant.Id
              INNER JOIN ProductImages AS image ON image.ProductSkuId = sku.Id
              WHERE variant.ProductId = product.Id
                AND image.Url = product.ImageUrl
          )
    )
        THROW 51000, ''Cleanup blocked: a legacy Product image was not migrated.'', 1;
    ');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    ALTER TABLE [ImportReceiptDetails] DROP CONSTRAINT [FK_ImportReceiptDetails_Products_ProductId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    ALTER TABLE [OrderItems] DROP CONSTRAINT [FK_OrderItems_Products_ProductId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    DROP TABLE [Specifications];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    DROP INDEX [IX_OrderItems_ProductId] ON [OrderItems];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    DROP INDEX [IX_ImportReceiptDetails_ProductId] ON [ImportReceiptDetails];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Color');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Products] DROP COLUMN [Color];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'ImageUrl');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Products] DROP COLUMN [ImageUrl];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Price');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Products] DROP COLUMN [Price];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'StockQuantity');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Products] DROP COLUMN [StockQuantity];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderItems]') AND [c].[name] = N'ProductId');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [OrderItems] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [OrderItems] DROP COLUMN [ProductId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ImportReceiptDetails]') AND [c].[name] = N'ProductId');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [ImportReceiptDetails] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [ImportReceiptDetails] DROP COLUMN [ProductId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728221857_CleanupLegacyProductSchema', N'10.0.9');
END;

COMMIT;
GO

