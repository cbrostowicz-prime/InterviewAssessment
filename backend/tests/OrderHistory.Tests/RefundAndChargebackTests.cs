using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHistory.Api.Models;
using OrderHistory.Api.Services;
using OrderHistory.Tests.Infrastructure;

namespace OrderHistory.Tests;

[Collection("SqlServer")]
public class RefundAndChargebackTests(SqlServerFixture fx)
{
    [Fact]
    public async Task RefundingPaidOrder_TransitionsToRefunded_AndExcludesFromStats()
    {
        await using var db = fx.CreateContext();
        var user = await db.AddUserAsync("refund-paid");
        var t = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var orders = await db.AddOrdersAsync(user.Id,
        [
            (t.AddDays(1), 5000, OrderStatus.Delivered),
            (t.AddDays(2), 2000, OrderStatus.Paid)
        ]);
        var stats = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));
        var svc = new OrderService(db, stats);

        // Warm the stats cache before the refund so we also exercise invalidation.
        var before = await stats.GetAsync(user.Id, CancellationToken.None);
        before!.OrderCount.Should().Be(2);
        before.LifetimeSpend.Should().Be(70m);

        var result = await svc.RefundAsync(orders[1].Id, CancellationToken.None);

        result.Should().Be(RefundResult.Refunded);
        var stored = await db.Orders.FindAsync(orders[1].Id);
        stored!.Status.Should().Be(OrderStatus.Refunded);

        var after = await stats.GetAsync(user.Id, CancellationToken.None);
        after!.OrderCount.Should().Be(1);
        after.LifetimeSpend.Should().Be(50m);
    }

    [Fact]
    public async Task ChargingBackPaidOrder_TransitionsToChargedBack_AndExcludesFromStats()
    {
        await using var db = fx.CreateContext();
        var user = await db.AddUserAsync("chargeback-paid");
        var t = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var orders = await db.AddOrdersAsync(user.Id,
        [
            (t.AddDays(1), 5000, OrderStatus.Delivered),
            (t.AddDays(2), 2000, OrderStatus.Paid)
        ]);
        var stats = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));
        var svc = new OrderService(db, stats);

        var before = await stats.GetAsync(user.Id, CancellationToken.None);
        before!.OrderCount.Should().Be(2);
        before.LifetimeSpend.Should().Be(70m);

        var result = await svc.ChargebackAsync(orders[1].Id, CancellationToken.None);

        result.Should().Be(ChargebackResult.ChargedBack);
        var stored = await db.Orders.FindAsync(orders[1].Id);
        // ChargedBack is the next enum value after Refunded (= 5). The candidate
        // adds it to OrderStatus as part of this work item.
        ((int)stored!.Status).Should().Be(6);

        var after = await stats.GetAsync(user.Id, CancellationToken.None);
        after!.OrderCount.Should().Be(1);
        after.LifetimeSpend.Should().Be(50m);
    }
}
