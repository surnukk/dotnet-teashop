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

public sealed class PromotionRepository : BaseDapperRepository<Promotion>, IPromotionRepository
{
    private const string Columns =
        "PromotionID, Code, DiscountPercentage, StartDate, EndDate, IsActive";

    public PromotionRepository(SqlConnection c, Func<SqlTransaction?> tx)
        : base(c, tx, "dbo.Promotions", "PromotionID", Columns, softDelete: true) { }

    protected override string InsertSql => @"
        INSERT INTO dbo.Promotions (Code, DiscountPercentage, StartDate, EndDate, IsActive)
        OUTPUT INSERTED.PromotionID
        VALUES (@Code, @DiscountPercentage, @StartDate, @EndDate, @IsActive)";

    protected override string UpdateSql => @"
        UPDATE dbo.Promotions
        SET Code = @Code, DiscountPercentage = @DiscountPercentage,
            StartDate = @StartDate, EndDate = @EndDate, IsActive = @IsActive,
            UpdatedAt = SYSUTCDATETIME()
        WHERE PromotionID = @PromotionID AND IsDeleted = 0";

    public async Task<Promotion?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        return await Connection.QuerySingleOrDefaultAsync<Promotion>(Command(
            $"SELECT {Columns} FROM dbo.Promotions WHERE Code = @Code AND IsDeleted = 0",
            new { Code = code }, ct));
    }
}