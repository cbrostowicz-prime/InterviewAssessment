namespace OrderHistory.Api.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TotalCents { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
