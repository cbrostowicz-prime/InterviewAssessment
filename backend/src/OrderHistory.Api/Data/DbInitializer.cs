using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace OrderHistory.Api.Data;

public static class DbInitializer
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(90);

    public static async Task InitializeAsync(IServiceProvider services, ILogger logger, CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        await WaitForServerAsync(db, logger, ct);

        logger.LogInformation("Applying EF migrations…");
        await db.Database.MigrateAsync(ct);

        if (!await db.Users.AnyAsync(ct))
        {
            logger.LogInformation("Database is empty; running seed.");
            var (users, orders, items) = DbSeeder.Build();
            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(ct);
                await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Users] ON;", ct);
                db.Users.AddRange(users);
                await db.SaveChangesAsync(ct);
                await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Users] OFF;", ct);

                await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Orders] ON;", ct);
                db.Orders.AddRange(orders);
                await db.SaveChangesAsync(ct);
                await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Orders] OFF;", ct);

                await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [OrderItems] ON;", ct);
                db.OrderItems.AddRange(items);
                await db.SaveChangesAsync(ct);
                await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [OrderItems] OFF;", ct);

                await tx.CommitAsync(ct);
            });
            logger.LogInformation("Seed complete: {Users} users, {Orders} orders, {Items} items.",
                users.Count, orders.Count, items.Count);
        }
        else
        {
            logger.LogInformation("Database already populated; skipping seed.");
        }
    }

    private static async Task WaitForServerAsync(OrderDbContext db, ILogger logger, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.Add(ConnectTimeout);
        var builder = new SqlConnectionStringBuilder(db.Database.GetConnectionString())
        {
            InitialCatalog = "master",
            ConnectTimeout = 5
        };
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                await using var conn = new SqlConnection(builder.ConnectionString);
                await conn.OpenAsync(ct);
                return;
            }
            catch (SqlException)
            {
                if (DateTime.UtcNow >= deadline)
                {
                    throw new InvalidOperationException(
                        $"SQL Server did not become reachable within {ConnectTimeout.TotalSeconds:N0}s.");
                }
                logger.LogInformation("Retrying database connect… (attempt {Attempt})", attempt);
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
    }
}
