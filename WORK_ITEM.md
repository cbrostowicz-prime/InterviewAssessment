# ORD-1738 — Order history pager lets users click into a "phantom" page past the last one

**Reporter:** Marcus Holloway (Customer Success)
**Severity:** Minor
**Component:** order-history-frontend

## What's happening

A customer (Alice Nguyen, user 101) opened a CS ticket saying "I thought
I had more orders — the page count keeps going up but the list is
blank." The CSM repro'd it in the app immediately: the Next button on
the order-history pager never stops. You can click past the last real
page, the table goes empty, and the footer counter renders a nonsense
range like "41–35 of 35" while implying there's a Page 5 of a 4-page
dataset.

It's not a data problem — the API returns the right total and an empty
items list for the over-shot page. The Pager just doesn't bound itself,
so clicking Next on the last page bumps `page` to `lastPage + 1`,
re-fetches, and renders an empty table.

Repeated clicks keep climbing (Page 6, Page 7, …) with no ceiling. Prev
works fine and brings you back to a real page.

## Repro

1. Start the stack: `docker compose up -d`, run the API, run the frontend.
2. Open the app — Alice Nguyen (user 101) is selected by default.
3. Click **Next** on the pager until you're on the last page (with the
   default page size of 10 that's around Page 4 for Alice — the exact
   last page depends on her seeded order count, ~30–60).
4. The footer reads something like "31–35 of 35" and the table shows the
   tail of her orders. Good.
5. Click **Next** one more time. The order table renders empty. The
   footer now reads "41–35 of 35" — the "from" number is past the "to"
   and past the total.
6. Click **Next** again. It keeps incrementing — there is no upper bound.
7. Click **Prev**. You land back on a real page with real rows.

There's a failing component test that pins this down on a tiny fixture
(total=10, pageSize=5, page=2 — already the last page):

```
cd frontend
npx vitest run components/__tests__/Pager
```

The new `Pager > does not emit a page beyond the last page when Next is
clicked on the last page` case fails with `Pager emitted page(s) past
lastPage=2: 3`.

## Acceptance criteria

- Clicking Next on the last page is a no-op (button disabled, or click
  ignored — pick one and be consistent with the Prev-on-first-page
  behaviour, which is already disabled).
- The footer never shows a "from" greater than the "total" or a page
  number greater than the last page.
- Existing Pager tests pass; the new overshoot test passes.

## Heads-up

The bound on Next used to live in `Pager.tsx` itself (mirror of the
`page <= 1` guard on Prev). It looks like it was removed during a recent
refactor. Decide whether bounds-checking lives in the Pager component,
in the parent (`App.tsx`), or both — and write down the choice. The
parent currently passes `setPage` straight through without clamping, so
right now nobody is enforcing the upper bound.
