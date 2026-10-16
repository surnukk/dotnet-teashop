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
    private const string Columns = "OrderID, PaymentMethod, Amount, PaymentStatus, TransactionDate";

    public PaymentRepository(SqlConnection c, Func<SqlTransaction?> tx)
        : base(c, tx, "dbo.PaymentRecords", "OrderID", Columns, softDelete: false) { }

    protected override string InsertSql => @"
        INSERT INTO dbo.PaymentRecords (OrderID, PaymentMethod, Amount, PaymentStatus, TransactionDate)
        OUTPUT INSERTED.OrderID
        VALUES (@OrderID, @PaymentMethod, @Amount, @PaymentStatus, @TransactionDate)";

    protected override string UpdateSql => @"
        UPDATE dbo.PaymentRecords
        SET PaymentMethod = @PaymentMethod, Amount = @Amount,
            PaymentStatus = @PaymentStatus, TransactionDate = @TransactionDate
        WHERE OrderID = @OrderID";

    public async Task<PaymentRecord?> GetByOrderIdAsync(long orderId, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        return await Connection.QuerySingleOrDefaultAsync<PaymentRecord>(Command(
            $"SELECT {Columns} FROM dbo.PaymentRecords WHERE OrderID = @OrderId",
            new { OrderId = orderId }, ct));
    }
}