# ORD-1567 — Stats panel doesn't refresh after cancelling an order

**Reporter:** Marcus Whitfield (Support Tier 2)
**Severity:** Minor
**Component:** order-history-web
**Status:** Open

## What's happening

Cancelling an order from the orders table updates the row's Status to
"Cancelled" right away, but the Lifetime stats panel above it keeps showing
the pre-cancel Orders count and Lifetime spend. Hitting the browser's
refresh button (F5) makes the panel snap to the correct numbers, so the
data lands somewhere — it just isn't surfacing until the page reloads.

This came in twice last week from agents who were second-guessing whether
the cancel actually took. The row says cancelled; the totals disagree.
Confusing in a screen-share.

## Repro

1. Pick any seeded user (101–105) from the User dropdown.
2. Find any Placed or Paid order in the table.
3. Note the current Stats panel values (Order Count, Lifetime Spend).
4. Click Cancel on that row.
5. The order row's Status flips to "Cancelled".
6. **The Stats panel does NOT update.**
7. Refresh the page (F5). Stats now drop correctly.

There's also a failing Vitest covering this:

```
cd frontend
npx vitest run src/hooks/__tests__/useCancelOrder.test.tsx
```

## Acceptance criteria

- Cancelling an order updates the Stats panel in the same render cycle the
  orders table updates — no manual refresh required.
- The new Vitest goes green.
- The existing test suite still passes.

## Heads-up

Before you go spelunking in the backend: verify the API response first.
Open DevTools Network (or `curl`) and hit `GET /api/users/{userId}/stats`
right after step 5 — the response body is already the post-cancel numbers.
There was a real backend stats-cache invalidation bug a couple sprints ago
(ORD-1411) that looked similar; this one isn't that. The server is handing
back fresh data; the client just isn't asking for it.
