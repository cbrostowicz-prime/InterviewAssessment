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

| Batch timestamp (UTC) | Orders in batch | Order IDs                                            |
|-----------------------|-----------------|------------------------------------------------------|
| 2025-06-04 11:15:00   | 10              | 237, 241, 245, 249, 253, 257, 261, 265, 269, 273     |
| 2025-01-21 16:45:00   | 10              | 236, 240, 244, 248, 252, 256, 260, 264, 268, 272     |
| 2024-09-03 09:30:00   | 10              | 235, 239, 243, 247, 251, 255, 259, 263, 267, 271     |
| 2024-05-12 14:00:00   | 10              | 234, 238, 242, 246, 250, 254, 258, 262, 266, 270     |

You can pull these yourself: `GET /api/orders?userId=982&pageSize=50`,
or pick "Felix Roy (ERP import)" from the user dropdown in the UI.

## What the customer sent us

Sasha attached a screen recording. The customer was paging with
`pageSize=5`. Three consecutive pages from their session:

| Page | Order IDs returned                                          |
|------|-------------------------------------------------------------|
| 1    | **#273**, **#269**, **#257**, #265, #261                    |
| 2    | **#269** (repeat), **#257** (repeat), #253, #249, #245      |
| 3    | #237, #272, #268, #264, #260                                |

Two orders from page 1 (#269 and #257) appear again on page 2. Meanwhile
**#241 never appears at all** — the customer paged through all 8 pages and
it was never shown, even though `GET /api/orders/241` returns it just fine
and the totals across pages add up to 40.

The customer's stated frustration: "I'm trying to find a $135.94 charge
from June and I see it on one page, refresh, and now it's on a different
page or gone."

## Repro

The on-call rotation handed this over with the following note:

> I added a failing test before I went out — `PaginationStableOrderingTests`.
> It pages through this user's tied-timestamp orders and inspects what the
> query is actually doing. Start there. Don't merge until it goes green.
> Sorry for the dropped baton.

```
cd backend
dotnet test --filter "FullyQualifiedName~PaginationStableOrderingTests"
```

Heads-up: the bug is **intermittent in the API/UI** — SQL Server's choice
of query plan depends on stats, parallelism, and load, and on a small local
dataset you may see consistent results across pages even though the
underlying query is fundamentally unsafe. The failing test pins this down
deterministically; trust it over manual repros.

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
