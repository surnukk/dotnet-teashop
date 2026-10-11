use EastLeaf_OrderDB;
go

create or alter procedure dbo.usp_CreateCustomer
@FirstName       nvarchar(50),
@LastName        nvarchar(50),
@Email           nvarchar(100),
@PhoneNumber     nvarchar(20)  = null,
@DeliveryAddress nvarchar(255) = null,
@NewCustomerID   bigint output
as
begin
    set nocount on;
    if exists (select 1 from dbo.Customer where Email = @Email)
        throw 50006, 'Customer with this email already exists', 1;

    insert into dbo.Customer (FirstName, LastName, Email, PhoneNumber, DeliveryAddress, IsActive)
    values (@FirstName, @LastName, @Email, @PhoneNumber, @DeliveryAddress, 1);

    set @NewCustomerID = scope_identity();
end;
go

create or alter procedure dbo.usp_CreatePromotion
@Code               nvarchar(50),
@DiscountPercentage decimal(5,2),
@StartDate          datetime2,
@EndDate            datetime2,
@NewPromotionID     bigint output
as
begin
    set nocount on;
    if @DiscountPercentage <= 0 or @DiscountPercentage > 100
        throw 50008, 'Discount percentage must be between 0 and 100', 1;

    if @EndDate <= @StartDate
        throw 50009, 'End date must be greater than start date', 1;

    if exists (select 1 from dbo.Promotions where Code = @Code)
        throw 50010, 'Promotion code already exists', 1;

    insert into dbo.Promotions (Code, DiscountPercentage, StartDate, EndDate, IsActive)
    values (@Code, @DiscountPercentage, @StartDate, @EndDate, 1);

    set @NewPromotionID = scope_identity();
end;
go

create or alter procedure dbo.usp_ConfirmOrder
@OrderID bigint
as
begin
    set nocount on;
    set xact_abort on;
    begin transaction;

    declare @CurrentState nvarchar(30);
    select @CurrentState = OrderState from dbo.Orders with (updlock) where OrderID = @OrderID and IsDeleted = 0;

    if @CurrentState is null
        throw 50001, 'Order not found', 1;

    if @CurrentState not in ('Submitted', 'AwaitingValidation')
        throw 50002, 'Stock can be confirmed only for a new order', 1;

    update dbo.Orders
    set OrderState = 'StockConfirmed', UpdatedAt = sysutcdatetime()
    where OrderID = @OrderID;

    commit transaction;
end;
go

create or alter procedure dbo.usp_MarkOrderAsPaid
@OrderID       bigint,
@PaymentMethod nvarchar(50),
@Amount        decimal(18,2)
as
begin
    set nocount on;
    set xact_abort on;
    begin transaction;

    declare @CurrentState nvarchar(30);
    declare @OrderTotal   decimal(18,2);
    select @CurrentState = OrderState, @OrderTotal = TotalAmount from dbo.Orders with (updlock) where OrderID = @OrderID and IsDeleted = 0;

    if @CurrentState is null
        throw 50001, 'Order not found', 1;

    if @CurrentState <> 'StockConfirmed'
        throw 50003, 'Order must be in StockConfirmed status before payment', 1;

    if @Amount < @OrderTotal
        throw 50004, 'Payment amount is less than the total order amount', 1;

    if exists (select 1 from dbo.PaymentRecords where OrderID = @OrderID)
        update dbo.PaymentRecords
        set PaymentMethod = @PaymentMethod, Amount = @Amount,
            PaymentStatus = 'Completed', TransactionDate = sysutcdatetime()
        where OrderID = @OrderID;
    else
        insert into dbo.PaymentRecords (OrderID, PaymentMethod, Amount, PaymentStatus, TransactionDate)
        values (@OrderID, @PaymentMethod, @Amount, 'Completed', sysutcdatetime());

    update dbo.Orders
    set OrderState = 'Paid', UpdatedAt = sysutcdatetime()
    where OrderID = @OrderID;

    commit transaction;
end;
go

create or alter procedure dbo.usp_ShipOrder
@OrderID bigint
as
begin
    set nocount on;
    set xact_abort on;
    begin transaction;

    declare @CurrentState nvarchar(30);
    select @CurrentState = OrderState from dbo.Orders with (updlock) where OrderID = @OrderID and IsDeleted = 0;

    if @CurrentState is null
        throw 50001, 'Order not found', 1;

    if @CurrentState <> 'Paid'
        throw 50005, 'Cannot ship an unpaid order. Order must be Paid first', 1;

    update dbo.Orders
    set OrderState = 'Shipped', UpdatedAt = sysutcdatetime()
    where OrderID = @OrderID;

    commit transaction;
end;
go

create or alter procedure dbo.usp_CancelOrder
    @OrderID bigint
as
begin
    set nocount on;
    set xact_abort on;
    begin transaction;

    declare @CurrentState nvarchar(30);
    select @CurrentState = OrderState from dbo.Orders with (updlock) where OrderID = @OrderID and IsDeleted = 0;

    if @CurrentState is null
        throw 50001, 'Order not found', 1;

    if @CurrentState not in ('Submitted', 'AwaitingValidation', 'StockConfirmed')
        throw 50007, 'Order can be cancelled only before payment', 1;

    update dbo.Orders
    set OrderState = 'Cancelled', UpdatedAt = sysutcdatetime()
    where OrderID = @OrderID;

    commit transaction;
end;
go