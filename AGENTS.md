# AGENTS.md

Repository-level instructions for coding agents working in this codebase.

## Purpose

Keep implementation style and architecture consistent so new features can be added safely and predictably.

## Architecture Boundaries

- Respect project layering:
  - `Listed.API`: HTTP transport, controllers, middleware, request/response contracts.
  - `Listed.Application`: use-case logic (commands/queries/handlers), contracts, result/error flow.
  - `Listed.Domain`: entities and domain rules only.
  - `Listed.Infrastructure`: EF Core persistence, repository implementations, security/runtime integrations.
- Do not leak infrastructure concerns into `Domain` or controller concerns into `Application`.

## API And Controller Rules

- Controllers should contain endpoint actions only.
- Do not add private helper methods inside controller files.
- Put shared endpoint helper logic in utility classes (for auth, use `Listed.API/Common/Utils/AuthUtils.cs`).
- Keep endpoint behavior explicit and map `Result` failures via error mappers.

## Application Layer Rules

- Use CQRS handler interfaces already in the repo (`ICommandHandler`, `IQueryHandler`).
- Keep business failures in `Result`/`Error` flow, not thrown exceptions.
- Validate inputs early in handlers.
- Keep handler methods readable with focused private methods where needed.

## Naming And Style Preferences

- Use clear, explicit names.
- Boolean-returning method names should be intention-revealing (prefer `Should...` / `Does...` / `Is...` / `Has...`).
- Avoid unnecessary abstraction and avoid over-engineering.
- Keep code clean and simple over clever.

## DI And Composition

- Register dependencies through extension methods:
  - `AddApplication()`
  - `AddInfrastructure()`
  - `AddApi()`
- Prefer interface-based injection.
- Avoid concrete type injection unless there is a strong reason.
- Keep DI registrations simple (`AddScoped<IService, Service>()` style when possible).

## Auth And Session Invariants

- Access token lifetime is 15 minutes.
- Refresh token lifetime is 30 days.
- Login policy:
  - Valid credentials should always create/rotate device refresh session.
  - Do not return same-device `409` conflict for missing/wrong login refresh cookie.
- Refresh policy:
  - Requires valid refresh token plus matching `device_id` session binding.
- Logout policy:
  - `logout` invalidates current session (refresh token + current access token `jti`).
  - `logout-all` invalidates all sessions and increments `auth_version`.

## Configuration And Environments

- Local developer runtime is Docker-based and uses `appsettings.Docker.json`.
- `appsettings.json` should keep non-local placeholders (no real secrets committed).
- In local Docker, client traffic should go through Nginx (`http://localhost:5000`).

## Persistence And Migrations

- Keep schema/configuration changes coherent with domain and repository behavior.
- Generate/apply EF migrations only when explicitly requested (and usually as final step after code/test updates).

## Tests And Validation

- Add/adjust tests with behavior changes.
- Prefer running:
  - `dotnet clean Listed.sln -v minimal`
  - `dotnet build Listed.sln -v minimal`
  - `dotnet test Listed.sln -v minimal`
- Keep test expectations aligned with current product behavior (not stale assumptions).

## Documentation

- Keep `README.md` updated when architecture, setup, or behavior changes.
- Keep docs practical and implementation-accurate.

