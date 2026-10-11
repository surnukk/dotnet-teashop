use EastLeaf_OrderDB;
go

set identity_insert Customer on;
insert into Customer (CustomerID, FirstName, LastName, Email, PhoneNumber, DeliveryAddress)
select v.CustomerID, v.FirstName, v.LastName, v.Email, v.PhoneNumber, v.DeliveryAddress
from (values
(1, N'Olena', N'Petrenko', N'olena.petrenko@gmail.com', N'+380501112233', N'Kyiv, Khreshch st., 1'),
(2, N'Ivan', N'Kozak', N'ivan.kozak@gmail.com', N'+380674445566', N'Lviv, Svobody ave., 12'),
(3, N'Mariya', N'Shevchenko', N'mariya.shev@gmail.com', N'+380637778899', N'Odesa, Derybasi st., 5'),
(4, N'Andriy', N'Melnyk', N'andriy.melnyk@gmail.com', N'+380503332211', N'Kharkiv, Sumska st., 25'),
(5, N'Sofia', N'Bondar', N'sofia.bondar@gmail.com', N'+380935554433', N'Dnipro, Yavorny ave., 40')
) as v(CustomerID, FirstName, LastName, Email, PhoneNumber, DeliveryAddress)
where not exists (select 1 from Customer c where c.CustomerID = v.CustomerID);
set identity_insert Customer off;
go

set identity_insert Promotions on;
insert into Promotions (PromotionID, Code, DiscountPercentage, StartDate, EndDate)
select v.PromotionID, v.Code, v.DiscountPercentage, v.StartDate, v.EndDate
from (values
(1, N'TEA10', 10.00, '2026-01-01', '2026-12-31'),
(2, N'SPRING20', 20.00, '2026-03-01', '2026-05-31'),
(3, N'SUMMER15',  15.00, '2026-06-01', '2026-08-31'),
(4, N'AUTUMN5', 5.00, '2026-09-01', '2026-11-30'),
(5, N'WELCOME50', 50.00, '2026-01-01', '2026-12-31')
) as v(PromotionID, Code, DiscountPercentage, StartDate, EndDate)
where not exists (select 1 from Promotions p where p.PromotionID = v.PromotionID);
set identity_insert Promotions off;
go

insert into Products (ProductID, ProductName, Price)
select v.ProductID, v.ProductName, v.Price
from (values
(1, N'Sencha Premium', 300.00),
(2, N'Darjeeling First Flush', 550.00),
(3, N'Shu Pu-erh Aged 2018', 600.00),
(4, N'Da Hong Pao Oolong', 450.00),
(5, N'Ceramic Gaiwan Set', 850.00)
) as v(ProductID, ProductName, Price)
where not exists (select 1 from Products p where p.ProductID = v.ProductID);
go

set identity_insert Orders on;
insert into Orders (OrderID, CustomerID, PromotionID, OrderDate, OrderState, TotalAmount)
select v.OrderID, v.CustomerID, v.PromotionID, v.OrderDate, v.OrderState, v.TotalAmount
from (values
(1, 1, 1, '2026-10-05', N'Submitted', 675.00),
(2, 2, null, '2026-10-06', N'StockConfirmed', 1200.00),
(3, 3, 4, '2026-10-02', N'Paid', 427.50),
(4, 4, null, '2026-09-28', N'Shipped', 1950.00),
(5, 5, 5, '2026-09-20', N'Cancelled', 150.00)
) as v(OrderID, CustomerID, PromotionID, OrderDate, OrderState, TotalAmount)
where not exists (select 1 from Orders o where o.OrderID = v.OrderID);
set identity_insert Orders off;
go

insert into OrderItems (OrderID, ProductID, ProductName, UnitPrice, Units)
select v.OrderID, v.ProductID, v.ProductName, v.UnitPrice, v.Units
from (values
(1, 1, N'Sencha Premium', 300.00, 1),
(1, 4, N'Da Hong Pao Oolong', 450.00, 1),
(2, 3, N'Shu Pu-erh Aged 2018', 600.00, 2),
(3, 4, N'Da Hong Pao Oolong', 450.00, 1),
(4, 2, N'Darjeeling First Flush', 550.00, 2),
(4, 5, N'Ceramic Gaiwan Set', 850.00, 1),
(5, 1, N'Sencha Premium', 300.00, 1)
) as v(OrderID, ProductID, ProductName, UnitPrice, Units)
where not exists (select 1 from OrderItems oi where oi.OrderID = v.OrderID and oi.ProductID = v.ProductID);
go

insert into PaymentRecords (OrderID, PaymentMethod, Amount, PaymentStatus)
select v.OrderID, v.PaymentMethod, v.Amount, v.PaymentStatus
from (values
(1, N'Card', 675.00, N'Pending'),
(2, N'ApplePay', 1200.00, N'Pending'),
(3, N'GooglePay', 427.50, N'Completed'),
(4, N'Card', 1950.00, N'Completed'),
(5, N'CashOnDelivery', 150.00,  N'Failed')
) as v(OrderID, PaymentMethod, Amount, PaymentStatus)
where not exists (select 1 from PaymentRecords p where p.OrderID = v.OrderID);
go