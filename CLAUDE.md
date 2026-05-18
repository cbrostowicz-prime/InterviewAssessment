# Repository tour

This file orients an AI coding agent working in this repo. Read it once
before searching; it captures conventions that grep alone will not.

## Top-level layout

```
backend/    ASP.NET Core 8 Web API and xUnit test project
frontend/   Vite + React 18 + TypeScript app
docker-compose.yml   SQL Server 2022 + Azurite for local development
```

## Backend

Solution: `backend/OrderHistory.sln`.

```
backend/src/OrderHistory.Api/
  Program.cs              host + DI + EF migration/seed on startup
  Controllers/            thin HTTP layer; no business logic here
  Services/               OrderService, UserStatsService — all queries live here
  Data/OrderDbContext.cs  EF Core DbContext
  Data/DbSeeder.cs        deterministic seed (same data every install)
  Models/                 Order, OrderItem, User, OrderStatus
  Dtos/                   request/response shapes at the API boundary
  Migrations/             EF Core migrations
backend/tests/OrderHistory.Tests/
  Infrastructure/SqlServerFixture.cs   xUnit collection fixture that
                                       creates a per-run database on the
                                       SQL Server container, applies
                                       migrations, and drops it on
                                       teardown. Inject via primary
                                       constructor and tag the class with
                                       [Collection("SqlServer")].
  Infrastructure/TestData.cs           AddUserAsync / AddOrdersAsync
                                       extensions for arranging data.
```

### Conventions

- C# 12 idioms: file-scoped namespaces, primary constructors, collection
  expressions where they read well.
- Async all the way down. Public service methods take a
  `CancellationToken`.
- Read queries use `AsNoTracking()`. Writes use tracked entities and
  `SaveChangesAsync`.
- DTOs at the API boundary, never entities. DTOs live in `Dtos/` and are
  mapped manually (no AutoMapper).
- Pagination: clients send `page` (1-based) and `pageSize` (max 100).
  The list endpoint returns `{ items, page, pageSize, total }`.
- Money is stored as `TotalCents` (int) on the entity. DTOs expose
  `total` as decimal dollars.
- Errors: return `NotFound()` / `BadRequest()` / `Conflict()` for known
  states. Let unexpected exceptions bubble up; ASP.NET Core's developer
  exception page handles them in Development.

### Where things live

- New endpoint: add a method to the relevant controller; new query goes
  in the matching service. Do not put EF queries in controllers.
- New DTO: `Dtos/`. Add a `record` with init-only props.
- New migration: `dotnet ef migrations add <Name> --project
  src/OrderHistory.Api --startup-project src/OrderHistory.Api`. The API
  applies them on startup.
- New test: matching class under `tests/OrderHistory.Tests/`. Tag with
  `[Collection("SqlServer")]` and inject `SqlServerFixture` via the
  primary constructor. Call `fx.CreateContext()` for a fresh DbContext.

### Caching

`UserStatsService` caches per-user stats in `IMemoryCache` with a
five-minute TTL. Writes that change a user's stats must invalidate via
`UserStatsService.Invalidate(userId)`.

## Frontend

```
frontend/src/
  main.tsx, App.tsx
  api/client.ts        axios instance pointing at /api
  api/orders.ts        typed wrappers for the order endpoints
  components/          OrderTable, StatsPanel, UserPicker, Pager
  hooks/               TanStack Query hooks (useOrders, useStats, ...)
  index.css            Tailwind entry
```

### Conventions

- TypeScript strict; no `any`. Use `unknown` and narrow.
- Server state lives in TanStack Query. Local UI state is `useState`.
  No Redux, no Zustand.
- Mutations call `queryClient.invalidateQueries` for affected keys.
- Tailwind utility classes inline; no CSS modules. Keep markup readable.
- Components: function components only, named exports, props as inline
  type aliases.

### Dev proxy

`vite.config.ts` proxies `/api` → `http://localhost:5080`. The frontend
calls `/api/...`; there is no need to configure CORS for local dev. The
backend has permissive dev CORS as a safety net.

## Running

See `README.md` for setup. Common commands:

```powershell
docker compose up -d
dotnet run --project backend/src/OrderHistory.Api
cd frontend; npm run dev
dotnet test --project backend
```
