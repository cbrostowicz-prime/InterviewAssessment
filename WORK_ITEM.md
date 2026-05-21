# ORD-1701 — Add refund and chargeback transitions to order history

**Reporter:** Dani Reyes (Finance Ops) / Marcus Tan (Trust & Safety)
**Severity:** Major (feature)
**Component:** order-history-api / order-history-web
**Status:** Open

## Background

Two adjacent asks landed in the same sprint and overlap enough that we're
tracking them on one ticket:

- **Finance** runs end-of-month reconciliation against the order-history
  API. Today they have no first-class way to mark an order as **refunded**
  inside the system — they fix it in the ledger and the order row keeps
  showing as `Paid`/`Delivered`. That breaks lifetime-spend numbers on the
  account page and makes the reconciliation diff impossible to read.

- **Operations / Trust & Safety** needs a **chargeback** transition for
  fraud handling. When a card network reverses a payment we need it
  recorded on the order, distinct from a refund (so we can report on
  fraud rates separately) and excluded from lifetime spend the same way.

Both transitions are non-destructive from a row standpoint — the order
stays, the status changes, stats stop counting it.

## What we need

- `POST /api/orders/{id}/refund` — allowed from `Paid` or `Delivered`.
  Transitions the order to `Refunded` (status value already exists).
- `POST /api/orders/{id}/chargeback` — allowed from `Paid` or `Delivered`.
  Transitions the order to a new `ChargedBack` status (add it to the
  `OrderStatus` enum; next free value is `6`).
- `UserStatsService.ComputeAsync` excludes both `Refunded` and
  `ChargedBack` from order count and lifetime spend. (`Refunded` is
  already excluded — keep it that way.)
- Frontend `OrderTable` gets **Refund** and **Chargeback** buttons per
  row, enabled only on `Paid` or `Delivered`. Wire mutation hooks and
  invalidate the right query keys (orders list + stats) on success.

## Acceptance criteria

- The two new service methods (`OrderService.RefundAsync`,
  `OrderService.ChargebackAsync`) succeed on a `Paid` or `Delivered`
  order, return a meaningful result enum (`NotFound` /
  `AlreadyRefunded` etc.) otherwise, and persist the new status.
- `UserStatsService.GetAsync` reflects the new status on the next read
  — no TTL wait. (See "Heads-up" below.)
- Controller endpoints return `204 No Content` on success, `404` for
  missing, `409` for an invalid transition. Today both endpoints return
  `404` — they don't exist yet.
- The two failing tests in
  `backend/tests/OrderHistory.Tests/RefundAndChargebackTests.cs` go
  green:
  - `RefundingPaidOrder_TransitionsToRefunded_AndExcludesFromStats`
  - `ChargingBackPaidOrder_TransitionsToChargedBack_AndExcludesFromStats`
- All existing tests still pass.

## Repro / current gap

There is no UI affordance and the endpoints don't exist:

```
curl -i -X POST http://localhost:5080/api/orders/1/refund      # 404
curl -i -X POST http://localhost:5080/api/orders/1/chargeback  # 404
```

Service-level proof — the stubs throw `NotImplementedException`:

```
cd backend
dotnet test tests/OrderHistory.Tests/OrderHistory.Tests.csproj \
  --filter "FullyQualifiedName~RefundAndChargebackTests" --nologo
```

## Heads-up

This is the follow-on to ORD-1411 (the cancel-doesn't-invalidate-stats
bug). The stats-invalidation discipline you established there applies
here too — design these transitions so future status changes don't drift
out of sync with the cache again. If a single chokepoint or convention
falls out naturally, capture it in the PR description.
