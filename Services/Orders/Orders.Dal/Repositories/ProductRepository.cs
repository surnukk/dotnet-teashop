using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Dal.Interfaces;
using Orders.Domain.Models;
using System.Data;

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

    // ---- Лаб. 08: оновлення локальної копії довідника подіями Каталогу ----
    private static DynamicParameters Args(long productId, DateTime at)
    {
        var p = new DynamicParameters();
        p.Add("ProductId", productId, DbType.Int64);
        p.Add("At", at, DbType.DateTime2);      // datetime2 — без втрати точності порівняння
        return p;
    }

    /// <summary>Створення: додаємо рядок, якщо його ще немає.</summary>
    public async Task<bool> CreateSnapshotAsync(long productId, string name, decimal price, DateTime at, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var p = Args(productId, at);
        p.Add("Name", name, DbType.String, size: 200);
        p.Add("Price", price, DbType.Decimal, precision: 18, scale: 2);

        const string sql = @"
            IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductID = @ProductId)
                INSERT INTO dbo.Products (ProductID, ProductName, Price, NameUpdatedAt, PriceUpdatedAt)
                VALUES (@ProductId, @Name, @Price, @At, @At);";
        return await Connection.ExecuteAsync(Command(sql, p, ct)) > 0;
    }

    /// <summary>Зміна ціни: застосовуємо, лише якщо подія новіша за вже застосовану. Немає рядка — створюємо.</summary>
    public async Task<bool> UpdatePriceSnapshotAsync(long productId, string name, decimal price, DateTime at, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var p = Args(productId, at);
        p.Add("Name", name, DbType.String, size: 200);
        p.Add("Price", price, DbType.Decimal, precision: 18, scale: 2);

        const string sql = @"
            UPDATE dbo.Products SET Price = @Price, PriceUpdatedAt = @At
            WHERE ProductID = @ProductId AND (PriceUpdatedAt IS NULL OR PriceUpdatedAt < @At);

            IF @@ROWCOUNT = 0 AND NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductID = @ProductId)
                INSERT INTO dbo.Products (ProductID, ProductName, Price, NameUpdatedAt, PriceUpdatedAt)
                VALUES (@ProductId, @Name, @Price, @At, @At);";
        return await Connection.ExecuteAsync(Command(sql, p, ct)) > 0;
    }

    /// <summary>Перейменування: лише якщо подія новіша за вже застосовану зміну назви.</summary>
    public async Task<bool> UpdateNameSnapshotAsync(long productId, string name, DateTime at, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var p = Args(productId, at);
        p.Add("Name", name, DbType.String, size: 200);

        const string sql = @"
            UPDATE dbo.Products SET ProductName = @Name, NameUpdatedAt = @At
            WHERE ProductID = @ProductId AND (NameUpdatedAt IS NULL OR NameUpdatedAt < @At);";
        return await Connection.ExecuteAsync(Command(sql, p, ct)) > 0;
    }

    /// <summary>Видалення: рядок, на який посилаються OrderItems, лишається як історія замовлень.</summary>
    public async Task<bool> DeleteSnapshotAsync(long productId, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var p = new DynamicParameters();
        p.Add("ProductId", productId, DbType.Int64);

        const string sql = @"
            DELETE FROM dbo.Products
            WHERE ProductID = @ProductId
              AND NOT EXISTS (SELECT 1 FROM dbo.OrderItems WHERE ProductID = @ProductId);";
        return await Connection.ExecuteAsync(Command(sql, p, ct)) > 0;
    }
}