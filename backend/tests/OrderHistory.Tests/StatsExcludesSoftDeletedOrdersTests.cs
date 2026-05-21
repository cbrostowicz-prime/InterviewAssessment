using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHistory.Api.Models;
using OrderHistory.Api.Services;
using OrderHistory.Tests.Infrastructure;

namespace OrderHistory.Tests;

[Collection("SqlServer")]
public class StatsExcludesSoftDeletedOrdersTests(SqlServerFixture fx)
{
    [Fact]
    public async Task Stats_DoNotCount_SoftDeletedOrders()
    {
        await using var db = fx.CreateContext();
        var user = await db.AddUserAsync("stats-soft-delete");
        var t = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        await db.AddOrdersAsync(user.Id,
        [
            (t.AddDays(1), 5000, OrderStatus.Delivered),
            (t.AddDays(2), 7500, OrderStatus.Paid),
            (t.AddDays(3), 2500, OrderStatus.Placed)
        ]);

        db.Orders.AddRange(
            new Order
            {
                UserId = user.Id,
                CreatedAt = t.AddDays(4),
                TotalCents = 9999,
                Status = OrderStatus.Placed,
                DeletedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Order
            {
                UserId = user.Id,
                CreatedAt = t.AddDays(5),
                TotalCents = 12345,
                Status = OrderStatus.Paid,
                DeletedAt = DateTime.UtcNow.AddDays(-1)
            });
        await db.SaveChangesAsync();

        var svc = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));

        var stats = await svc.GetAsync(user.Id, CancellationToken.None);

        stats.Should().NotBeNull();
        stats!.OrderCount.Should().Be(3, "soft-deleted orders must not be counted toward stats");
        stats.LifetimeSpend.Should().Be(150m, "lifetime spend must sum only the three non-deleted orders (50 + 75 + 25)");
    }
}
