using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Dal.Interfaces;

namespace Orders.Dal.Repositories;

public abstract class BaseDapperRepository<T> : RepositoryBase, IGenericRepository<T>
    where T : class
{
    private readonly string _table;
    private readonly string _keyColumn;
    private readonly string _columns;
    private readonly bool _softDelete;

    protected BaseDapperRepository(SqlConnection connection, Func<SqlTransaction?> tx,
        string table, string keyColumn, string columns, bool softDelete)
        : base(connection, tx)
    {
        _table = table;
        _keyColumn = keyColumn;
        _columns = columns;
        _softDelete = softDelete;
    }

    protected abstract string InsertSql { get; }
    protected abstract string UpdateSql { get; }

    private string AndActive => _softDelete ? " AND IsDeleted = 0" : "";

    protected CommandDefinition Command(string sql, object? parameters, CancellationToken ct)
        => new(sql, parameters, Transaction, cancellationToken: ct);

    public async Task<T?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var sql = $"SELECT {_columns} FROM {_table} WHERE {_keyColumn} = @Id{AndActive}";
        await EnsureOpenAsync(ct);
        return await Connection.QuerySingleOrDefaultAsync<T>(Command(sql, new { Id = id }, ct));
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        var where = _softDelete ? " WHERE IsDeleted = 0" : "";
        var sql = $"SELECT {_columns} FROM {_table}{where}";
        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<T>(Command(sql, null, ct));
        return rows.AsList();
    }

    public async Task<long> AddAsync(T entity, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        return await Connection.ExecuteScalarAsync<long>(Command(InsertSql, entity, ct));
    }

    public async Task<bool> UpdateAsync(T entity, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        return await Connection.ExecuteAsync(Command(UpdateSql, entity, ct)) > 0;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        var sql = _softDelete
            ? $"UPDATE {_table} SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME() WHERE {_keyColumn} = @Id AND IsDeleted = 0"
            : $"DELETE FROM {_table} WHERE {_keyColumn} = @Id";
        await EnsureOpenAsync(ct);
        return await Connection.ExecuteAsync(Command(sql, new { Id = id }, ct)) > 0;
    }
}