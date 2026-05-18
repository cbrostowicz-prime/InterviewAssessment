# ORD-1247 — Order history shows duplicate orders across pages

**Reporter:** Sasha Lin (Customer Success)
**Severity:** Major
**Component:** order-history-api / order-history-web
**Status:** Open

## What's happening

A handful of customers are seeing the same order listed on two consecutive
pages of their order history. Other orders seem to be missing entirely — but
those orders still show up correctly if you look them up directly by ID. The
list endpoint's reported total count is right; the items just appear scrambled
across pages.

We can't reproduce it for most users. The clearest reports are from customers
whose orders were created in bulk imports (ERP migration last quarter — a
batch of orders gets the same `created_at` because the importer was writing
them in a tight loop). The screenshots Sasha attached are from **user 982**.

## Repro

The on-call rotation handed this over with the following note:

> I added a failing test before I went out — `PaginationStableOrderingTests`.
> It pages through a synthesized run of tied-timestamp orders and inspects
> what the query is actually doing. Start there. Don't merge until it goes
> green. Sorry for the dropped baton.

```
cd backend
dotnet test --filter "FullyQualifiedName~PaginationStableOrderingTests"
```

## Acceptance criteria

- Paging through a user's orders returns each order exactly once, even when
  multiple orders share a `created_at` timestamp.
- Regression test coverage so this can't reappear.
- Existing pagination tests still pass.

## Not in scope

- Cursor-based pagination is fine if you want it, but it's not required —
  the simplest correct fix is also acceptable. Document the choice in the PR.
- Soft-delete behavior. There's a separate ticket if you spot something
  there.
