using Microsoft.Data.SqlClient;

namespace Orders.Dal.Migrations;

/// <summary>Виконує всі вбудовані *.sql скрипти за порядком імен (скрипти ідемпотентні).</summary>
public static class DbMigrator
{
    public static async Task ApplyAsync(string connectionString, CancellationToken ct = default)
    {
        var assembly = typeof(DbMigrator).Assembly;
        var scripts = assembly.GetManifestResourceNames()
            .Where(n => n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.Ordinal);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        foreach (var name in scripts)
        {
            await using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(ct);

            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(ct);
        }
    }
}