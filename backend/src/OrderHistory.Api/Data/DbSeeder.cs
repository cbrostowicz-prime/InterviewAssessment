using OrderHistory.Api.Models;

namespace OrderHistory.Api.Data;

public static class DbSeeder
{
    public static readonly DateTime ReferenceNow = new(2025, 12, 1, 12, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyList<User> Users { get; } =
    [
        new User { Id = 101, Name = "Alice Nguyen",    Email = "alice@example.com" },
        new User { Id = 102, Name = "Ben Carter",      Email = "ben@example.com" },
        new User { Id = 103, Name = "Carla Diaz",      Email = "carla@example.com" },
        new User { Id = 104, Name = "Devon Park",      Email = "devon@example.com" },
        new User { Id = 105, Name = "Esra Yilmaz",     Email = "esra@example.com" },
        new User { Id = 982, Name = "Felix Roy (ERP import)", Email = "felix.roy@example.com" }
    ];

    private static readonly (string Sku, int PriceCents)[] Catalog =
    [
        ("SKU-WIDGET-RED",   1299),
        ("SKU-WIDGET-BLUE",  1499),
        ("SKU-GADGET-S",     2499),
        ("SKU-GADGET-M",     3299),
        ("SKU-GADGET-L",     4199),
        ("SKU-DOOHICKEY",     899),
        ("SKU-THINGAMAJIG",  1899),
        ("SKU-CONTRAPTION",  5299),
        ("SKU-GIZMO-MINI",   1099),
        ("SKU-GIZMO-PRO",    7899)
    ];

    public static (IReadOnlyList<User> Users, IReadOnlyList<Order> Orders, IReadOnlyList<OrderItem> Items)
        Build()
    {
        var orders = new List<Order>();
        var items = new List<OrderItem>();
        var rng = new Random(20240117);

        int nextOrderId = 1;
        int nextItemId = 1;

        foreach (var user in Users)
        {
            if (user.Id == 982)
            {
                SeedBulkImportUser(user, orders, items, rng, ref nextOrderId, ref nextItemId);
                continue;
            }

            var orderCount = 30 + rng.Next(31);

            for (var k = 0; k < orderCount; k++)
            {
                var daysAgo = rng.NextDouble() * 730d;
                var jitterMinutes = rng.Next(0, 1440);
                var createdAt = ReferenceNow
                    .AddDays(-daysAgo)
                    .AddMinutes(jitterMinutes);

                var lineCount = 1 + rng.Next(4);
                var orderId = nextOrderId++;
                var status = PickStatus(rng, createdAt);

                var lineTotal = 0;
                for (var li = 0; li < lineCount; li++)
                {
                    var prod = Catalog[rng.Next(Catalog.Length)];
                    var qty = 1 + rng.Next(3);
                    lineTotal += qty * prod.PriceCents;
                    items.Add(new OrderItem
                    {
                        Id = nextItemId++,
                        OrderId = orderId,
                        Sku = prod.Sku,
                        Quantity = qty,
                        UnitPriceCents = prod.PriceCents
                    });
                }

                orders.Add(new Order
                {
                    Id = orderId,
                    UserId = user.Id,
                    Status = status,
                    CreatedAt = createdAt,
                    TotalCents = lineTotal,
                    DeletedAt = null
                });
            }
        }

        return (Users, orders, items);
    }

    private static void SeedBulkImportUser(
        User user,
        List<Order> orders,
        List<OrderItem> items,
        Random rng,
        ref int nextOrderId,
        ref int nextItemId)
    {
        DateTime[] importBatches =
        [
            new DateTime(2024, 5, 12, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 9, 03, 09, 30, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 21, 16, 45, 0, DateTimeKind.Utc),
            new DateTime(2025, 6, 04, 11, 15, 0, DateTimeKind.Utc)
        ];
        const int ordersPerBatch = 10;

        foreach (var createdAt in importBatches)
        {
            for (var i = 0; i < ordersPerBatch; i++)
            {
                var orderId = nextOrderId++;
                var lineCount = 1 + rng.Next(3);
                var lineTotal = 0;
                for (var li = 0; li < lineCount; li++)
                {
                    var prod = Catalog[rng.Next(Catalog.Length)];
                    var qty = 1 + rng.Next(3);
                    lineTotal += qty * prod.PriceCents;
                    items.Add(new OrderItem
                    {
                        Id = nextItemId++,
                        OrderId = orderId,
                        Sku = prod.Sku,
                        Quantity = qty,
                        UnitPriceCents = prod.PriceCents
                    });
                }
                orders.Add(new Order
                {
                    Id = orderId,
                    UserId = user.Id,
                    Status = OrderStatus.Delivered,
                    CreatedAt = createdAt,
                    TotalCents = lineTotal,
                    DeletedAt = null
                });
            }
        }
    }

    private static OrderStatus PickStatus(Random rng, DateTime createdAt)
    {
        var ageDays = (ReferenceNow - createdAt).TotalDays;
        if (ageDays > 21)
        {
            return rng.Next(20) == 0 ? OrderStatus.Cancelled : OrderStatus.Delivered;
        }
        if (ageDays > 7)
        {
            return rng.Next(10) switch
            {
                0 => OrderStatus.Cancelled,
                1 or 2 => OrderStatus.Shipped,
                _ => OrderStatus.Delivered
            };
        }
        return rng.Next(10) switch
        {
            < 4 => OrderStatus.Placed,
            < 7 => OrderStatus.Paid,
            < 9 => OrderStatus.Shipped,
            _ => OrderStatus.Cancelled
        };
    }
}
