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

CREATE TABLE [Categories] (
    [CategoryId] int NOT NULL IDENTITY,
    [CategoryName] nvarchar(100) NOT NULL,
    [Descrip] nvarchar(500) NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryId]),
    CONSTRAINT [CK_Categories_Name] CHECK (len(ltrim(rtrim([CategoryName]))) > 0)
);
GO

CREATE TABLE [Tags] (
    [TagId] int NOT NULL IDENTITY,
    [TagName] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_Tags] PRIMARY KEY ([TagId]),
    CONSTRAINT [CK_Tags_Name] CHECK (len(ltrim(rtrim([TagName]))) > 0)
);
GO

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
GO

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
GO

CREATE TABLE [ProductImages] (
    [ImageId] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [ImageUrl] nvarchar(500) NOT NULL,
    [IsMain] bit NOT NULL,
    CONSTRAINT [PK_ProductImages] PRIMARY KEY ([ImageId]),
    CONSTRAINT [CK_ProductImages_Url] CHECK (len(ltrim(rtrim([ImageUrl]))) > 0),
    CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId]) ON DELETE CASCADE
);
GO

CREATE TABLE [ProductTags] (
    [ProductId] int NOT NULL,
    [TagId] int NOT NULL,
    [AddedDate] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_ProductTags] PRIMARY KEY ([ProductId], [TagId]),
    CONSTRAINT [FK_ProductTags_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductTags_Tags_TagId] FOREIGN KEY ([TagId]) REFERENCES [Tags] ([TagId]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_Categories_CategoryName] ON [Categories] ([CategoryName]);
GO

CREATE UNIQUE INDEX [UX_ProductImages_Main] ON [ProductImages] ([ProductId]) WHERE [IsMain] = 1;
GO

CREATE INDEX [IX_Products_CategoryId_IsAvailable] ON [Products] ([CategoryId], [IsAvailable]) INCLUDE ([ProductName], [Price]);
GO

CREATE INDEX [IX_Products_ProductName] ON [Products] ([ProductName]);
GO

CREATE INDEX [IX_ProductTags_TagId] ON [ProductTags] ([TagId]);
GO

CREATE UNIQUE INDEX [IX_Tags_TagName] ON [Tags] ([TagName]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261017155454_InitialCreate', N'8.0.0');
GO

COMMIT;
GO

