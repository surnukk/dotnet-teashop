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

public sealed class ProductRepository : BaseDapperRepository<Product>, IProductRepository
{
    private const string Columns = "ProductID, ProductName, Price";

    public ProductRepository(SqlConnection c, Func<SqlTransaction?> tx)
        : base(c, tx, "dbo.Products", "ProductID", Columns, softDelete: false) { }

    protected override string InsertSql => @"
        INSERT INTO dbo.Products (ProductID, ProductName, Price)
        OUTPUT INSERTED.ProductID
        VALUES (@ProductID, @ProductName, @Price)";

    protected override string UpdateSql => @"
        UPDATE dbo.Products SET ProductName = @ProductName, Price = @Price
        WHERE ProductID = @ProductID";

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IEnumerable<long> ids, CancellationToken ct = default)
    {
        var idList = ids.Distinct().ToArray();
        if (idList.Length == 0) return Array.Empty<Product>();

        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<Product>(Command(
            $"SELECT {Columns} FROM dbo.Products WHERE ProductID IN @Ids",
            new { Ids = idList }, ct));
        return rows.AsList();
    }
}