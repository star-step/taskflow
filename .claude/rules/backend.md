---
paths:
  - "src/TaskFlow.Api/**"
  - "src/TaskFlow.Shared/**"
---

# Backend rules (TaskFlow.Api, TaskFlow.Shared)

- Endpoint flow: controller (binding plus `[ProducesResponseType]` for every status) → `I*Service` → `AppDbContext`.
  Register new services in `Infrastructure/ServiceCollectionExtensions.cs`.
- Validation happens in two layers. DataAnnotations on the Shared DTOs (`[Required]`, `[StringLength(TaskFieldLimits.*)]`,
  `[DefinedEnum]`, `[NotWhiteSpace]`) run automatically. Business rules throw `DomainValidationException(field, message)`
  with camelCase field names.
- Errors: add a code to `ErrorCodes` and an `AppException` subclass; `GlobalExceptionHandler` shapes the response.
  Model-binding errors go through `ProblemDetailsSetup.ValidationResponse`, which is registered with `PostConfigure`
  because `AddControllers()` would otherwise overwrite it.
- Queries: use `AsNoTracking()` plus `Select(TaskMappings.ToResponse)` for reads, and keep filters and sorting in
  `IQueryable` so EF translates them to one SQL statement. Always add a stable tie-breaker to ordering.
- Never set `CreatedDate`/`ModifiedDate` by hand; `AppDbContext.SaveChangesAsync` stamps entities that implement
  `IHasTimestamps`.
- Auth: every endpoint needs a signed-in user unless it has `[AllowAnonymous]`. Cookie-authenticated endpoints need
  `[RequireClientHeader]`. Anonymous auth endpoints need `[EnableRateLimiting(RateLimits.*)]`. Read the user id from
  the `sub` claim (`MapInboundClaims = false`).
- Options classes live in `Options/` and are bound with `ValidateDataAnnotations().ValidateOnStart()`.
- After changing the model: add a migration, check the generated SQL types and indexes, and run the
  `Category=Integration` tests, which apply the migrations to a real SQL Server.
- Shared stays netstandard2.0-compatible. Anything added there must also compile for .NET Framework 4.6.1+ consumers.
