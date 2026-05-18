using OrderHistory.Api.Models;

namespace OrderHistory.Api.Dtos;

public record OrderItemDto(string Sku, int Quantity, decimal UnitPrice);

public record OrderDetailDto(
    int Id,
    int UserId,
    decimal Total,
    OrderStatus Status,
    DateTime CreatedAt,
    IReadOnlyList<OrderItemDto> Items);
