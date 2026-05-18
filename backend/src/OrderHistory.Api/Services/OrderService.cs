using Microsoft.EntityFrameworkCore;
using OrderHistory.Api.Data;
using OrderHistory.Api.Dtos;
using OrderHistory.Api.Models;

namespace OrderHistory.Api.Services;

public class OrderService(OrderDbContext db, UserStatsService stats)
{
    public async Task<PagedResult<OrderSummaryDto>> ListByUserAsync(
        int userId,
        int page,
        int pageSize,
        bool includeDeleted,
        CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => pageSize
        };

        var query = db.Orders.AsNoTracking().Where(o => o.UserId == userId);
        if (!includeDeleted)
        {
            query = query.Where(o => o.DeletedAt == default(DateTime?));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.UserId,
                o.TotalCents / 100m,
                o.Status,
                o.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<OrderSummaryDto>(items, page, pageSize, total);
    }

    public async Task<OrderDetailDto?> GetByIdAsync(int orderId, CancellationToken ct)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            return null;
        }

        var items = order.Items
            .Select(i => new OrderItemDto(i.Sku, i.Quantity, i.UnitPriceCents / 100m))
            .ToList();

        return new OrderDetailDto(
            order.Id,
            order.UserId,
            order.TotalCents / 100m,
            order.Status,
            order.CreatedAt,
            items);
    }

    public async Task<OrderDetailDto> PlaceAsync(PlaceOrderRequest request, CancellationToken ct)
    {
        var userExists = await db.Users.AnyAsync(u => u.Id == request.UserId, ct);
        if (!userExists)
        {
            throw new InvalidOperationException($"User {request.UserId} does not exist.");
        }

        var items = request.Items
            .Select(i => new OrderItem
            {
                Sku = i.Sku,
                Quantity = i.Quantity,
                UnitPriceCents = (int)Math.Round(i.UnitPrice * 100m)
            })
            .ToList();

        var order = new Order
        {
            UserId = request.UserId,
            Status = OrderStatus.Placed,
            CreatedAt = DateTime.UtcNow,
            TotalCents = items.Sum(i => i.Quantity * i.UnitPriceCents),
            Items = items
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        stats.Invalidate(request.UserId);

        var detailItems = order.Items
            .Select(i => new OrderItemDto(i.Sku, i.Quantity, i.UnitPriceCents / 100m))
            .ToList();

        return new OrderDetailDto(
            order.Id,
            order.UserId,
            order.TotalCents / 100m,
            order.Status,
            order.CreatedAt,
            detailItems);
    }

    public async Task<CancelResult> CancelAsync(int orderId, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            return CancelResult.NotFound;
        }
        if (order.Status == OrderStatus.Cancelled)
        {
            return CancelResult.AlreadyCancelled;
        }
        if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered)
        {
            return CancelResult.NotCancellable;
        }

        order.Status = OrderStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return CancelResult.Cancelled;
    }
}

public enum CancelResult
{
    Cancelled,
    NotFound,
    AlreadyCancelled,
    NotCancellable
}
