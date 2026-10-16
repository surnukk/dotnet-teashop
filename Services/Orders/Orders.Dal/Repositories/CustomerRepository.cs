using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Data.SqlClient;
using Orders.Dal.Interfaces;
using Orders.Domain.Models;

namespace Orders.Dal.Repositories;

public sealed class CustomerRepository : RepositoryBase, ICustomerRepository
{
    private const string Columns = "CustomerID, FirstName, LastName, Email, PhoneNumber, DeliveryAddress, IsActive";

    public CustomerRepository(SqlConnection c, Func<SqlTransaction?> tx) : base(c, tx) { }

    public async Task<Customer?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = $"SELECT {Columns} FROM dbo.Customer WHERE CustomerID = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = $"SELECT {Columns} FROM dbo.Customer WHERE Email = @Email AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = email;

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql =
            $"SELECT {Columns} FROM dbo.Customer WHERE IsDeleted = 0 ORDER BY LastName, FirstName";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);

        var result = new List<Customer>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            result.Add(Map(reader));
        return result;
    }

    public async Task<long> AddAsync(Customer customer, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.Customer (FirstName, LastName, Email, PhoneNumber, DeliveryAddress, IsActive)
            OUTPUT INSERTED.CustomerID
            VALUES (@FirstName, @LastName, @Email, @PhoneNumber, @DeliveryAddress, @IsActive)";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        AddWriteParameters(command, customer);

        return await ExecuteAsync(async () => (long)(await command.ExecuteScalarAsync(ct))!);
    }

    public async Task<bool> UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE dbo.Customer
            SET FirstName = @FirstName, LastName = @LastName, Email = @Email,
                PhoneNumber = @PhoneNumber, DeliveryAddress = @DeliveryAddress,
                IsActive = @IsActive, UpdatedAt = SYSUTCDATETIME()
            WHERE CustomerID = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        AddWriteParameters(command, customer);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = customer.CustomerID;

        return await ExecuteAsync(async () => await command.ExecuteNonQueryAsync(ct) > 0);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE dbo.Customer
            SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME()
            WHERE CustomerID = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;

        return await ExecuteAsync(async () => await command.ExecuteNonQueryAsync(ct) > 0);
    }

    private static void AddWriteParameters(SqlCommand command, Customer c)
    {
        command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 50).Value = c.FirstName;
        command.Parameters.Add("@LastName", SqlDbType.NVarChar, 50).Value = c.LastName;
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = c.Email;
        command.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 20).Value =
            (object?)c.PhoneNumber ?? DBNull.Value;
        command.Parameters.Add("@DeliveryAddress", SqlDbType.NVarChar, 255).Value =
            (object?)c.DeliveryAddress ?? DBNull.Value;
        command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = c.IsActive;
    }

    private static Customer Map(SqlDataReader r) => new()
    {
        CustomerID = r.GetInt64(0),
        FirstName = r.GetString(1),
        LastName = r.GetString(2),
        Email = r.GetString(3),
        PhoneNumber = r.IsDBNull(4) ? null : r.GetString(4),
        DeliveryAddress = r.IsDBNull(5) ? null : r.GetString(5),
        IsActive = r.GetBoolean(6)
    };
}