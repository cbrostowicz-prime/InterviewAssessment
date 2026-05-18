using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderHistory.Api.Models;
using OrderHistory.Api.Services;
using OrderHistory.Tests.Infrastructure;

namespace OrderHistory.Tests;

[Collection("SqlServer")]
public class PaginationStableOrderingTests(SqlServerFixture fx)
{
    [Fact]
    public async Task ListByUserAsync_PagedQueryOrdersByUniqueTiebreaker()
    {
        var interceptor = new CapturingInterceptor();
        await using var db = fx.CreateContext(interceptor);

        var user = await db.AddUserAsync("paging-tied");
        var tied = new DateTime(2024, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        var spec = new List<(DateTime, int, OrderStatus)>();
        for (var i = 0; i < 40; i++)
        {
            var ts = i < 14 ? tied : i < 28 ? tied.AddDays(1) : tied.AddDays(2);
            spec.Add((ts, 1000 + i, OrderStatus.Delivered));
        }
        await db.AddOrdersAsync(user.Id, spec);

        var stats = new UserStatsService(db, new MemoryCache(new MemoryCacheOptions()));
        var svc = new OrderService(db, stats);
        interceptor.ExecutedCommands.Clear();

        await svc.ListByUserAsync(user.Id, page: 1, pageSize: 5, includeDeleted: false, CancellationToken.None);

        var pagedQuery = interceptor.ExecutedCommands
            .FirstOrDefault(c => c.Contains("OFFSET", StringComparison.OrdinalIgnoreCase));
        pagedQuery.Should().NotBeNull("the list endpoint must run a paged query that we can inspect");

        var orderByClause = Regex.Match(pagedQuery!, @"ORDER BY[^\n]+", RegexOptions.IgnoreCase).Value;
        orderByClause.Should().NotBeNullOrEmpty();
        orderByClause.Should().MatchRegex(
            @"\bId\b",
            "paginated queries need a unique tiebreaker (the primary key) so pages do not contain duplicates or gaps when CreatedAt ties");
    }
}
