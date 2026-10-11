Запити перевірки (CRUD, пошук, пагінацію і транзакційний сценарій)

-- p1 (EastLeaf_OrderDB)
select * from vw_OrderSummary; 
select * from Customer where Email like 'olena%';
select OrderID, OrderDate, TotalAmount from Orders
order by OrderDate desc offset 0 rows fetch next 2 rows only;

insert into Customer (FirstName, LastName, Email) values (N'Test', N'User', N'test@example.com');
update Customer set PhoneNumber = N'+380000000000', UpdatedAt = sysutcdatetime() where Email = N'test@example.com';
update Customer set IsDeleted = 1 where Email = N'test@example.com';

begin transaction;
update Orders set OrderState = 'AwaitingValidation', UpdatedAt = sysutcdatetime()
where OrderID = 1 and OrderState = 'Submitted';
select OrderID, OrderState from Orders where OrderID = 1;
rollback transaction;

update Orders set OrderState = 'Done' where OrderID = 1;


-- p2 (EastLeaf_CatalogDB)
select * from vw_ProductCatalog;
select ProductID, ProductName, Price from Products where ProductName like 'Da%';
select ProductID, ProductName, Price from Products
order by ProductID offset 0 rows fetch next 3 rows only;
select p.ProductName from Products p
join ProductTags pt on pt.ProductID = p.ProductID
join Tags t on t.TagID = pt.TagID where t.TagName = N'Bestseller';
update Products set Price = 320.00, UpdatedAt = sysutcdatetime() where ProductID = 1;
insert into Products (CategoryID, ProductName, Price, StockQuantity) values (1, N'Test', -5, 1);


-- p3 (MongoDB)
db.reviews.find({ productId: 4 })  
db.reviews.find().sort({ createdAt: -1 }).skip(0).limit(2) 
db.reviews.aggregate([{ $group: { _id: "$productName", avg: { $avg: "$rating" } } }])
db.reviews.updateOne({ _id: ObjectId("66f0c2000000000000000002") }, { $set: { rating: 5 } })