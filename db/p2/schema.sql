create database EastLeaf_CatalogDB;
go

use EastLeaf_CatalogDB;
go

create table Categories (
CategoryID   int identity(1,1) primary key,
CategoryName nvarchar(100) not null unique,
Descrip      nvarchar(500) null
);
go

create table Products (
ProductID     int identity(1,1) primary key,
CategoryID    int           not null,
ProductName   nvarchar(200) not null,
Price         decimal(18,2) not null,
StockQuantity int           not null,
IsAvailable   bit           not null default 1,
constraint CK_Products_Price check (Price > 0),
constraint CK_Products_Stock check (StockQuantity >= 0),
constraint FK_Products_Categories foreign key (CategoryID) references Categories(CategoryID)
);
go

create table ProductDetails (
ProductID     int           primary key,
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
ProductID        int primary key,
WaterTemperature int not null,
BrewTimeSeconds  int not null,
SteepsCount      int not null,
constraint CK_Brewing_Temp   check (WaterTemperature between 1 and 100),
constraint CK_Brewing_Time   check (BrewTimeSeconds > 0),
constraint CK_Brewing_Steeps check (SteepsCount > 0),
constraint FK_BrewingGuides_Products foreign key (ProductID) references Products(ProductID) on delete cascade
);
go

create table ProductImages (
ImageID   int identity(1,1) primary key,
ProductID int           not null,
ImageUrl  nvarchar(500) not null,
IsMain    bit           not null default 0,
constraint FK_ProductImages_Products foreign key (ProductID) references Products(ProductID) on delete cascade
);
go

create table Tags (
TagID   int identity(1,1) primary key,
TagName nvarchar(50) not null unique
);
go

create table ProductTags (
ProductID int       not null,
TagID     int       not null,
AddedDate datetime2 not null default sysdatetime(),
constraint PK_ProductTags primary key (ProductID, TagID),
constraint FK_ProductTags_Products foreign key (ProductID) references Products(ProductID) on delete cascade,
constraint FK_ProductTags_Tags     foreign key (TagID)     references Tags(TagID)         on delete cascade
);
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