IF OBJECT_ID(N'dbo.ProcessedEvents', N'U') IS NULL
    CREATE TABLE dbo.ProcessedEvents (
        EventId     UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProcessedEvents PRIMARY KEY,
        ProcessedAt DATETIME2        NOT NULL CONSTRAINT DF_ProcessedEvents_ProcessedAt DEFAULT SYSUTCDATETIME()
    );

IF COL_LENGTH(N'dbo.Products', N'NameUpdatedAt') IS NULL
    ALTER TABLE dbo.Products ADD NameUpdatedAt DATETIME2 NULL;

IF COL_LENGTH(N'dbo.Products', N'PriceUpdatedAt') IS NULL
    ALTER TABLE dbo.Products ADD PriceUpdatedAt DATETIME2 NULL;