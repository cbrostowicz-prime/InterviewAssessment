using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OrderHistory.Api.Data;
using OrderHistory.Api.Dtos;
using OrderHistory.Api.Models;

namespace OrderHistory.Api.Services;

public class UserStatsService(OrderDbContext db, IMemoryCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private static string Key(int userId) => $"user-stats:{userId}";

    public async Task<UserStatsDto?> GetAsync(int userId, CancellationToken ct)
    {
        if (cache.TryGetValue<UserStatsDto>(Key(userId), out var cached) && cached is not null)
        {
            return cached;
        }

        var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId, ct);
        if (!exists)
        {
            return null;
        }

        var stats = await ComputeAsync(userId, ct);
        cache.Set(Key(userId), stats, CacheTtl);
        return stats;
    }

    public void Invalidate(int userId) => cache.Remove(Key(userId));

    private async Task<UserStatsDto> ComputeAsync(int userId, CancellationToken ct)
    {
        var rows = db.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId
                        && o.Status != OrderStatus.Cancelled
                        && o.Status != OrderStatus.Refunded);

        var count = await rows.CountAsync(ct);
        var totalCents = count == 0 ? 0L : await rows.SumAsync(o => (long)o.TotalCents, ct);
        return new UserStatsDto(userId, count, totalCents / 100m);
    }
}
