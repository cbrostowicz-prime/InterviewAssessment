using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHistory.Api.Models;
using OrderHistory.Api.Services;
using OrderHistory.Tests.Infrastructure;

namespace OrderHistory.Tests;

[Collection("SqlServer")]
public class OrderSearchFilterTests(SqlServerFixture fx)
{
    [Fact]
    public async Task Filter_ByStatus_ReturnsOnlyMatching()
    {
        await using var db = fx.CreateContext();
        var user = await db.AddUserAsync("filter-status");
        var stats = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));
        var orders = new OrderService(db, stats);

        var baseTime = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        await db.AddOrdersAsync(user.Id, new (DateTime, int, OrderStatus)[]
        {
            (baseTime.AddDays(0), 1000, OrderStatus.Placed),
            (baseTime.AddDays(1), 1100, OrderStatus.Paid),
            (baseTime.AddDays(2), 1200, OrderStatus.Shipped),
            (baseTime.AddDays(3), 1300, OrderStatus.Delivered),
            (baseTime.AddDays(4), 1400, OrderStatus.Cancelled),
            (baseTime.AddDays(5), 1500, OrderStatus.Refunded),
        });

        var page = await orders.ListByUserAsync(
            user.Id,
            page: 1,
            pageSize: 100,
            includeDeleted: false,
            CancellationToken.None,
            status: new[] { OrderStatus.Placed, OrderStatus.Paid });

        page.Total.Should().Be(2, "only Placed and Paid orders should be counted");
        page.Items.Should().OnlyContain(o =>
            o.Status == OrderStatus.Placed || o.Status == OrderStatus.Paid);
    }

    [Fact]
    public async Task Filter_ByDateRange_ReturnsOnlyWithinRange()
    {
        await using var db = fx.CreateContext();
        var user = await db.AddUserAsync("filter-dates");
        var stats = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));
        var orders = new OrderService(db, stats);

        var baseTime = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        await db.AddOrdersAsync(user.Id, Enumerable.Range(0, 10).Select(i =>
            (baseTime.AddDays(i), 1000 + i * 50, OrderStatus.Delivered)));

        var from = baseTime.AddDays(3);
        var to = baseTime.AddDays(6);

        var page = await orders.ListByUserAsync(
            user.Id,
            page: 1,
            pageSize: 100,
            includeDeleted: false,
            CancellationToken.None,
            fromDate: from,
            toDate: to);

        page.Items.Should().OnlyContain(o => o.CreatedAt >= from && o.CreatedAt <= to);
        page.Total.Should().Be(4, "days 3, 4, 5, 6 fall in the inclusive window");
    }

    [Fact]
    public async Task Filter_CombinesStatusAndDate()
    {
        await using var db = fx.CreateContext();
        var user = await db.AddUserAsync("filter-combo");
        var stats = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));
        var orders = new OrderService(db, stats);

        var baseTime = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);
        await db.AddOrdersAsync(user.Id, new (DateTime, int, OrderStatus)[]
        {
            (baseTime.AddDays(0), 1000, OrderStatus.Placed),    // in window, matching status
            (baseTime.AddDays(1), 1100, OrderStatus.Shipped),   // in window, wrong status
            (baseTime.AddDays(2), 1200, OrderStatus.Placed),    // in window, matching status
            (baseTime.AddDays(10), 1300, OrderStatus.Placed),   // out of window, matching status
            (baseTime.AddDays(11), 1400, OrderStatus.Shipped),  // out of window, wrong status
        });

        var from = baseTime;
        var to = baseTime.AddDays(5);

        var page = await orders.ListByUserAsync(
            user.Id,
            page: 1,
            pageSize: 100,
            includeDeleted: false,
            CancellationToken.None,
            status: new[] { OrderStatus.Placed },
            fromDate: from,
            toDate: to);

        page.Total.Should().Be(2, "only Placed orders inside the date window should be counted (AND semantics)");
        page.Items.Should().OnlyContain(o =>
            o.Status == OrderStatus.Placed && o.CreatedAt >= from && o.CreatedAt <= to);
    }
}
