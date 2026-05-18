# ORD-1247 — Order history shows duplicate orders across pages

**Reporter:** Sasha Lin (Customer Success)
**Severity:** Major
**Component:** order-history-api / order-history-web
**Status:** Open

## What's happening

Customers are seeing the same order appear on two consecutive pages of
their order history. Other orders go missing from the list entirely — but
those orders still show up correctly when you look them up directly by ID.
The list endpoint's `total` count is right; it's just the items that come
back scrambled across pages.

It only shows up when a user has multiple orders sharing the same
`created_at` timestamp. That's exactly what happens to anyone whose orders
came in from a bulk import — the importer writes them in a tight loop and
they all stamp identically.

## Repro

The clearest report is from **user 982 ("Felix Roy")**, whose 40 orders
were created in four import batches:

| Batch timestamp (UTC) | Orders in batch | Order IDs |
|-----------------------|----------------:|-----------|
| 2025-06-04 11:15:00   | 10              | 264 – 273 |
| 2025-01-21 16:45:00   | 10              | 254 – 263 |
| 2024-09-03 09:30:00   | 10              | 244 – 253 |
| 2024-05-12 14:00:00   | 10              | 234 – 243 |

Open the UI, pick "Felix Roy (ERP import)" from the user dropdown, and
page through with the default page size. You'll likely see something like:

| Page | Order IDs on screen                  |
|------|--------------------------------------|
| 1    | #267, #266, #272, #271, #268         |
| 2    | **#268** (repeat), #265, #264, **#269**, **#271** (repeat) |
| 3    | #260, #261, #262, #257, #258         |

Now **refresh the page**. You'll see a completely different set of order
IDs on page 1 — the exact same orders just reshuffled — and a different
set of dupes between pages. This is the smoking gun: the result for any
given page is not stable across requests.

You can also hit the API directly to see it:

```
GET http://localhost:5080/api/orders?userId=982&page=1&pageSize=5
GET http://localhost:5080/api/orders?userId=982&page=2&pageSize=5
```

Compare the IDs between the two responses. Hit each one again and watch
them change.

## What we think is happening

Pagination is fed a sort that doesn't uniquely identify rows, so the
database is free to return tied rows in any order — and it's exercising
that freedom on every call. With OFFSET/FETCH on an unstable sort, the
first N rows of "the second page" can overlap with the first N rows of
"the first page," and rows that should appear on page 2 are pushed out
of the window entirely.

The on-call rotation left this note:

> I added a failing test before I went out — `PaginationStableOrderingTests`.
> It pages through this user's tied-timestamp orders and inspects what the
> query is actually doing. Start there. Don't merge until it goes green.
> Sorry for the dropped baton.

```
cd backend
dotnet test --filter "FullyQualifiedName~PaginationStableOrderingTests"
```

## Acceptance criteria

- Paging through a user's orders returns each order exactly once, even
  when multiple orders share a `created_at` timestamp.
- The order of rows on a given page is the same across repeated requests.
- Regression test coverage so this can't reappear.
- Existing pagination tests still pass.

## Not in scope

- Cursor-based pagination is fine if you want it, but it's not required —
  the simplest correct fix is also acceptable. Document the choice in the
  PR description.
- Soft-delete behavior. There's a separate ticket if you spot something
  there.
