using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHistory.Api.Models;
using OrderHistory.Api.Services;
using OrderHistory.Tests.Infrastructure;

namespace OrderHistory.Tests;

[Collection("SqlServer")]
public class UserStatsServiceTests(SqlServerFixture fx)
{
    [Fact]
    public async Task LifetimeSpend_ExcludesCancelledAndRefunded()
    {
        await using var db = fx.CreateContext();
        var user = await db.AddUserAsync("stats-exclude");
        var t = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await db.AddOrdersAsync(user.Id,
        [
            (t.AddDays(1), 5000, OrderStatus.Delivered),
            (t.AddDays(2), 7500, OrderStatus.Delivered),
            (t.AddDays(3), 1000, OrderStatus.Cancelled),
            (t.AddDays(4), 2000, OrderStatus.Refunded)
        ]);
        var svc = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));

        var stats = await svc.GetAsync(user.Id, CancellationToken.None);

        stats.Should().NotBeNull();
        stats!.OrderCount.Should().Be(2);
        stats.LifetimeSpend.Should().Be(125m);
    }

    [Fact]
    public async Task UnknownUser_ReturnsNull()
    {
        await using var db = fx.CreateContext();
        var svc = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));

        var stats = await svc.GetAsync(999_999, CancellationToken.None);

        stats.Should().BeNull();
    }

    [Fact]
    public async Task PlaceOrder_InvalidatesCache_SubsequentReadReflectsNewOrder()
    {
        await using var db = fx.CreateContext();
        var user = await db.AddUserAsync("stats-invalidation");
        var stats = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));
        var orderSvc = new OrderService(db, stats);

        var initial = await stats.GetAsync(user.Id, CancellationToken.None);
        initial!.LifetimeSpend.Should().Be(0m);

        await orderSvc.PlaceAsync(
            new Api.Dtos.PlaceOrderRequest(user.Id,
            [
                new Api.Dtos.PlaceOrderItem("SKU-X", 1, 25.00m)
            ]),
            CancellationToken.None);

        var after = await stats.GetAsync(user.Id, CancellationToken.None);
        after!.LifetimeSpend.Should().Be(25m);
    }
}
