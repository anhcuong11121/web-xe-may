BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728200927_AddProductVariantCatalog'
)
BEGIN
    CREATE TABLE [ProductVariants] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [VersionCode] varchar(64) NOT NULL,
        [Status] nvarchar(32) NOT NULL DEFAULT N'Active',
        CONSTRAINT [PK_ProductVariants] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProductVariants_Status] CHECK ([Status] IN ('Active', 'Inactive', 'Discontinued')),
        CONSTRAINT [FK_ProductVariants_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728200927_AddProductVariantCatalog'
)
BEGIN
    CREATE TABLE [ProductSkus] (
        [Id] int NOT NULL IDENTITY,
        [ProductVariantId] int NOT NULL,
        [SkuCode] varchar(64) NOT NULL,
        [ColorName] nvarchar(100) NOT NULL,
        [ColorHexCode] varchar(9) NULL,
        [Price] decimal(18,2) NOT NULL,
        [StockQuantity] int NOT NULL,
        [Status] nvarchar(32) NOT NULL DEFAULT N'Active',
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ProductSkus] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProductSkus_Price_NonNegative] CHECK ([Price] >= 0),
        CONSTRAINT [CK_ProductSkus_Status] CHECK ([Status] IN ('Active', 'Inactive', 'Discontinued')),
        CONSTRAINT [CK_ProductSkus_StockQuantity_NonNegative] CHECK ([StockQuantity] >= 0),
        CONSTRAINT [FK_ProductSkus_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728200927_AddProductVariantCatalog'
)
BEGIN
    CREATE TABLE [VariantSpecifications] (
        [ProductVariantId] int NOT NULL,
        [EngineType] nvarchar(100) NOT NULL,
        [FuelType] nvarchar(50) NOT NULL,
        [EngineCapacityCc] int NOT NULL,
        [HorsePower] int NOT NULL,
        [CurbWeightKg] decimal(8,2) NULL,
        [Dimensions] nvarchar(100) NULL,
        [FuelTankCapacityLiters] decimal(8,2) NULL,
        [MaxPower] nvarchar(100) NULL,
        [FuelConsumptionLitersPer100Km] decimal(8,2) NULL,
        [OtherDetails] nvarchar(2000) NULL,
        CONSTRAINT [PK_VariantSpecifications] PRIMARY KEY ([ProductVariantId]),
        CONSTRAINT [FK_VariantSpecifications_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728200927_AddProductVariantCatalog'
)
BEGIN
    CREATE TABLE [ProductImages] (
        [Id] int NOT NULL IDENTITY,
        [ProductSkuId] int NOT NULL,
        [Url] nvarchar(500) NOT NULL,
        [AltText] nvarchar(200) NULL,
        [DisplayOrder] int NOT NULL,
        [IsPrimary] bit NOT NULL,
        CONSTRAINT [PK_ProductImages] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProductImages_DisplayOrder_NonNegative] CHECK ([DisplayOrder] >= 0),
        CONSTRAINT [FK_ProductImages_ProductSkus_ProductSkuId] FOREIGN KEY ([ProductSkuId]) REFERENCES [ProductSkus] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728200927_AddProductVariantCatalog'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ProductImages_ProductSkuId] ON [ProductImages] ([ProductSkuId]) WHERE [IsPrimary] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728200927_AddProductVariantCatalog'
)
BEGIN
    CREATE INDEX [IX_ProductImages_ProductSkuId_DisplayOrder] ON [ProductImages] ([ProductSkuId], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728200927_AddProductVariantCatalog'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductSkus_ProductVariantId_ColorName] ON [ProductSkus] ([ProductVariantId], [ColorName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728200927_AddProductVariantCatalog'
)
BEGIN
    CREATE INDEX [IX_ProductSkus_ProductVariantId_Status] ON [ProductSkus] ([ProductVariantId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728200927_AddProductVariantCatalog'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductSkus_SkuCode] ON [ProductSkus] ([SkuCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728200927_AddProductVariantCatalog'
)
BEGIN
    CREATE INDEX [IX_ProductVariants_ProductId_Status] ON [ProductVariants] ([ProductId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728200927_AddProductVariantCatalog'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductVariants_ProductId_VersionCode] ON [ProductVariants] ([ProductId], [VersionCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728200927_AddProductVariantCatalog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728200927_AddProductVariantCatalog', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728201821_BackfillLegacyProductCatalog'
)
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM Products AS product
        INNER JOIN ProductSkus AS sku
            ON sku.SkuCode = CONCAT(
                'LEGACY-P',
                RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), product.Id), 10),
                '-S01')
        INNER JOIN ProductVariants AS variant
            ON variant.Id = sku.ProductVariantId
        WHERE variant.ProductId <> product.Id
           OR variant.VersionCode <> CONCAT('LEGACY-V', CONVERT(varchar(10), product.Id))
    )
    BEGIN
        THROW 51000, 'Legacy SKU code collision detected. Backfill was cancelled.', 1;
    END;

    INSERT INTO ProductVariants (ProductId, Name, VersionCode, Status)
    SELECT
        product.Id,
        N'Phiên bản hiện tại',
        CONCAT('LEGACY-V', CONVERT(varchar(10), product.Id)),
        N'Active'
    FROM Products AS product
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM ProductVariants AS variant
        WHERE variant.ProductId = product.Id
          AND variant.VersionCode = CONCAT('LEGACY-V', CONVERT(varchar(10), product.Id))
    );

    INSERT INTO VariantSpecifications
    (
        ProductVariantId,
        EngineType,
        FuelType,
        EngineCapacityCc,
        HorsePower,
        CurbWeightKg,
        Dimensions,
        FuelTankCapacityLiters,
        MaxPower,
        FuelConsumptionLitersPer100Km,
        OtherDetails
    )
    SELECT
        variant.Id,
        specification.EngineType,
        specification.FuelType,
        specification.EngineCapacityCc,
        specification.HorsePower,
        specification.CurbWeightKg,
        specification.Dimensions,
        specification.FuelTankCapacityLiters,
        specification.MaxPower,
        specification.FuelConsumptionLitersPer100Km,
        specification.OtherDetails
    FROM Products AS product
    INNER JOIN ProductVariants AS variant
        ON variant.ProductId = product.Id
       AND variant.VersionCode = CONCAT('LEGACY-V', CONVERT(varchar(10), product.Id))
    INNER JOIN Specifications AS specification
        ON specification.ProductId = product.Id
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM VariantSpecifications AS existing
        WHERE existing.ProductVariantId = variant.Id
    );

    INSERT INTO ProductSkus
    (
        ProductVariantId,
        SkuCode,
        ColorName,
        ColorHexCode,
        Price,
        StockQuantity,
        Status
    )
    SELECT
        variant.Id,
        CONCAT(
            'LEGACY-P',
            RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), product.Id), 10),
            '-S01'),
        COALESCE(NULLIF(LTRIM(RTRIM(product.Color)), N''), N'Chưa xác định'),
        NULL,
        product.Price,
        product.StockQuantity,
        N'Active'
    FROM Products AS product
    INNER JOIN ProductVariants AS variant
        ON variant.ProductId = product.Id
       AND variant.VersionCode = CONCAT('LEGACY-V', CONVERT(varchar(10), product.Id))
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM ProductSkus AS sku
        WHERE sku.SkuCode = CONCAT(
            'LEGACY-P',
            RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), product.Id), 10),
            '-S01')
    );

    INSERT INTO ProductImages
    (
        ProductSkuId,
        Url,
        AltText,
        DisplayOrder,
        IsPrimary
    )
    SELECT
        sku.Id,
        LTRIM(RTRIM(product.ImageUrl)),
        LEFT(product.Name, 200),
        0,
        1
    FROM Products AS product
    INNER JOIN ProductVariants AS variant
        ON variant.ProductId = product.Id
       AND variant.VersionCode = CONCAT('LEGACY-V', CONVERT(varchar(10), product.Id))
    INNER JOIN ProductSkus AS sku
        ON sku.ProductVariantId = variant.Id
       AND sku.SkuCode = CONCAT(
            'LEGACY-P',
            RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), product.Id), 10),
            '-S01')
    WHERE NULLIF(LTRIM(RTRIM(product.ImageUrl)), N'') IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM ProductImages AS image
          WHERE image.ProductSkuId = sku.Id
            AND image.IsPrimary = 1
      );

    IF EXISTS
    (
        SELECT 1
        FROM Products AS product
        LEFT JOIN ProductVariants AS variant
            ON variant.ProductId = product.Id
           AND variant.VersionCode = CONCAT('LEGACY-V', CONVERT(varchar(10), product.Id))
        LEFT JOIN ProductSkus AS sku
            ON sku.ProductVariantId = variant.Id
           AND sku.SkuCode = CONCAT(
               'LEGACY-P',
               RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), product.Id), 10),
               '-S01')
        WHERE variant.Id IS NULL
           OR sku.Id IS NULL
           OR sku.Price <> product.Price
           OR sku.StockQuantity <> product.StockQuantity
    )
    BEGIN
        THROW 51001, 'Legacy Product to Variant/SKU reconciliation failed. Backfill was rolled back.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM Specifications AS specification
        INNER JOIN ProductVariants AS variant
            ON variant.ProductId = specification.ProductId
           AND variant.VersionCode = CONCAT(
               'LEGACY-V',
               CONVERT(varchar(10), specification.ProductId))
        LEFT JOIN VariantSpecifications AS variantSpecification
            ON variantSpecification.ProductVariantId = variant.Id
        WHERE variantSpecification.ProductVariantId IS NULL
    )
    BEGIN
        THROW 51002, 'Legacy Specification reconciliation failed. Backfill was rolled back.', 1;
    END;

    IF
    (
        SELECT COALESCE(SUM(CONVERT(bigint, product.StockQuantity)), 0)
        FROM Products AS product
    ) <>
    (
        SELECT COALESCE(SUM(CONVERT(bigint, sku.StockQuantity)), 0)
        FROM ProductSkus AS sku
        INNER JOIN ProductVariants AS variant
            ON variant.Id = sku.ProductVariantId
        INNER JOIN Products AS product
            ON product.Id = variant.ProductId
        WHERE variant.VersionCode = CONCAT(
                  'LEGACY-V',
                  CONVERT(varchar(10), product.Id))
          AND sku.SkuCode = CONCAT(
                  'LEGACY-P',
                  RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), product.Id), 10),
                  '-S01')
    )
    BEGIN
        THROW 51003, 'Legacy stock total mismatch. Backfill was rolled back.', 1;
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728201821_BackfillLegacyProductCatalog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728201821_BackfillLegacyProductCatalog', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728202623_AddTransactionSkuLinks'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [ProductSkuId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728202623_AddTransactionSkuLinks'
)
BEGIN
    ALTER TABLE [ImportReceiptDetails] ADD [ProductSkuId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728202623_AddTransactionSkuLinks'
)
BEGIN
    SET NOCOUNT ON;

    UPDATE item
    SET item.ProductSkuId = sku.Id
    FROM OrderItems AS item
    INNER JOIN ProductVariants AS variant
        ON variant.ProductId = item.ProductId
       AND variant.VersionCode = CONCAT(
           'LEGACY-V',
           CONVERT(varchar(10), item.ProductId))
    INNER JOIN ProductSkus AS sku
        ON sku.ProductVariantId = variant.Id
       AND sku.SkuCode = CONCAT(
           'LEGACY-P',
           RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), item.ProductId), 10),
           '-S01')
    WHERE item.ProductSkuId IS NULL;

    UPDATE detail
    SET detail.ProductSkuId = sku.Id
    FROM ImportReceiptDetails AS detail
    INNER JOIN ProductVariants AS variant
        ON variant.ProductId = detail.ProductId
       AND variant.VersionCode = CONCAT(
           'LEGACY-V',
           CONVERT(varchar(10), detail.ProductId))
    INNER JOIN ProductSkus AS sku
        ON sku.ProductVariantId = variant.Id
       AND sku.SkuCode = CONCAT(
           'LEGACY-P',
           RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), detail.ProductId), 10),
           '-S01')
    WHERE detail.ProductSkuId IS NULL;

    IF EXISTS
    (
        SELECT 1
        FROM OrderItems AS item
        LEFT JOIN ProductSkus AS sku
            ON sku.Id = item.ProductSkuId
        LEFT JOIN ProductVariants AS variant
            ON variant.Id = sku.ProductVariantId
        WHERE item.ProductSkuId IS NULL
           OR variant.ProductId <> item.ProductId
    )
    BEGIN
        THROW 51010, 'OrderItem legacy SKU reconciliation failed. Migration was rolled back.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM ImportReceiptDetails AS detail
        LEFT JOIN ProductSkus AS sku
            ON sku.Id = detail.ProductSkuId
        LEFT JOIN ProductVariants AS variant
            ON variant.Id = sku.ProductVariantId
        WHERE detail.ProductSkuId IS NULL
           OR variant.ProductId <> detail.ProductId
    )
    BEGIN
        THROW 51011, 'ImportReceiptDetail legacy SKU reconciliation failed. Migration was rolled back.', 1;
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728202623_AddTransactionSkuLinks'
)
BEGIN
    CREATE INDEX [IX_OrderItems_ProductSkuId] ON [OrderItems] ([ProductSkuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728202623_AddTransactionSkuLinks'
)
BEGIN
    CREATE INDEX [IX_ImportReceiptDetails_ProductSkuId] ON [ImportReceiptDetails] ([ProductSkuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728202623_AddTransactionSkuLinks'
)
BEGIN
    ALTER TABLE [ImportReceiptDetails] ADD CONSTRAINT [FK_ImportReceiptDetails_ProductSkus_ProductSkuId] FOREIGN KEY ([ProductSkuId]) REFERENCES [ProductSkus] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728202623_AddTransactionSkuLinks'
)
BEGIN
    ALTER TABLE [OrderItems] ADD CONSTRAINT [FK_OrderItems_ProductSkus_ProductSkuId] FOREIGN KEY ([ProductSkuId]) REFERENCES [ProductSkus] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728202623_AddTransactionSkuLinks'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728202623_AddTransactionSkuLinks', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728210708_CutoverTransactionsToSku'
)
BEGIN
    DROP INDEX [IX_OrderItems_OrderId_ProductId] ON [OrderItems];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728210708_CutoverTransactionsToSku'
)
BEGIN
    ALTER TABLE [ImportReceiptDetails] DROP CONSTRAINT [PK_ImportReceiptDetails];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728210708_CutoverTransactionsToSku'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [ColorNameSnapshot] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728210708_CutoverTransactionsToSku'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [ProductNameSnapshot] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728210708_CutoverTransactionsToSku'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [SkuCodeSnapshot] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728210708_CutoverTransactionsToSku'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [VariantNameSnapshot] nvarchar(120) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728210708_CutoverTransactionsToSku'
)
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM OrderItems AS item
        LEFT JOIN ProductSkus AS sku
            ON sku.Id = item.ProductSkuId
        LEFT JOIN ProductVariants AS variant
            ON variant.Id = sku.ProductVariantId
        WHERE item.ProductSkuId IS NULL
           OR sku.Id IS NULL
           OR variant.ProductId <> item.ProductId
    )
    BEGIN
        THROW 51020, 'OrderItem SKU cutover validation failed. Migration was rolled back.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM ImportReceiptDetails AS detail
        LEFT JOIN ProductSkus AS sku
            ON sku.Id = detail.ProductSkuId
        LEFT JOIN ProductVariants AS variant
            ON variant.Id = sku.ProductVariantId
        WHERE detail.ProductSkuId IS NULL
           OR sku.Id IS NULL
           OR variant.ProductId <> detail.ProductId
    )
    BEGIN
        THROW 51021, 'ImportReceiptDetail SKU cutover validation failed. Migration was rolled back.', 1;
    END;

    UPDATE item
    SET
        item.ProductNameSnapshot = product.Name,
        item.VariantNameSnapshot = variant.Name,
        item.ColorNameSnapshot = sku.ColorName,
        item.SkuCodeSnapshot = sku.SkuCode
    FROM OrderItems AS item
    INNER JOIN ProductSkus AS sku
        ON sku.Id = item.ProductSkuId
    INNER JOIN ProductVariants AS variant
        ON variant.Id = sku.ProductVariantId
    INNER JOIN Products AS product
        ON product.Id = variant.ProductId;

    IF EXISTS
    (
        SELECT 1
        FROM OrderItems AS item
        WHERE NULLIF(LTRIM(RTRIM(item.ProductNameSnapshot)), N'') IS NULL
           OR NULLIF(LTRIM(RTRIM(item.VariantNameSnapshot)), N'') IS NULL
           OR NULLIF(LTRIM(RTRIM(item.ColorNameSnapshot)), N'') IS NULL
           OR NULLIF(LTRIM(RTRIM(item.SkuCodeSnapshot)), N'') IS NULL
    )
    BEGIN
        THROW 51022, 'OrderItem snapshot backfill failed. Migration was rolled back.', 1;
    END;

    IF EXISTS
    (
        SELECT item.OrderId, item.ProductSkuId
        FROM OrderItems AS item
        GROUP BY item.OrderId, item.ProductSkuId
        HAVING COUNT(*) > 1
    )
    BEGIN
        THROW 51023, 'Duplicate OrderId/ProductSkuId detected. Migration was rolled back.', 1;
    END;

    IF EXISTS
    (
        SELECT detail.ImportReceiptId, detail.ProductSkuId
        FROM ImportReceiptDetails AS detail
        GROUP BY detail.ImportReceiptId, detail.ProductSkuId
        HAVING COUNT(*) > 1
    )
    BEGIN
        THROW 51024, 'Duplicate ImportReceiptId/ProductSkuId detected. Migration was rolled back.', 1;
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728210708_CutoverTransactionsToSku'
)
BEGIN
    DROP INDEX [IX_OrderItems_ProductSkuId] ON [OrderItems];
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderItems]') AND [c].[name] = N'ProductSkuId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [OrderItems] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [OrderItems] ALTER COLUMN [ProductSkuId] int NOT NULL;
    CREATE INDEX [IX_OrderItems_ProductSkuId] ON [OrderItems] ([ProductSkuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728210708_CutoverTransactionsToSku'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderItems]') AND [c].[name] = N'ColorNameSnapshot');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [OrderItems] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [OrderItems] ALTER COLUMN [ColorNameSnapshot] nvarchar(100) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728210708_CutoverTransactionsToSku'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderItems]') AND [c].[name] = N'ProductNameSnapshot');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [OrderItems] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [OrderItems] ALTER COLUMN [ProductNameSnapshot] nvarchar(200) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728210708_CutoverTransactionsToSku'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderItems]') AND [c].[name] = N'SkuCodeSnapshot');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [OrderItems] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [OrderItems] ALTER COLUMN [SkuCodeSnapshot] nvarchar(64) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728210708_CutoverTransactionsToSku'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderItems]') AND [c].[name] = N'VariantNameSnapshot');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [OrderItems] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [OrderItems] ALTER COLUMN [VariantNameSnapshot] nvarchar(120) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728210708_CutoverTransactionsToSku'
)
BEGIN
    DROP INDEX [IX_ImportReceiptDetails_ProductSkuId] ON [ImportReceiptDetails];
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ImportReceiptDetails]') AND [c].[name] = N'ProductSkuId');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [ImportReceiptDetails] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [ImportReceiptDetails] ALTER COLUMN [ProductSkuId] int NOT NULL;
    CREATE INDEX [IX_ImportReceiptDetails_ProductSkuId] ON [ImportReceiptDetails] ([ProductSkuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728210708_CutoverTransactionsToSku'
)
BEGIN
    ALTER TABLE [ImportReceiptDetails] ADD CONSTRAINT [PK_ImportReceiptDetails] PRIMARY KEY ([ImportReceiptId], [ProductSkuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728210708_CutoverTransactionsToSku'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OrderItems_OrderId_ProductSkuId] ON [OrderItems] ([OrderId], [ProductSkuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728210708_CutoverTransactionsToSku'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728210708_CutoverTransactionsToSku', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    IF EXISTS (
        SELECT 1
        FROM OrderItems AS item
        LEFT JOIN ProductSkus AS sku ON sku.Id = item.ProductSkuId
        LEFT JOIN ProductVariants AS variant ON variant.Id = sku.ProductVariantId
        WHERE sku.Id IS NULL OR variant.ProductId <> item.ProductId
    )
        THROW 51000, 'Cleanup blocked: OrderItems contain missing or mismatched SKU links.', 1;

    IF EXISTS (
        SELECT 1
        FROM ImportReceiptDetails AS detail
        LEFT JOIN ProductSkus AS sku ON sku.Id = detail.ProductSkuId
        LEFT JOIN ProductVariants AS variant ON variant.Id = sku.ProductVariantId
        WHERE sku.Id IS NULL OR variant.ProductId <> detail.ProductId
    )
        THROW 51000, 'Cleanup blocked: ImportReceiptDetails contain missing or mismatched SKU links.', 1;

    IF EXISTS (
        SELECT 1
        FROM OrderItems
        WHERE NULLIF(LTRIM(RTRIM(ProductNameSnapshot)), '') IS NULL
           OR NULLIF(LTRIM(RTRIM(VariantNameSnapshot)), '') IS NULL
           OR NULLIF(LTRIM(RTRIM(ColorNameSnapshot)), '') IS NULL
           OR NULLIF(LTRIM(RTRIM(SkuCodeSnapshot)), '') IS NULL
    )
        THROW 51000, 'Cleanup blocked: OrderItem snapshots are incomplete.', 1;

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
        THROW 51000, 'Cleanup blocked: at least one Product has no SKU.', 1;

    IF EXISTS (
        SELECT 1
        FROM ProductSkus
        WHERE Price < 0 OR StockQuantity < 0
    )
        THROW 51000, 'Cleanup blocked: SKU price or stock is negative.', 1;

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
        THROW 51000, 'Cleanup blocked: Product stock and aggregate SKU stock differ.', 1;

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
        THROW 51000, 'Cleanup blocked: a legacy Specification was not migrated.', 1;

    IF EXISTS (
        SELECT 1
        FROM Products AS product
        WHERE NULLIF(LTRIM(RTRIM(product.ImageUrl)), '') IS NOT NULL
          AND NOT EXISTS (
              SELECT 1
              FROM ProductVariants AS variant
              INNER JOIN ProductSkus AS sku ON sku.ProductVariantId = variant.Id
              INNER JOIN ProductImages AS image ON image.ProductSkuId = sku.Id
              WHERE variant.ProductId = product.Id
                AND image.Url = product.ImageUrl
          )
    )
        THROW 51000, 'Cleanup blocked: a legacy Product image was not migrated.', 1;
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
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Color');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Products] DROP COLUMN [Color];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'ImageUrl');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [Products] DROP COLUMN [ImageUrl];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Price');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [Products] DROP COLUMN [Price];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'StockQuantity');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [Products] DROP COLUMN [StockQuantity];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderItems]') AND [c].[name] = N'ProductId');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [OrderItems] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [OrderItems] DROP COLUMN [ProductId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728221857_CleanupLegacyProductSchema'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ImportReceiptDetails]') AND [c].[name] = N'ProductId');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [ImportReceiptDetails] DROP CONSTRAINT ' + @var11 + ';');
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

