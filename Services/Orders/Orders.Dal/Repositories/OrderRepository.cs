using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Dal.Interfaces;
using Orders.Domain.Models;

namespace Orders.Dal.Repositories;

public sealed class OrderRepository : BaseDapperRepository<Order>, IOrderRepository
{
    private const string Columns = "OrderID, CustomerID, PromotionID, OrderDate, OrderState, TotalAmount";

    public OrderRepository(SqlConnection c, Func<SqlTransaction?> tx)
        : base(c, tx, "dbo.Orders", "OrderID", Columns, softDelete: true) { }

    protected override string InsertSql => @"
        INSERT INTO dbo.Orders (CustomerID, PromotionID, OrderState, TotalAmount)
        OUTPUT INSERTED.OrderID
        VALUES (@CustomerID, @PromotionID, @OrderState, @TotalAmount)";

    protected override string UpdateSql => @"
        UPDATE dbo.Orders
        SET CustomerID = @CustomerID, PromotionID = @PromotionID,
            TotalAmount = @TotalAmount, UpdatedAt = SYSUTCDATETIME()
        WHERE OrderID = @OrderID AND IsDeleted = 0";

    public async Task<Order?> GetWithItemsAsync(long orderId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT o.OrderID, o.CustomerID, o.PromotionID, o.OrderDate, o.OrderState, o.TotalAmount,
                   i.ProductID, i.ProductName, i.UnitPrice, i.Units
            FROM dbo.Orders o
            LEFT JOIN dbo.OrderItems i ON i.OrderID = o.OrderID
            WHERE o.OrderID = @OrderId AND o.IsDeleted = 0";

        await EnsureOpenAsync(ct);

        Order? result = null;
        await Connection.QueryAsync<Order, OrderItem?, Order>(
            Command(sql, new { OrderId = orderId }, ct),
            (order, item) =>
            {
                result ??= order;
                if (item is not null)
                {
                    item.OrderID = result.OrderID;
                    result.Items.Add(item);
                }
                return result;
            },
            splitOn: "ProductID"); 

        return result;
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(
        long customerId, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<Order>(Command(
            $"SELECT {Columns} FROM dbo.Orders WHERE CustomerID = @CustomerId AND IsDeleted = 0 ORDER BY OrderDate DESC",
            new { CustomerId = customerId }, ct));
        return rows.AsList();
    }

    public async Task AddItemAsync(OrderItem item, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        await Connection.ExecuteAsync(Command(@"
            INSERT INTO dbo.OrderItems (OrderID, ProductID, ProductName, UnitPrice, Units)
            VALUES (@OrderID, @ProductID, @ProductName, @UnitPrice, @Units)", item, ct));
    }

    public Task ConfirmAsync(long orderId, CancellationToken ct = default)
        => CallProcedureAsync("dbo.usp_ConfirmOrder", new { OrderID = orderId }, ct);

    public Task MarkAsPaidAsync(long orderId, string paymentMethod, decimal amount,
        CancellationToken ct = default) => CallProcedureAsync("dbo.usp_MarkOrderAsPaid",
            new { OrderID = orderId, PaymentMethod = paymentMethod, Amount = amount }, ct);

    public Task ShipAsync(long orderId, CancellationToken ct = default) => CallProcedureAsync("dbo.usp_ShipOrder", new { OrderID = orderId }, ct);

    public Task CancelAsync(long orderId, CancellationToken ct = default) => CallProcedureAsync("dbo.usp_CancelOrder", new { OrderID = orderId }, ct);

    private async Task CallProcedureAsync(string procedure, object parameters, CancellationToken ct)
    {
        await EnsureOpenAsync(ct);
        await Connection.ExecuteAsync(new CommandDefinition(
            procedure, parameters, Transaction,
            commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }
}
