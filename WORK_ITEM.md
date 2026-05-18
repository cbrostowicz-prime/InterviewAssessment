# ORD-1503 — Export a user's order history as CSV

**Reporter:** Marcus Hwang (Finance)
**Severity:** Medium / Feature
**Component:** order-history-api / order-history-web
**Status:** Open

## Background

Finance currently screenshots the order list and retypes it into a
spreadsheet when they need to reconcile. They've asked for a real export.

## What we need

- A new API endpoint that produces a CSV of a given user's orders.
- The CSV gets written to Azure Blob Storage and the endpoint returns a
  download URL the browser can hit directly. (Our internal pattern: the API
  shouldn't stream large files through its own pipeline. We already use blob
  storage for batch exports elsewhere — connection string lives under
  `BlobStorage` in config.)
- A button on the order history page that triggers the export and starts a
  browser download.

## CSV shape

Columns: `order_id`, `created_at` (ISO 8601 UTC), `status`, `total` (decimal
dollars, e.g. `123.45`, **not** cents).

## Scope

- Same userId requirement as the list endpoint.
- Needs to work for power users with thousands of orders. The largest
  account today is around 60 orders, but the team is planning for growth
  (some of our enterprise prospects export 5–10k rows on a single report).
- Container name: `order-exports`.
- Local development uses Azurite, which is already wired into
  `docker-compose.yml`. Connection string for local dev belongs in
  `appsettings.Development.json`.

## Acceptance criteria

- The endpoint returns something the frontend can use to download the file.
- The file is written to the `order-exports` container.
- The frontend page has an Export CSV button and clicking it downloads a
  CSV.
- Existing tests still pass; new behavior has test coverage.

## Notes

- A few details aren't fully specified above — make a call and call it out
  in the PR description. We'd rather see a thoughtful interpretation than a
  fully-spec'd ticket.
