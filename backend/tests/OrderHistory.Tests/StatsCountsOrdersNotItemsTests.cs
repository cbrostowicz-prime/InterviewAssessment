using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHistory.Api.Models;
using OrderHistory.Api.Services;
using OrderHistory.Tests.Infrastructure;

namespace OrderHistory.Tests;

[Collection("SqlServer")]
public class StatsCountsOrdersNotItemsTests(SqlServerFixture fx)
{
    [Fact]
    public async Task OrderCount_ReflectsOrders_NotLineItems()
    {
        await using var db = fx.CreateContext();
        var user = await db.AddUserAsync("stats-fanout");
        var t = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var k = 0; k < 3; k++)
        {
            var order = new Order
            {
                UserId = user.Id,
                CreatedAt = t.AddDays(k),
                TotalCents = 5000,
                Status = OrderStatus.Delivered,
                DeletedAt = null,
                Items =
                {
                    new OrderItem { Sku = "SKU-A", Quantity = 1, UnitPriceCents = 2000 },
                    new OrderItem { Sku = "SKU-B", Quantity = 1, UnitPriceCents = 3000 }
                }
            };
            db.Orders.Add(order);
        }
        await db.SaveChangesAsync();

        var svc = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));

        var stats = await svc.GetAsync(user.Id, CancellationToken.None);

        stats.Should().NotBeNull();
        stats!.OrderCount.Should().Be(3, "three orders were placed; line-item count must not leak into the order count");
        stats.LifetimeSpend.Should().Be(150m);
    }
}
