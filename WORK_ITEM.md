# ORD-1622 — Stats Order Count is higher than the order list total for some users

**Reporter:** Jordan Reyes (Customer Support)
**Severity:** Major
**Component:** order-history-api / order-history-web
**Status:** Open

## What's happening

Support escalated this after a customer (Ben Carter, user 102) noticed the
numbers on his account page don't agree with each other. The orders table
pager shows one total at the bottom, and the Stats panel "Order Count" sits
visibly higher. He was worried we were "losing" orders, or worse, charging
for ones he didn't see. We're not — but the discrepancy is real.

Spot-checking the other seeded accounts, only **user 102** shows the
mismatch. The other users (101, 103, 104, 105) line up.

Finance also pings on this because the per-user lifetime spend they pull
from `/api/users/{id}/stats` is similarly inflated for the affected
account, which throws off a couple of their reconciliation reports.

## Repro

1. Start the stack: `docker compose up -d`, run the API, run the frontend.
2. In the User dropdown, pick **Ben Carter** (user 102).
3. Look at the orders table pager — note the total (e.g. "Page 1 of 3,
   52 orders").
4. Look at the Stats panel "Order Count" on the same screen — it reads
   higher than the pager total (e.g. 53 vs 52, depending on cancellations).
5. Switch the dropdown to any other user. Pager total and Stats Order Count
   match.

There's a failing integration test that pins the behavior down:
`StatsExcludesSoftDeletedOrdersTests.Stats_DoNotCount_SoftDeletedOrders`.

```
cd backend
dotnet test --filter "FullyQualifiedName~StatsExcludesSoftDeletedOrdersTests"
```

## Acceptance criteria

- Stats `OrderCount` and `LifetimeSpend` must not include orders that have
  been soft-deleted (`DeletedAt != null`).
- The orders list keeps its current behavior — soft-deleted orders stay
  hidden unless `includeDeleted=true`.
- The failing test goes green; the existing tests still pass.

## Heads-up

Soft-delete is a cross-cutting concern. Please give the rest of the order
endpoints a pass while you're in there — anything that operates on an
order id (cancel, refund, single-order fetch, the pager's total) should
behave consistently for deleted orders. We'd rather not get a follow-up
ticket next sprint when somebody tries to refund a deleted order through
the API. If you want to lift the filter to a single place so this can't
drift again, that's encouraged — call it out in the PR description.
