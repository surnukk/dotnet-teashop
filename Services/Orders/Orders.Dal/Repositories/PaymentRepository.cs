using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Dal.Interfaces;
using Orders.Domain.Models;

namespace Orders.Dal.Repositories;

public sealed class PaymentRepository : BaseDapperRepository<PaymentRecord>, IPaymentRepository
{
    private const string Columns = "PaymentID, OrderID, Amount, PaymentMethod, PaymentStatus, TransactionDate";

    public PaymentRepository(SqlConnection c, Func<SqlTransaction?> tx)
        : base(c, tx, "dbo.Payments", "PaymentID", Columns, softDelete: false) { }

    protected override string InsertSql => @"
        INSERT INTO dbo.Payments (OrderID, Amount, PaymentMethod, PaymentStatus, TransactionDate)
        OUTPUT INSERTED.PaymentID
        VALUES (@OrderID, @Amount, @PaymentMethod, @PaymentStatus, @TransactionDate)";

    protected override string UpdateSql => @"
        UPDATE dbo.Payments
        SET OrderID = @OrderID, Amount = @Amount, PaymentMethod = @PaymentMethod,
            PaymentStatus = @PaymentStatus, TransactionDate = @TransactionDate
        WHERE PaymentID = @PaymentID";

    public async Task<PaymentRecord?> GetByOrderIdAsync(long orderId, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        return await Connection.QuerySingleOrDefaultAsync<PaymentRecord>(Command(
            $"SELECT {Columns} FROM dbo.Payments WHERE OrderID = @OrderId",
            new { OrderId = orderId }, ct));
    }
}