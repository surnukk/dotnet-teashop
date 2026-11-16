using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Dal.Interfaces;

namespace Orders.Dal.Repositories;

public sealed class ProcessedEventRepository : RepositoryBase, IProcessedEventRepository
{
    public ProcessedEventRepository(SqlConnection connection, Func<SqlTransaction?> tx) : base(connection, tx) { }

    public async Task<bool> TryAddAsync(Guid eventId, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        try
        {
            await Connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO dbo.ProcessedEvents (EventId) VALUES (@EventId)",
                new { EventId = eventId }, Transaction, cancellationToken: ct));
            return true;
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)   // порушення PRIMARY KEY — уже оброблено
        {
            return false;
        }
    }
}