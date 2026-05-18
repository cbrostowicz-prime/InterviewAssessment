namespace OrderHistory.Api.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int UnitPriceCents { get; set; }

    public Order Order { get; set; } = null!;
}
