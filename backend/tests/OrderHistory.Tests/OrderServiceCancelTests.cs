using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHistory.Api.Models;
using OrderHistory.Api.Services;
using OrderHistory.Tests.Infrastructure;

namespace OrderHistory.Tests;

[Collection("SqlServer")]
public class OrderServiceCancelTests(SqlServerFixture fx)
{
    [Fact]
    public async Task CancelPlacedOrder_TransitionsToCancelled()
    {
        await using var db = fx.CreateContext();
        var user = await db.AddUserAsync("cancel-placed");
        var orders = await db.AddOrdersAsync(user.Id,
        [
            (new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 5000, OrderStatus.Placed)
        ]);
        var stats = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));
        var svc = new OrderService(db, stats);

        var result = await svc.CancelAsync(orders[0].Id, CancellationToken.None);

        result.Should().Be(CancelResult.Cancelled);
        var stored = await db.Orders.FindAsync(orders[0].Id);
        stored!.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task CancelShippedOrder_ReturnsNotCancellable()
    {
        await using var db = fx.CreateContext();
        var user = await db.AddUserAsync("cancel-shipped");
        var orders = await db.AddOrdersAsync(user.Id,
        [
            (new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc), 5000, OrderStatus.Shipped)
        ]);
        var stats = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));
        var svc = new OrderService(db, stats);

        var result = await svc.CancelAsync(orders[0].Id, CancellationToken.None);

        result.Should().Be(CancelResult.NotCancellable);
    }

    [Fact]
    public async Task CancelMissingOrder_ReturnsNotFound()
    {
        await using var db = fx.CreateContext();
        var stats = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));
        var svc = new OrderService(db, stats);

        var result = await svc.CancelAsync(999_999, CancellationToken.None);

        result.Should().Be(CancelResult.NotFound);
    }
}
