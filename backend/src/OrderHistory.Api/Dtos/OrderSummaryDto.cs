using OrderHistory.Api.Models;

namespace OrderHistory.Api.Dtos;

public record OrderSummaryDto(
    int Id,
    int UserId,
    decimal Total,
    OrderStatus Status,
    DateTime CreatedAt);
