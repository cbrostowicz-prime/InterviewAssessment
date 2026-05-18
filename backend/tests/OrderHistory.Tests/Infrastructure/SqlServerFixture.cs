using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OrderHistory.Api.Data;

namespace OrderHistory.Tests.Infrastructure;

public class SqlServerFixture : IAsyncLifetime
{
    private const string MasterConnectionString =
        "Server=localhost,11433;Database=master;User Id=sa;Password=Local_Dev_Pass1!;TrustServerCertificate=True;Encrypt=False;";

    public string DatabaseName { get; } = $"OrderHistory_Test_{Guid.NewGuid():N}";
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        try
        {
            await using var conn = new SqlConnection(MasterConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE [{DatabaseName}];";
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                "Could not create test database. Is `docker compose up -d` running and SQL Server healthy?",
                ex);
        }

        ConnectionString =
            $"Server=localhost,11433;Database={DatabaseName};User Id=sa;Password=Local_Dev_Pass1!;TrustServerCertificate=True;Encrypt=False;";

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        SqlConnection.ClearAllPools();
        await using var conn = new SqlConnection(MasterConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            IF DB_ID(N'{DatabaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{DatabaseName}];
            END";
        await cmd.ExecuteNonQueryAsync();
    }

    public OrderDbContext CreateContext(params IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlServer(ConnectionString);
        if (interceptors.Length > 0)
        {
            builder = builder.AddInterceptors(interceptors);
        }
        return new OrderDbContext(builder.Options);
    }
}

[CollectionDefinition("SqlServer")]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
}
