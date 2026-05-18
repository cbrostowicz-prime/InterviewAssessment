namespace OrderHistory.Api.Models;

public enum OrderStatus
{
    Placed = 0,
    Paid = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4,
    Refunded = 5
}
