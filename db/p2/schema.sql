use master;
go

if db_id('EastLeaf_CatalogDB') is not null
begin
    alter database EastLeaf_CatalogDB set single_user with rollback immediate;
    drop database EastLeaf_CatalogDB;
end
go

create database EastLeaf_CatalogDB;
go

use EastLeaf_CatalogDB;
go

create table Categories (
CategoryID   bigint identity(1,1) primary key,
CategoryName nvarchar(100) not null unique,
Descrip      nvarchar(500) null,
constraint CK_Categories_Name check (len(ltrim(rtrim(CategoryName))) > 0)
);
go

create table Products (
ProductID     bigint identity(1,1) primary key,
CategoryID    bigint        not null,
ProductName   nvarchar(200) not null,
Price         decimal(18,2) not null,
StockQuantity int           not null default 0,
IsAvailable   bit           not null default 1,
constraint CK_Products_Name  check (len(ltrim(rtrim(ProductName))) > 0),
constraint CK_Products_Price check (Price > 0),
constraint CK_Products_Stock check (StockQuantity >= 0),
constraint FK_Products_Categories foreign key (CategoryID) references Categories(CategoryID)
);
go

create table ProductDetails (
ProductID     bigint        primary key,
Descrip       nvarchar(max) null,
OriginCountry nvarchar(100) not null,
Region        nvarchar(150) null,
TeaForm       nvarchar(50)  not null,
TeaVariety    nvarchar(100) not null,
FlavorNotes   nvarchar(300) null,
WeightGrams   int           not null,
constraint CK_ProductDetails_Weight check (WeightGrams > 0),
constraint FK_ProductDetails_Products foreign key (ProductID) references Products(ProductID) on delete cascade
);
go

create table BrewingGuides (
ProductID        bigint primary key,
WaterTemperature int    not null,
BrewTimeSeconds  int    not null,
SteepsCount      int    not null,
constraint CK_Brewing_Temp   check (WaterTemperature between 1 and 100),
constraint CK_Brewing_Time   check (BrewTimeSeconds > 0),
constraint CK_Brewing_Steeps check (SteepsCount > 0),
constraint FK_BrewingGuides_Products foreign key (ProductID) references Products(ProductID) on delete cascade
);
go

create table ProductImages (
ImageID   bigint identity(1,1) primary key,
ProductID bigint        not null,
ImageUrl  nvarchar(500) not null,
IsMain    bit           not null default 0,
constraint CK_ProductImages_Url check (len(ltrim(rtrim(ImageUrl))) > 0),
constraint FK_ProductImages_Products foreign key (ProductID) references Products(ProductID) on delete cascade
);
go

create table Tags (
TagID   bigint identity(1,1) primary key,
TagName nvarchar(50) not null unique,
constraint CK_Tags_Name check (len(ltrim(rtrim(TagName))) > 0)
);
go

create table ProductTags (
ProductID bigint    not null,
TagID     bigint    not null,
AddedDate datetime2 not null default sysdatetime(),
constraint PK_ProductTags primary key (ProductID, TagID),
constraint FK_ProductTags_Products foreign key (ProductID) references Products(ProductID) on delete cascade,
constraint FK_ProductTags_Tags     foreign key (TagID)     references Tags(TagID)         on delete cascade
);
go

create index IX_Products_CategoryID_IsAvailable on Products(CategoryID, IsAvailable) include (ProductName, Price);
create index IX_Products_ProductName on Products(ProductName);
create index IX_ProductImages_ProductID on ProductImages(ProductID);
create unique index UX_ProductImages_Main on ProductImages(ProductID) where IsMain = 1;
create index IX_ProductTags_TagID on ProductTags(TagID);
go

create view vw_ProductCatalog as
select  p.ProductID, p.ProductName, c.CategoryName, p.Price, p.StockQuantity, p.IsAvailable,
        d.OriginCountry, d.Region, d.TeaForm, d.TeaVariety, d.FlavorNotes, d.WeightGrams,
        g.WaterTemperature, g.BrewTimeSeconds, g.SteepsCount, i.ImageUrl as MainImageUrl
from Products p
join Categories c          on c.CategoryID = p.CategoryID
left join ProductDetails d on d.ProductID = p.ProductID
left join BrewingGuides g  on g.ProductID = p.ProductID
left join ProductImages i  on i.ProductID = p.ProductID and i.IsMain = 1;
go