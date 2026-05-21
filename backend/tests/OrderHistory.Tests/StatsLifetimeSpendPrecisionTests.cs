using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHistory.Api.Models;
using OrderHistory.Api.Services;
using OrderHistory.Tests.Infrastructure;

namespace OrderHistory.Tests;

[Collection("SqlServer")]
public class StatsLifetimeSpendPrecisionTests(SqlServerFixture fx)
{
    [Fact]
    public async Task LifetimeSpend_PreservesCentsAcrossOrders()
    {
        await using var db = fx.CreateContext();
        var user = await db.AddUserAsync("stats-precision");
        var t = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await db.AddOrdersAsync(user.Id,
        [
            (t.AddDays(1), 1999, OrderStatus.Delivered),
            (t.AddDays(2), 2599, OrderStatus.Delivered),
            (t.AddDays(3), 4999, OrderStatus.Delivered)
        ]);
        var svc = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));

        var stats = await svc.GetAsync(user.Id, CancellationToken.None);

        stats.Should().NotBeNull();
        stats!.OrderCount.Should().Be(3);
        stats.LifetimeSpend.Should().Be(95.97m, "$19.99 + $25.99 + $49.99 = $95.97, not $93.00");
    }
}
