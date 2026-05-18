using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHistory.Api.Dtos;
using OrderHistory.Api.Services;
using OrderHistory.Tests.Infrastructure;

namespace OrderHistory.Tests;

[Collection("SqlServer")]
public class CancelInvalidatesStatsCacheTests(SqlServerFixture fx)
{
    [Fact]
    public async Task CancellingOrder_RemovesCancelledOrderFromSubsequentStats()
    {
        await using var db = fx.CreateContext();
        var user = await db.AddUserAsync("cancel-stats");
        var stats = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));
        var orders = new OrderService(db, stats);

        var placed = await orders.PlaceAsync(
            new PlaceOrderRequest(user.Id, [new PlaceOrderItem("SKU-TEST", 1, 100m)]),
            CancellationToken.None);

        var before = await stats.GetAsync(user.Id, CancellationToken.None);
        before.Should().NotBeNull();
        before!.OrderCount.Should().Be(1);
        before.LifetimeSpend.Should().Be(100m);

        var outcome = await orders.CancelAsync(placed.Id, CancellationToken.None);
        outcome.Should().Be(CancelResult.Cancelled);

        var after = await stats.GetAsync(user.Id, CancellationToken.None);
        after.Should().NotBeNull();
        after!.OrderCount.Should().Be(0, "the cancelled order should no longer count");
        after.LifetimeSpend.Should().Be(0m, "the cancelled order's total should not appear in lifetime spend");
    }
}
