# ORD-1644 — Filter the order list by status and date range

**Reporter:** Dana Okafor (Support Lead)
**Severity:** Medium / Feature
**Component:** order-history-api / order-history-web
**Status:** Open

## Background

Support agents spend half their day scrolling through orders looking for ones
in a specific status from last month. When a customer calls about a
"refund I requested in April," the agent currently pages through every
order on the account (some power users have hundreds) hunting for the
matching row by eye.

We need real filter controls on the order history page, and the API needs
to honor those filters so pagination doesn't fight against the agent.

## What we need

- `GET /api/orders` accepts new query parameters:
  - `status` — repeating param, multi-select. `?status=Placed&status=Paid`
    returns orders whose status is in the supplied set.
  - `fromDate` — ISO date string. Inclusive lower bound on `CreatedAt`.
  - `toDate` — ISO date string. Inclusive upper bound on `CreatedAt`.
- `OrderService.ListByUserAsync` applies the filters **before** pagination.
  The `total` in the response must reflect the filtered count, not the
  unfiltered count, or the pager will lie.
- A filter bar above the order table on the user history page:
  - Multi-select checkbox group for statuses
    (Placed / Paid / Shipped / Delivered / Cancelled / Refunded).
  - Two `<input type="date">` controls for From and To.
  - Apply on change is fine; an Apply / Search button is also fine.
    Candidate's call — pick what reads better.
- Changing any filter must reset pagination to page 1. Today the pager
  state would survive a filter change and the user would land on page 5
  of a 1-page result.

## Repro / current behaviour

- No filter controls render above the order table — only the user picker
  and pager are there.
- The API silently accepts unknown query parameters today. A request like

  ```
  GET /api/orders?userId=101&status=Placed&fromDate=2026-01-01
  ```

  returns **200 OK with every order on the account**, not the filtered
  subset. There's a failing integration test that pins this down:
  `OrderSearchFilterTests.Filter_ByStatus_ReturnsOnlyMatching`.

  ```
  cd backend
  dotnet test --filter "FullyQualifiedName~OrderSearchFilterTests"
  ```

## Acceptance criteria

- The three failing `OrderSearchFilterTests` cases go green.
- The existing list tests still pass (filters default to "no filter").
- The UI surfaces the controls described above, and switching filters
  resets the page to 1.
- `total` in the paged response is the filtered count.

## Heads-up

A separate ticket (ORD-1647, CSV export) wants the same filters to flow
through to the export endpoint eventually. Out of scope here, but try not
to design yourself into a corner if it's cheap to share the filter shape.
