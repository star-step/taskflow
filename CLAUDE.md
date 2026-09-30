# TaskFlow

Internal task manager: .NET 8 API (`src/TaskFlow.Api`) on SQL Server, React 18 + Vite SPA (`src/TaskFlow.Web`),
netstandard2.0 contracts library (`src/TaskFlow.Shared`). Design and decisions: `docs/ARCHITECTURE.md` (read it
before changing behaviour); UI reference: `docs/mockups/index.html`. Path-scoped rules live in `.claude/rules/`.

## Commands (run from the repo root)

- Build (warnings are errors): `dotnet build`
- .NET unit + HTTP pipeline tests, no Docker: `dotnet test --filter "Category!=Integration"`
- SQL Server integration tests (Testcontainers; needs Docker): `dotnet test --filter Category=Integration`
  - With Colima: `export DOCKER_HOST=unix://$HOME/.colima/default/docker.sock TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/var/run/docker.sock`
- Web: `npm --prefix src/TaskFlow.Web test` (Vitest), `run typecheck`, `run lint` (0 warnings allowed), `run build`
- Whole stack: `docker compose up -d --build` → http://localhost:8080 (needs `.env`, copied from `.env.example`)
- Rebuild one service after a change: `docker compose up -d --build api` (or `web`)
- Smoke test a running stack: `.github/scripts/smoke-test.sh` · CI (`.github/workflows/ci.yml`) runs all of the above
- Local dev: `docker compose up -d db`, `dotnet run --project src/TaskFlow.Api` (:5080, Swagger), `npm --prefix src/TaskFlow.Web run dev` (:5173)
- New migration: `dotnet tool restore`, then `dotnet ef migrations add <Name> --project src/TaskFlow.Api --output-dir Data/Migrations`
- Vulnerability checks: `dotnet list package --vulnerable --include-transitive` and `npm --prefix src/TaskFlow.Web audit`

## Architecture rules

- One API project, layered by folder: `Controllers` → `Services` → `Data`. Do not split it into more projects.
- Controllers stay thin: bind, call a service, return. Business rules live in services.
- Services sit behind interfaces (`ITaskService`, `ITaskSummaryQuery`, `IAuthService`, `ITokenService`) and are registered
  in `Infrastructure/ServiceCollectionExtensions.cs`.
- EF Core for all data access except the summary. Raw SQL exists only in `Services/SqlTaskSummaryQuery.cs` (ADO.NET).
  Keep `database/task-summary.sql` identical to it. Raw SQL bypasses the soft-delete filter, so filter `IsDeleted = 0` yourself.
- Soft delete only: set `IsDeleted`; never remove task rows. The global query filter hides deleted rows.
- Mappings are hand-written (`Mapping/TaskMappings.cs`, projected in SQL via `Select`). No AutoMapper.
- Request/response contracts belong in `TaskFlow.Shared`. Keep it netstandard2.0: no `record`, no `init`, no newer APIs.
- Schema changes go through Code First migrations only; never edit an applied migration.

## Conventions

- Names: entity `TaskItem`, enum `TaskItemStatus` (avoids clashing with `System.Threading.Tasks`). Enums are stored as
  `tinyint`, with values ordered so that sorting by value sorts by urgency.
- Field limits come only from `TaskFieldLimits` and `AccountRules` in `TaskFlow.Shared`. Never repeat a literal length.
- Every error response is ProblemDetails with `errorCode` and `traceId`. Throw an `AppException` subclass
  (`Errors/`) with a code from `ErrorCodes`; don't build error bodies in controllers.
- Every async path takes a `CancellationToken` and passes it down to EF or SqlClient.
- Time comes from the injected `TimeProvider` (`AppDbContext.UtcNow()` in data code). Never `DateTime.UtcNow`.
  All stored times are UTC `datetime2(3)`.
- Secrets never go in committed config. They come from `.env`, env vars or user-secrets; `appsettings.Development.json`
  holds dev-only values.
- Front end: server state in TanStack Query, filters in the URL, overlays and dropdowns via the shared Radix-based
  components, styles from CSS Modules and `styles/tokens.css`. No CSS framework.

## Testing rules

- New behaviour or a bug fix starts with a failing test, then the code that makes it pass.
- Service tests use `TestHost` (in-memory SQLite, which is relational) with `FakeTimeProvider`. Never the EF InMemory provider.
- HTTP behaviour (status codes, error shape, auth, cookies) goes in `ApiPipelineTests` via `ApiFactory`.
- SQL Server-specific SQL gets a `[Trait("Category","Integration")]` Testcontainers test.
- Assert with xUnit `Assert`. No FluentAssertions (its v8 licence is commercial).
- Front-end logic (validation, API client, formatting) gets Vitest tests in `src/TaskFlow.Web/src/test`.

## Workflow

1. Plan before code: state the change, the files it touches and how it will be verified. Update `docs/ARCHITECTURE.md`
   when a decision changes.
2. Work in small steps. After every change, run `dotnet build`, the relevant tests and
   `npm --prefix src/TaskFlow.Web run typecheck`. Add `docker compose` and a browser check for UI or wiring changes.
3. Commit immediately after each validated change. Don't batch several changes into one commit. Use an imperative
   subject; the body says why, and ends with a `Verified:` line listing exactly what was run and its result
   (e.g. `Verified: dotnet build; dotnet test --filter "Category!=Integration" (51 passed); npm run typecheck`).
   Never commit a red build, `.env` or build output.
4. Report results honestly: say what was run and what wasn't.
