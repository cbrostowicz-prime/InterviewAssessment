# ORD-1612 — Order count on Stats panel is wildly inflated

**Reporter:** Priya Mehta (Account Mgmt)
**Severity:** Major
**Component:** order-history-api

## What's happening

The "Order Count" tile on the customer account page is reading roughly
2–3× the number of orders the customer actually has. Lifetime spend on
the same tile looks right — it matches what Finance gets when they sum
the visible Total column by hand — but the count is nonsense.

It affects every seeded user, and it scales with how many line items
each order has, so customers with bigger carts look the worst. Account
Management noticed it first when a CSM pulled up Ben Carter (user 102)
to talk through his history and the panel claimed ~76 orders while the
list pager said 35.

## Repro

1. Start the stack: `docker compose up -d`, run the API and the frontend.
2. Open the app, pick **Ben Carter** (user 102) from the User dropdown.
3. Page through the orders table to confirm the real count — with the
   default page size of 10, the pager reads "Page 1 of 4" and the last
   page is short, so it's ~35 orders.
4. Look at the **Stats** panel on the right. "Order Count" reads
   dramatically higher (around 76 in local testing) — far more than the
   actual orders.
5. "Lifetime Spend" on the same panel matches a hand-sum of the visible
   Total column, so the money side is fine.

There's a failing test that pins this down with a tiny fixture (3 orders
× 2 line items each, expect OrderCount = 3, actual = 6):

```
cd backend
dotnet test tests/OrderHistory.Tests/OrderHistory.Tests.csproj \
  --filter "FullyQualifiedName~StatsCountsOrdersNotItemsTests" --nologo
```

## Acceptance criteria

- "Order Count" on the Stats panel equals the number of non-cancelled,
  non-refunded, non-soft-deleted orders for the user.
- "Lifetime Spend" stays correct (it currently is).
- `StatsCountsOrdersNotItemsTests` and all existing tests pass.

## Heads-up

Git blame on `UserStatsService.ComputeAsync` shows someone reshaped the
query to aggregate over `OrderItems` instead of `Orders` — presumably to
avoid a second round-trip for the totals. Spend stayed right by
coincidence (order totals already equal the sum of their items), but the
row count is now the line-item count, not the order count. Either fan
back out to two queries or use `Distinct()` on OrderId; either is fine.
