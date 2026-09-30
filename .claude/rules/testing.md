---
paths:
  - "tests/**"
  - "src/TaskFlow.Web/src/test/**"
---

# Testing rules

- Write the failing test first, watch it fail for the right reason, then implement.
- Pick the lowest level that proves the behaviour:
  - Service logic → `tests/TaskFlow.Api.Tests/Services` with `TestHost` (fresh in-memory SQLite per test,
    seeded with the 13 migration rows, `FakeTimeProvider` fixed at 2026-09-28 12:00 UTC; move time with `Clock.Advance`).
  - HTTP contract (status, ProblemDetails shape, auth, cookies, routing) → `Api/ApiPipelineTests` via `ApiFactory`.
  - SQL Server-only behaviour (raw SQL, migrations) → `Integration/` with `[Trait("Category","Integration")]` and
    `[SkippableFact]`, using Testcontainers.MsSql.
  - Shared contracts and helpers → `tests/TaskFlow.Shared.Tests`.
  - Front-end logic → Vitest in `src/TaskFlow.Web/src/test`.
- Seed data is the fixture. Seeded tasks 1–12 are visible and task 13 is soft-deleted; assert on known ids and ordering,
  and don't assume an empty database.
- Use xUnit `Assert` only. Name tests `Method_Condition_ExpectedResult`.
- Never use the EF InMemory provider: it ignores relational behaviour such as query filters, constraints and translation.
- A test must not depend on another test's state or on wall-clock time. Use a new `TestHost` or a unique email per test.
- Before committing, run `dotnet test --filter "Category!=Integration"`. Also run `dotnet test --filter Category=Integration`
  whenever migrations or raw SQL changed.
