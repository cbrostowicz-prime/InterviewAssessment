# ORD-1583 — Lifetime spend in stats panel runs short of summed order totals

**Reporter:** Marcus Hwang (Finance)
**Severity:** Major
**Component:** order-history-api / order-history-web
**Status:** Open

## What's happening

Our month-end reconciliation report pulls lifetime-spend per customer from
the order-history API. The numbers it returns are consistently **lower**
than what we get if we sum the order rows ourselves. The gap is small per
order — somewhere in the $0.50 to $1.00 range — but it's systematic, it's
on every account I've checked, and it's the kind of thing auditors will
absolutely notice.

It's not a one-off. Every user I sample is short. Whatever it is, it
shaves something off each order before adding them up.

## Repro

1. `docker compose up -d`, run the API, run the frontend.
2. Open the app, pick **Alice Nguyen** (user 101) in the user picker.
3. Look at the **Lifetime spend** value in the stats panel on the right.
4. In the orders table below, manually add up the **Total** column for
   page 1 (default 5 rows; small enough to spot-check on paper).
   Example: an order at $19.99 + one at $25.99 + one at $49.99 should
   contribute $95.97 to the user's lifetime.
5. Note that the stats panel's lifetime spend, when you compare it
   against the sum of *all* the user's non-cancelled / non-refunded
   orders, is short by roughly $0.50–$1 per order.

Concretely: a user whose qualifying orders are $19.99, $25.99, and
$49.99 should show **$95.97** lifetime spend. Today the panel shows
**$93.00** — exactly $0.99 short, one per order, every time.

There's also a failing integration test:
`StatsLifetimeSpendPrecisionTests.LifetimeSpend_PreservesCentsAcrossOrders`.

```
cd backend
dotnet test --filter "FullyQualifiedName~StatsLifetimeSpendPrecisionTests"
```

## Acceptance criteria

- Lifetime spend equals the exact sum of qualifying order totals, to the
  cent.
- The new failing test goes green; existing stats tests still pass.

## Heads-up

This may not be the only place we're losing cents. Please audit the rest
of the money path — order placement totals (`PlaceAsync`), any other
aggregation, and the display formatter on the frontend — before closing
the ticket. If you find more of the same pattern, fold the fixes in.
