# TaskFlow

A lightweight task manager for an internal team: a .NET 8 REST API on SQL Server, and a responsive React front end.

- **API:** list with filtering and sorting, get, create, update, change status, soft delete, and a raw-SQL summary.
- **Accounts:** registration limited to company email domains, sign-in with lockout, and 15-minute JWTs with a rotating httpOnly refresh cookie.
- **UI:** table on desktop, cards on phones, a drag-and-drop board, colour + icon priority, inline status dropdowns.
- **Extras:** unit tests (.NET and React), Docker Compose for the whole stack, and a .NET Standard 2.0 shared library.

Design and decisions: [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md). UI mockups: [`docs/mockups/index.html`](docs/mockups/index.html).

## Run everything with Docker

Needs Docker (Docker Desktop, or Colima on macOS). On Apple Silicon, SQL Server runs under x86 emulation, so enable Rosetta (Docker Desktop: *Settings → General → Use Rosetta*; Colima: `colima start --vm-type vz --vz-rosetta --memory 6`).

```bash
cp .env.example .env
```

Edit `.env`: set `JWT_SIGNING_KEY` (`openssl rand -base64 48`) and, if you like, `SA_PASSWORD`. Then:

```bash
docker compose up --build
```

Open <http://localhost:8080>. The first start takes a minute while SQL Server initialises; the API applies migrations, seeds 12 sample tasks and creates the two demo accounts listed in `.env`. You can also register your own account with an `@company.com` address (change `ALLOWED_EMAIL_DOMAIN` to use another domain).

## Run locally for development

Prerequisites: .NET 8 SDK, Node 20+, and a SQL Server. The easiest SQL Server is the compose one:

```bash
docker compose up -d db
```

API (http://localhost:5080, Swagger at `/swagger`). `appsettings.Development.json` points at the compose database, applies migrations and creates the demo users:

```bash
dotnet run --project src/TaskFlow.Api
```

Web (http://localhost:5173, proxies `/api` to the API):

```bash
npm --prefix src/TaskFlow.Web install
```

```bash
npm --prefix src/TaskFlow.Web run dev
```

Demo accounts for local development are in `src/TaskFlow.Api/appsettings.Development.json`.

## Tests

.NET unit tests (SQLite in memory, no Docker needed):

```bash
dotnet test --filter "Category!=Integration"
```

With coverage (Cobertura XML under `TestResults/`; generated migrations are excluded):

```bash
dotnet test --filter "Category!=Integration" --settings coverage.runsettings --collect "XPlat Code Coverage" --results-directory TestResults
```

Raw-SQL summary against a real SQL Server in a throwaway container (needs Docker; skipped if unavailable). With Colima, first point Testcontainers at its socket: `export DOCKER_HOST=unix://$HOME/.colima/default/docker.sock TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/var/run/docker.sock`.

```bash
dotnet test --filter Category=Integration
```

React unit tests:

```bash
npm --prefix src/TaskFlow.Web test
```

## Project layout

| Path | What it is |
|---|---|
| `src/TaskFlow.Shared` | **netstandard2.0** library: enums, request/response contracts, validation attributes, field limits. Usable from .NET Framework 4.6.1+ and modern .NET alike. |
| `src/TaskFlow.Api` | ASP.NET Core 8 Web API: controllers, services, EF Core (Code First), Identity, JWT, raw SQL summary |
| `src/TaskFlow.Web` | React 18 + TypeScript + Vite; CSS Modules, TanStack Query, Radix primitives, dnd-kit |
| `tests/TaskFlow.Api.Tests` | xUnit: TaskService, AuthService, exception handler, SQL Server integration test |
| `tests/TaskFlow.Shared.Tests` | xUnit: shared validation and helpers, running the netstandard2.0 library on .NET 8 |
| `database/` | `task-summary.sql` (the raw summary query) and `seed.sql` (the sample data as a script) |

## API at a glance

| Method | Route | |
|---|---|---|
| POST | `/api/auth/register` · `/login` · `/refresh` · `/logout` | Accounts and sessions |
| GET | `/api/auth/me`, `/api/users` | Current user, assignee suggestions |
| GET | `/api/tasks?status=InProgress&priority=High&priority=Critical&search=…&sortBy=Priority&sortDir=Desc` | List (LINQ filtering and sorting) |
| GET | `/api/tasks/summary` | Counts by status × priority (raw SQL, `GROUPING SETS`) |
| GET · PUT · DELETE | `/api/tasks/{id}` | Read, replace, soft delete |
| POST | `/api/tasks` | Create |
| PATCH | `/api/tasks/{id}/status` | Change status only |

Every error is RFC 7807 `application/problem+json` with an `errorCode` and a `traceId`.

## Database migrations

Migrations live in `src/TaskFlow.Api/Data/Migrations`. To add one:

```bash
dotnet tool restore
```

```bash
dotnet ef migrations add <Name> --project src/TaskFlow.Api --output-dir Data/Migrations
```
