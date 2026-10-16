using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Data.SqlClient;
using Orders.Domain.Exceptions;

namespace Orders.Dal.Repositories;

public abstract class RepositoryBase
{
    protected readonly SqlConnection Connection;
    private readonly Func<SqlTransaction?> _currentTransaction;

    protected RepositoryBase(SqlConnection connection,
                             Func<SqlTransaction?> currentTransaction)
    {
        Connection = connection;
        _currentTransaction = currentTransaction;
    }

    protected SqlTransaction? Transaction => _currentTransaction();

    protected async Task EnsureOpenAsync(CancellationToken ct)
    {
        if (Connection.State != ConnectionState.Open)
            await Connection.OpenAsync(ct);
    }

    protected async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action)
    {
        try
        {
            return await action();
        }
        catch (SqlException ex) when (ex.Number == 50001)
        {
            throw new NotFoundException("Requested resource was not found");
        }
        catch (SqlException ex) when (ex.Number is >= 50002 and <= 50010)
        {
            throw new BusinessConflictException(ex.Message);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new BusinessConflictException("Record with the same key already exists");
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            throw new BusinessConflictException("Operation violates a reference or check constraint");
        }
    }

    protected Task ExecuteAsync(Func<Task> action)
        => ExecuteAsync<bool>(async () => { await action(); return true; });
}

