using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHistory.Api.Models;
using OrderHistory.Api.Services;
using OrderHistory.Tests.Infrastructure;

namespace OrderHistory.Tests;

[Collection("SqlServer")]
public class OrderServiceListTests(SqlServerFixture fx)
{
    private const string Slug = "list-tests";

    private async Task<(OrderService Svc, int UserId)> BuildAsync()
    {
        var db = fx.CreateContext();
        var user = await db.AddUserAsync(Slug);
        if (!db.Orders.Any(o => o.UserId == user.Id))
        {
            var baseTime = new DateTime(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);
            await db.AddOrdersAsync(user.Id,
                Enumerable.Range(0, 12).Select(i =>
                    (baseTime.AddDays(i), 1000 + i * 100, OrderStatus.Delivered)));
        }
        var stats = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));
        return (new OrderService(db, stats), user.Id);
    }

    [Fact]
    public async Task FirstPage_ReturnsMostRecentOrdersDescending()
    {
        var (svc, userId) = await BuildAsync();

        var page = await svc.ListByUserAsync(userId, page: 1, pageSize: 5, includeDeleted: false, CancellationToken.None);

        page.Total.Should().Be(12);
        page.Items.Should().HaveCount(5);
        page.Items.Should().BeInDescendingOrder(o => o.CreatedAt);
    }

    [Fact]
    public async Task PageBeyondLast_ReturnsEmpty()
    {
        var (svc, userId) = await BuildAsync();

        var page = await svc.ListByUserAsync(userId, page: 99, pageSize: 5, includeDeleted: false, CancellationToken.None);

        page.Items.Should().BeEmpty();
        page.Total.Should().Be(12);
    }

    [Fact]
    public async Task PageSize_IsCapped()
    {
        var (svc, userId) = await BuildAsync();

        var page = await svc.ListByUserAsync(userId, page: 1, pageSize: 5000, includeDeleted: false, CancellationToken.None);

        page.PageSize.Should().Be(100);
    }
}
