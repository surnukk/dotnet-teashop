using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Orders.Dal;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SqlConnection _connection;

    public UnitOfWork(string connectionString)
    {
        _connection = new SqlConnection(connectionString);
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}