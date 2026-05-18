# ORD-1247 — Order history shows duplicate orders across pages

**Reporter:** Sasha Lin (Customer Success)
**Severity:** Major
**Component:** order-history-api / order-history-web
**Status:** Open

## What's happening

A handful of customers are seeing the same order listed on two consecutive
pages of their order history. Other orders go missing from the list entirely
— but those orders still show up correctly when you look them up directly
by ID. The list endpoint's `total` count is right; it's just the items that
appear scrambled across pages.

We can't reproduce it for most users. The report Sasha forwarded is from
one specific customer — **user 982 ("Felix Roy")**, whose 40 orders were
all created in one of four bulk-import batches when their ERP integration
first went live. Every order inside a batch has an **identical**
`created_at` because the importer wrote them in a tight loop.

| Batch timestamp (UTC) | Orders in batch | Order IDs |
|-----------------------|----------------:|-----------|
| 2025-06-04 11:15:00   | 10              | 264 – 273 |
| 2025-01-21 16:45:00   | 10              | 254 – 263 |
| 2024-09-03 09:30:00   | 10              | 244 – 253 |
| 2024-05-12 14:00:00   | 10              | 234 – 243 |

You can pull these yourself: `GET /api/orders?userId=982&pageSize=50`,
or pick "Felix Roy (ERP import)" from the user dropdown in the UI.

## What the customer sent us

Sasha attached a screen recording. The customer was paging through their
order history with `pageSize=5`. The first two pages from their session:

| Page | Order IDs returned (customer's session) |
|------|-----------------------------------------|
| 1    | #273, #271, **#272**, #269, **#270**    |
| 2    | **#272** ← repeat, **#270** ← repeat, #268, #267, #266 |

Two orders from page 1 (#272 and #270) appear again on page 2. Meanwhile
**#265 and #264 never appear at all** — the customer paged through every
page and they were never shown, even though `GET /api/orders/265` and
`GET /api/orders/264` return them fine and the `total` field across pages
sums to 40.

The customer's stated frustration: "I'm trying to reconcile a $424.93
charge from June. I see it on one page, refresh, and it's on a different
page or gone." (That's order **#267** — June 4, 2025, $424.93.)

## What you'll see locally

If you page through user 982 against your local database, you'll most
likely see the canonical order:

| Page | Order IDs returned (your local DB)   |
|------|--------------------------------------|
| 1    | #273, #272, #271, #270, #269         |
| 2    | #268, #267, #266, #265, #264         |
| 3    | #263, #262, #261, #260, #259         |
| …    | …                                    |
| 8    | #238, #237, #236, #235, #234         |

No dupes, no gaps. That's because SQL Server's query plan on this small
dataset happens to be stable. **The bug is intermittent in the API/UI** —
plan choice depends on stats, parallelism, and concurrent load — but the
query itself is unsafe regardless of whether dupes surface today.

## Repro (this is the source of truth)

The on-call rotation handed this over with the following note:

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
- Regression test coverage so this can't reappear.
- Existing pagination tests still pass.

## Not in scope

- Cursor-based pagination is fine if you want it, but it's not required —
  the simplest correct fix is also acceptable. Document the choice in the
  PR description.
- Soft-delete behavior. There's a separate ticket if you spot something
  there.
