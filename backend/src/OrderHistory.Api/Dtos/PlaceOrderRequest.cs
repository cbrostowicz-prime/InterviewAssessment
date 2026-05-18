namespace OrderHistory.Api.Dtos;

public record PlaceOrderItem(string Sku, int Quantity, decimal UnitPrice);

public record PlaceOrderRequest(int UserId, IReadOnlyList<PlaceOrderItem> Items);
