IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017155454_InitialCreate'
)
BEGIN
    CREATE TABLE [Categories] (
        [CategoryId] int NOT NULL IDENTITY,
        [CategoryName] nvarchar(100) NOT NULL,
        [Descrip] nvarchar(500) NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryId]),
        CONSTRAINT [CK_Categories_Name] CHECK (len(ltrim(rtrim([CategoryName]))) > 0)
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017155454_InitialCreate'
)
BEGIN
    CREATE TABLE [Tags] (
        [TagId] int NOT NULL IDENTITY,
        [TagName] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Tags] PRIMARY KEY ([TagId]),
        CONSTRAINT [CK_Tags_Name] CHECK (len(ltrim(rtrim([TagName]))) > 0)
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017155454_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [ProductId] int NOT NULL IDENTITY,
        [CategoryId] int NOT NULL,
        [ProductName] nvarchar(200) NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [StockQuantity] int NOT NULL,
        [IsAvailable] bit NOT NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([ProductId]),
        CONSTRAINT [CK_Products_Name] CHECK (len(ltrim(rtrim([ProductName]))) > 0),
        CONSTRAINT [CK_Products_Price] CHECK ([Price] > 0),
        CONSTRAINT [CK_Products_Stock] CHECK ([StockQuantity] >= 0),
        CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([CategoryId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017155454_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductDetails] (
        [ProductId] int NOT NULL,
        [Descrip] nvarchar(max) NULL,
        [OriginCountry] nvarchar(100) NOT NULL,
        [Region] nvarchar(150) NULL,
        [TeaForm] nvarchar(50) NOT NULL,
        [TeaVariety] nvarchar(100) NOT NULL,
        [FlavorNotes] nvarchar(300) NULL,
        [WeightGrams] int NOT NULL,
        CONSTRAINT [PK_ProductDetails] PRIMARY KEY ([ProductId]),
        CONSTRAINT [CK_ProductDetails_Weight] CHECK ([WeightGrams] > 0),
        CONSTRAINT [FK_ProductDetails_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017155454_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductImages] (
        [ImageId] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [ImageUrl] nvarchar(500) NOT NULL,
        [IsMain] bit NOT NULL,
        CONSTRAINT [PK_ProductImages] PRIMARY KEY ([ImageId]),
        CONSTRAINT [CK_ProductImages_Url] CHECK (len(ltrim(rtrim([ImageUrl]))) > 0),
        CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017155454_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductTags] (
        [ProductId] int NOT NULL,
        [TagId] int NOT NULL,
        [AddedDate] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ProductTags] PRIMARY KEY ([ProductId], [TagId]),
        CONSTRAINT [FK_ProductTags_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductTags_Tags_TagId] FOREIGN KEY ([TagId]) REFERENCES [Tags] ([TagId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017155454_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_CategoryName] ON [Categories] ([CategoryName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017155454_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_ProductImages_Main] ON [ProductImages] ([ProductId]) WHERE [IsMain] = 1');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017155454_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_CategoryId_IsAvailable] ON [Products] ([CategoryId], [IsAvailable]) INCLUDE ([ProductName], [Price]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017155454_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_ProductName] ON [Products] ([ProductName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017155454_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductTags_TagId] ON [ProductTags] ([TagId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017155454_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tags_TagName] ON [Tags] ([TagName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261017155454_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261017155454_InitialCreate', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018144542_AddProductImagesIndex'
)
BEGIN
    CREATE INDEX [IX_ProductImages_ProductId] ON [ProductImages] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018144542_AddProductImagesIndex'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018144542_AddProductImagesIndex', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184930_SeedReferenceData'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'CategoryId', N'CategoryName', N'Descrip') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] ON;
    EXEC(N'INSERT INTO [Categories] ([CategoryId], [CategoryName], [Descrip])
    VALUES (1, N''Green Tea'', N''Fresh and grassy loose leaf green teas''),
    (2, N''Black Tea'', N''Strong and full-bodied fermented teas''),
    (3, N''Puerh Tea'', N''Aged and fermented compressed dark teas''),
    (4, N''Oolong Tea'', N''Partially oxidized semi-fermented teas''),
    (5, N''Teaware & Accessories'', N''Ceramic gaiwans, pots, and cups for brewing'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'CategoryId', N'CategoryName', N'Descrip') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184930_SeedReferenceData'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'TagId', N'TagName') AND [object_id] = OBJECT_ID(N'[Tags]'))
        SET IDENTITY_INSERT [Tags] ON;
    EXEC(N'INSERT INTO [Tags] ([TagId], [TagName])
    VALUES (1, N''Organic''),
    (2, N''Bestseller''),
    (3, N''New''),
    (4, N''Premium''),
    (5, N''Limited Edition'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'TagId', N'TagName') AND [object_id] = OBJECT_ID(N'[Tags]'))
        SET IDENTITY_INSERT [Tags] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184930_SeedReferenceData'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019184930_SeedReferenceData', N'8.0.0');
END;
GO

COMMIT;
GO

