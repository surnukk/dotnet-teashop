create database EastLeaf_OrderDB;
go

use EastLeaf_OrderDB;
go

create table Customer (
CustomerID      int identity(1,1) primary key,
FirstName       nvarchar(50)  not null,
LastName        nvarchar(50)  not null,
Email           nvarchar(100) not null unique,
PhoneNumber     nvarchar(20)  null,
DeliveryAddress nvarchar(255) null,
IsActive        bit not null default 1
);
go

create table Promotions (
PromotionID        int identity(1,1) primary key,
Code               nvarchar(50)  not null unique,
DiscountPercentage decimal(5,2)  not null,
StartDate          datetime2     not null,
EndDate            datetime2     not null,
IsActive           bit not null default 1,
constraint CK_Promotions_Discount check (DiscountPercentage > 0 and DiscountPercentage <= 100),
constraint CK_Promotions_Dates    check (EndDate > StartDate)
);
go

create table Products (
ProductID   int           primary key,
ProductName nvarchar(200) not null,
Price       decimal(18,2) not null,
constraint CK_Products_Price check (Price > 0)
);
go

create table Orders (
OrderID     int identity(1,1) primary key,
CustomerID  int           not null,
PromotionID int           null,
OrderDate   datetime2     not null default sysdatetime(),
OrderState  nvarchar(30)  not null default 'Submitted',
TotalAmount decimal(18,2) not null,
constraint CK_Orders_Status check (OrderState in ('Submitted','AwaitingValidation','StockConfirmed','Paid','Shipped','Cancelled')),
constraint CK_Orders_Total  check (TotalAmount >= 0),
constraint FK_Orders_Customer   foreign key (CustomerID)  references Customer(CustomerID),
constraint FK_Orders_Promotions foreign key (PromotionID) references Promotions(PromotionID) on delete set null
);
go

create table OrderItems (
OrderID     int           not null,
ProductID   int           not null,
ProductName nvarchar(200) not null,
UnitPrice   decimal(18,2) not null,
Units       int           not null,
constraint PK_OrderItems primary key (OrderID, ProductID),
constraint CK_OrderItems_Price check (UnitPrice >= 0),
constraint CK_OrderItems_Units check (Units > 0),
constraint FK_OrderItems_Orders   foreign key (OrderID)   references Orders(OrderID) on delete cascade,
constraint FK_OrderItems_Products foreign key (ProductID) references Products(ProductID)
);
go

create table PaymentRecords (
OrderID         int           primary key,
PaymentMethod   nvarchar(50)  not null,
Amount          decimal(18,2) not null,
PaymentStatus   nvarchar(30)  not null default 'Pending',
TransactionDate datetime2     not null default sysdatetime(),
constraint CK_Payment_Amount check (Amount >= 0),
constraint CK_Payment_Status check (PaymentStatus in ('Pending','Completed','Failed','Refunded')),
constraint FK_PaymentRecords_Orders foreign key (OrderID) references Orders(OrderID) on delete cascade
);
go

create view vw_OrderSummary as
select  o.OrderID, o.OrderDate, o.OrderState,
        c.CustomerID, c.FirstName + N' ' + c.LastName as CustomerName, c.Email,
        p.Code as PromoCode, p.DiscountPercentage,
        o.TotalAmount, pay.PaymentMethod, pay.PaymentStatus
from Orders o
join Customer c              on c.CustomerID = o.CustomerID
left join Promotions p       on p.PromotionID = o.PromotionID
left join PaymentRecords pay on pay.OrderID = o.OrderID;
go
