use EastLeaf_CatalogDB;
go

set identity_insert Categories on;
insert into Categories (CategoryID, CategoryName, Descrip)
select v.CategoryID, v.CategoryName, v.Descrip
from (values
(1, N'Green Tea', N'Fresh and grassy loose leaf green teas'),
(2, N'Black Tea', N'Strong and full-bodied fermented teas'),
(3, N'Puerh Tea', N'Aged and fermented compressed dark teas'),
(4, N'Oolong Tea',N'Partially oxidized semi-fermented teas'),
(5, N'Teaware & Accessories', N'Ceramic gaiwans, pots, and cups for brewing')
) as v(CategoryID, CategoryName, Descrip)
where not exists (select 1 from Categories c where c.CategoryID = v.CategoryID);
set identity_insert Categories off;
go

set identity_insert Products on;
insert into Products (ProductID, CategoryID, ProductName, Price, StockQuantity, IsAvailable)
select v.ProductID, v.CategoryID, v.ProductName, v.Price, v.StockQuantity, v.IsAvailable
from (values
(1, 1, N'Sencha Premium', 300.00, 50, 1),
(2, 2, N'Darjeeling First Flush', 550.00, 30, 1),
(3, 3, N'Shu Pu-erh Aged 2018', 600.00, 25, 1),
(4, 4, N'Da Hong Pao Oolong', 450.00, 40, 1),
(5, 5, N'Ceramic Gaiwan Set', 850.00, 15, 1)
) as v(ProductID, CategoryID, ProductName, Price, StockQuantity, IsAvailable)
where not exists (select 1 from Products p where p.ProductID = v.ProductID);
set identity_insert Products off;
go

insert into ProductDetails (ProductID, Descrip, OriginCountry, Region, TeaForm, TeaVariety, FlavorNotes, WeightGrams)
select v.ProductID, v.Descrip, v.OriginCountry, v.Region, v.TeaForm, v.TeaVariety, v.FlavorNotes, v.WeightGrams
from (values
(1, N'Delicate steamed green tea with rich umami flavor.', N'Japan', N'Shizuoka', N'Loose leaf', N'Sencha', N'Grass, seaweed, sweet finish', 100),
(2, N'Champagne of teas from the Himalayan foothills.', N'India', N'Darjeeling', N'Loose leaf', N'Black', N'Muscatel, floral, honey', 100),
(3, N'Earthy and smooth fermented dark tea cake.', N'China', N'Yunnan', N'Compressed cake',N'Shu Pu-erh',N'Earth, wood, walnut', 150),
(4, N'Legendary rock oolong with complex mineral profile.', N'China', N'Wuyi Mountains', N'Rolled leaves', N'Oolong', N'Mineral, orchid, roasted nuts', 100),
(5, N'Traditional Chinese tea brewing bowl and lid.', N'China', N'Fujian', N'Hardware', N'Accessories',N'Neutral ceramic', 250)
) as v(ProductID, Descrip, OriginCountry, Region, TeaForm, TeaVariety, FlavorNotes, WeightGrams)
where not exists (select 1 from ProductDetails d where d.ProductID = v.ProductID);
go

insert into ProductImages (ProductID, ImageUrl, IsMain)
select v.ProductID, v.ImageUrl, v.IsMain
from (values
(1, N'https://ad-ua.com/image/cache/catalog/sencha-1300x1300.jpg', 1),
(2, N'https://ad-ua.com/image/cache/catalog/dardzhiling-1300x1300.jpg', 1),
(3, N'https://ad-ua.com/image/cache/catalog/puerchornijvagovij-1300x1300.jpg', 1),
(4, N'https://ad-ua.com/image/cache/catalog/photo_2024-02-1016.51.00-1300x1300.jpeg', 1),
(5, N'https://todd-holland.com/products/fish-gaiwan-2-cups-copy-1', 1)
) as v(ProductID, ImageUrl, IsMain)
where not exists (select 1 from ProductImages i where i.ProductID = v.ProductID and i.ImageUrl = v.ImageUrl);
go

insert into BrewingGuides (ProductID, WaterTemperature, BrewTimeSeconds, SteepsCount)
select v.ProductID, v.WaterTemperature, v.BrewTimeSeconds, v.SteepsCount
from (values (1, 80, 90, 3), (2, 95, 180, 4), (3, 100, 30, 8), (4, 90, 60, 6), (5, 95, 10, 10)
) as v(ProductID, WaterTemperature, BrewTimeSeconds, SteepsCount)
where not exists (select 1 from BrewingGuides g where g.ProductID = v.ProductID);
go

set identity_insert Tags on;
insert into Tags (TagID, TagName)
select v.TagID, v.TagName
from (values (1, N'Organic'), (2, N'Bestseller'), (3, N'New'), (4, N'Premium'), (5, N'Limited Edition')
) as v(TagID, TagName)
where not exists (select 1 from Tags t where t.TagID = v.TagID);
set identity_insert Tags off;
go

insert into ProductTags (ProductID, TagID)
select v.ProductID, v.TagID
from (values (1,1),(1,2),(2,4),(3,2),(3,5),(4,3),(4,4),(5,2)
) as v(ProductID, TagID)
where not exists (select 1 from ProductTags pt where pt.ProductID = v.ProductID and pt.TagID = v.TagID);
go