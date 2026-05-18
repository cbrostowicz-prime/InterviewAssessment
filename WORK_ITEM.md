# ORD-1411 — Cancelled orders still count toward lifetime spend for several minutes

**Reporter:** Priya Mehta (Account Mgmt)
**Severity:** Major
**Component:** order-history-api / order-history-web
**Status:** Open

## What's happening

When a customer (or an agent on their behalf) cancels an order, the lifetime
spend on the account page does **not** drop right away. Refreshing the page
doesn't help. A few minutes later it self-corrects.

Newly **placed** orders show up in the stats panel immediately, so the
write/read path isn't the issue in general — just cancellations.

The customer-facing implication is small bookkeeping confusion, but Finance
is annoyed because the API is the source of truth their dashboard pulls
from, and the dashboard shows the same stale numbers for a window after
each cancellation.

## Repro

1. Start the stack: `docker compose up -d`, run the API and the frontend.
2. Pick a user who has at least one order in `Placed` or `Paid` status.
3. Note the lifetime spend in the stats panel.
4. Click **Cancel** on that order.
5. The orders table updates. The stats panel does **not**.

There's also an integration test that fails:
`CancelInvalidatesStatsCacheTests.CancellingOrder_RemovesCancelledOrderFromSubsequentStats`.

```
cd backend
dotnet test --filter "FullyQualifiedName~CancelInvalidatesStatsCacheTests"
```

## Acceptance criteria

- Cancelling an order causes lifetime spend and order count to update on the
  next stats read, not after a cache TTL.
- The existing tests still pass.
- The failing test goes green.

## Heads-up

We have two more status transitions landing in the next sprint
(`MarkRefunded`, `MarkChargedBack`) and they'll need to behave the same way
with respect to stats. If you want to set things up so future status
transitions can't drift again, that's encouraged. Capture the choice in the
PR description either way.
