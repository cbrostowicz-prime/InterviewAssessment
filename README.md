# Order History

A small order-history service. Backend exposes endpoints to list a user's
orders with pagination, look up an order by id, get lifetime spending stats,
and cancel an order. Frontend is a single page that selects a user, shows
their orders and stats.

## Stack

- ASP.NET Core 8 Web API, EF Core 8, xUnit + FluentAssertions
- React 18 + TypeScript (strict), Vite, Tailwind, TanStack Query v5, axios
- SQL Server 2022 and Azurite via Docker Compose

## Prerequisites

- .NET 8 SDK (or newer SDK able to target `net8.0`)
- Node.js 20+
- Docker Desktop

## Setup

From the repo root:

```powershell
docker compose up -d
```

The first SQL Server start can take 30–60 seconds. The backend retries the
initial connection during that window, so you can start the API immediately.

### Backend

```powershell
cd backend
dotnet restore
dotnet run --project src/OrderHistory.Api
```

The API listens on `http://localhost:5080`. On startup it applies pending
EF migrations and, if the database is empty, runs the deterministic seeder.

### Frontend

```powershell
cd frontend
npm install
npm run dev
```

The dev server runs on `http://localhost:5173` and proxies `/api` to the
backend.

### Tests

```powershell
cd backend
dotnet test
```

```powershell
cd frontend
npm test
```

## Troubleshooting

- **SQL Server connection refused on first start.** The container takes up
  to a minute to be ready. The API logs `Retrying database connect…` and
  succeeds once the server is up.
- **Port already in use.** SQL Server is mapped to host `11433` (not the
  default `1433`) to avoid conflicts with locally installed SQL Server
  instances. Azurite uses `10000`–`10002`. The API uses `5080`.
- **Migrations out of sync.** Drop the `OrderHistory` database in the
  container and restart the API; it will recreate and reseed.

## Layout

```
backend/    ASP.NET Core API and test project
frontend/   Vite + React + TypeScript app
docker-compose.yml
```

See `CLAUDE.md` for an agent-oriented tour of the codebase.
