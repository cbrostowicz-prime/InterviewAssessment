using Microsoft.EntityFrameworkCore;
using OrderHistory.Api.Data;
using OrderHistory.Api.Models;

namespace OrderHistory.Tests.Infrastructure;

public static class TestData
{
    public static async Task<User> AddUserAsync(this OrderDbContext db, string slug)
    {
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == $"{slug}@test.local");
        if (existing is not null)
        {
            return existing;
        }
        var user = new User { Name = slug, Email = $"{slug}@test.local" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static async Task<List<Order>> AddOrdersAsync(
        this OrderDbContext db,
        int userId,
        IEnumerable<(DateTime CreatedAt, int TotalCents, OrderStatus Status)> spec)
    {
        var created = new List<Order>();
        foreach (var (createdAt, totalCents, status) in spec)
        {
            var order = new Order
            {
                UserId = userId,
                CreatedAt = createdAt,
                TotalCents = totalCents,
                Status = status,
                DeletedAt = null
            };
            db.Orders.Add(order);
            created.Add(order);
        }
        await db.SaveChangesAsync();
        return created;
    }
}
