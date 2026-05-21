# ORD-1671 — Cancel button on Shipped orders looks like it does nothing

**Reporter:** Marco Reyes (Customer Support Lead)
**Severity:** Major
**Component:** order-history-frontend

## What's happening

Support has fielded a string of repeat calls this week from customers
insisting the Cancel button on the order-history page "doesn't work."
Every one of them is trying to cancel an order that has already shipped.
The button is enabled, they click it, the label flickers to "Cancelling…"
for a beat, and then it snaps back to "Cancel" — no toast, no banner, no
inline message, nothing in the row changes. The order stays "Shipped."

So they call us. One customer (Alice Nguyen, user 101) called twice
because she thought the first click was lost, then a third time because
she thought we were ignoring her.

The API is fine. The cancel endpoint correctly refuses with
`409 Conflict` and an error body of
`{"error":"Shipped or delivered orders cannot be cancelled."}`.
The frontend just swallows the rejection.

There are two things wrong here and a candidate fix can land on either
side (or both):

1. The Cancel button should not be enabled on a Shipped row in the first
   place — `OrderTable.tsx` has a `TERMINAL_STATUSES` set that already
   covers Delivered/Cancelled/Refunded but forgot Shipped.
2. Even if the user does trigger a 409 somehow (race, optimistic stale
   state), the UI has to surface the server's error. Right now the
   mutation hook has no `onError`, so the rejection vanishes.

## Repro

1. Start the stack: `docker compose up -d`, run the API, run the frontend.
2. Open the app, pick **Alice Nguyen** (user 101) from the User dropdown.
3. In the orders table, find any row with **Status = "Shipped"**
   (the seed data puts several in the 7–21 day window).
4. Click **Cancel** on that row.
5. The button label briefly reads "Cancelling…" then reverts to "Cancel".
   **Nothing else happens.** The row's status is still "Shipped", and
   there is no error toast, banner, or inline message anywhere on screen.
6. Open DevTools → Network. The `POST /api/orders/{id}/cancel` request
   returned `409 Conflict` with body
   `{"error":"Shipped or delivered orders cannot be cancelled."}`.
   The frontend ate the error silently.

There is a failing Vitest pinning this down:

```
cd frontend
npx vitest run components/__tests__/CancelOnShipped
```

## Acceptance criteria

- A user looking at a Shipped order cannot end up in a state where they
  clicked Cancel and got no feedback at all. Either the button is
  disabled on Shipped rows, or a 409 from the cancel endpoint surfaces a
  visible error message to the user (toast, banner, or inline). Both is
  the strongest answer.
- `CancelOnShipped` test passes.
- Existing tests still pass; no regressions in Cancel behavior for
  Placed / Paid orders (those should still cancel successfully).
