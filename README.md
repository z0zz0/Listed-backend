# Listed Backend

Work in progress. This document describes the current backend architecture and local developer setup so new functionality can be added consistently.

## Architecture

This solution follows a layered architecture (Domain -> Application -> Infrastructure -> API):

```text
Listed.API
  -> Listed.Application
       -> Listed.Domain
  -> Listed.Infrastructure
       -> Listed.Application
       -> Listed.Domain
```

Projects:

- `src/Listed.Domain`
  - Core entities, enums, and domain rules.
  - No framework dependencies.
- `src/Listed.Application`
  - Use-case orchestration via commands/queries and handlers.
  - Contracts (ports) for persistence/security abstractions.
  - Shared `Result`/`Error` primitives for predictable flow.
- `src/Listed.Infrastructure`
  - EF Core persistence, repository implementations, and security implementations.
  - PostgreSQL + Valkey integrations.
- `src/Listed.API`
  - HTTP entry points (controllers), middleware, auth setup, and DI composition.

## Design Patterns And Conventions

- CQRS-style handlers:
  - Commands via `ICommandHandler<TCommand, TResult>`.
  - Queries via `IQueryHandler<TQuery, TResult>`.
- Repository pattern:
  - Application depends on interfaces in `Listed.Application.Contracts`.
  - Infrastructure provides implementations.
- Explicit success/failure flow:
  - Handlers return `Result` / `Result<T>` instead of throwing for expected validation/business failures.
  - API maps `Error` codes to HTTP responses through error mappers.
- Composition-root extensions:
  - `AddApplication()`, `AddInfrastructure()`, `AddApi()` wire each layer from `Program.cs`.
- Configuration via options:
  - `AuthOptions` is validated at startup (required fields, minimum signing key length, lifetimes).

## Tech Stack

- Runtime:
  - .NET 10
  - ASP.NET Core Web API
- Persistence:
  - EF Core 10
  - PostgreSQL (Npgsql provider)
- Security/Auth:
  - JWT bearer authentication
  - BCrypt password hashing
  - Refresh-token rotation with hashed token storage
  - Valkey-backed auth state (revoked access token JTIs, revoked session IDs, user auth version cache)
- Distributed/runtime:
  - Valkey (StackExchange.Redis client)
  - Nginx reverse proxy + round-robin load balancing to two API containers
  - Serilog + Seq logging
- Testing:
  - xUnit, Moq
  - ASP.NET `WebApplicationFactory`
  - Testcontainers for PostgreSQL and Valkey

## Current Auth Design

- Access token:
  - JWT, lifetime: 15 minutes (`Auth:AccessTokenLifetimeMinutes`).
  - Includes claims like `sub`, `sid`, `jti`, `auth_version`.
- Refresh token:
  - Opaque token issued in HttpOnly cookie.
  - Only hash stored in DB.
  - Lifetime: 30 days (`Auth:RefreshTokenLifetimeDays`).
- Device-scoped refresh sessions:
  - `device_id` is tracked and persisted.
  - Unique active session enforced per `(user_id, device_id)` while `revoked_at IS NULL`.
  - Unique active refresh token enforced per `session_id` while `revoked_at IS NULL`.
- Immediate invalidation:
  - Logout current session revokes current refresh token and revokes session (`sid`) in Valkey until token expiry (plus current token `jti`).
  - Logout-all revokes all refresh tokens and increments `auth_version`; stale access tokens fail validation immediately.

## Local Get-Started Guide

### 1. Prerequisites

- Docker Desktop running
- Optional: .NET 10 SDK (only needed if you want to run `dotnet` commands from host, such as `dotnet test` or host-side EF commands)

### 2. Start the local stack

From repository root:

```bash
docker compose up --build
```

Services exposed locally:

- API (through Nginx): `http://localhost:5000`
- PostgreSQL: `localhost:5432`
- pgAdmin: `http://localhost:5050`
- Seq: `http://localhost:5341`
- Valkey: `localhost:6379`

Notes:

- Local environment is driven by `src/Listed.API/appsettings.Docker.json`.
- API containers run with `dotnet watch`, so code changes in mounted source files are hot-reloaded.

### 3. Apply database migrations

Recommended path is running EF inside the API container (avoids host/container DNS differences):

```bash
docker compose exec listed.api.1 dotnet ef database update --project /app/Listed.Infrastructure --startup-project /app/Listed.API --context ListedDbContext
```

### 4. Verify the app

- Call endpoints via Nginx (`http://localhost:5000/...`), not directly to internal API container ports.
- Example auth endpoint: `POST http://localhost:5000/api/auth/login`

### 5. Run tests

From repository root:

```bash
dotnet test Listed.sln
```

Detailed testing guidance lives in `tests/README.md`.

## Configuration Model

- `src/Listed.API/appsettings.json`
  - Base configuration with deployment placeholders for non-local environments.
- `src/Listed.API/appsettings.Docker.json`
  - Local developer overrides (DB, Valkey, local signing key, local data-protection settings).

Important:

- Non-local environments should inject real values via environment variables/secrets.
- `Auth:SigningKey` must be at least 32 characters.

## Extending The Codebase

When adding a new feature, follow this order:

1. Add/adjust domain model in `Listed.Domain` (entities/value rules).
2. Add application contracts + command/query + handler in `Listed.Application`.
3. Implement infrastructure adapters (repository/security/etc.) in `Listed.Infrastructure`.
4. Expose API contracts/endpoints in `Listed.API`.
5. Add tests:
   - Unit tests for domain/application logic.
   - Integration tests for infrastructure and API behavior.

## Current Features (WIP)

- Create user:
  - `POST /api/users`
  - Creates a user with hashed password and initial auth metadata.
- Get user by email:
  - `GET /api/users/by-email?email=...`
- Login:
  - `POST /api/auth/login`
  - Validates credentials, issues access token, and creates/rotates the device refresh session.
  - Writes refresh-token cookie for the current session.
- Refresh access token:
  - `POST /api/auth/refresh`
  - Rotates refresh token and returns new access token.
- Logout (current session):
  - `POST /api/auth/logout`
  - Revokes current refresh token + immediately invalidates all access tokens from the same session.
- Logout all sessions:
  - `POST /api/auth/logout-all`
  - Revokes all refresh tokens for user + bumps `auth_version` for immediate global invalidation.
- Current authenticated user:
  - `GET /api/auth/me`
- Local reverse proxy + load balancing:
  - Nginx routes to `listed.api.1` and `listed.api.2`.

## Troubleshooting

### 1) `dotnet ef` says "No project was found"

Cause:

- Command is being run from the wrong working directory or without explicit project paths.

Fix:

```bash
dotnet ef database update --project src/Listed.Infrastructure --startup-project src/Listed.API --context ListedDbContext
```

### 2) `dotnet ef` fails with missing connection strings or wrong environment

Cause:

- `appsettings.Docker.json` is not loaded unless environment is `Docker`.

Fix on Windows `cmd`:

```cmd
set ASPNETCORE_ENVIRONMENT=Docker
set DOTNET_ENVIRONMENT=Docker
dotnet ef database update --project src/Listed.Infrastructure --startup-project src/Listed.API --context ListedDbContext
```

### 3) `No such host is known` for `postgresql` when running EF from host

Cause:

- `postgresql` is a Docker-internal DNS name; it resolves inside containers, not on the host OS shell.

Fix:

- Preferred: run EF inside API container:

```bash
docker compose exec listed.api.1 dotnet ef database update --project /app/Listed.Infrastructure --startup-project /app/Listed.API --context ListedDbContext
```

- Alternative: use a host-resolvable connection string for host-side commands.

### 4) `http://localhost:5000` returns 404

Cause:

- API has no `/` route.

Fix:

- Call an actual endpoint, for example:
  - `POST http://localhost:5000/api/users`
  - `POST http://localhost:5000/api/auth/login`

### 5) Login works but authorized auth endpoints return `401`

Cause:

- JWT claim-name mapping can break direct reads of `sub`/`jti`/`exp`.

Fix:

- Keep `options.MapInboundClaims = false;` in JWT bearer setup (`src/Listed.API/Extensions/ApiServiceCollectionExtensions.cs`).

### 6) Migration exists but login fails with DB column/constraint mismatch

Cause:

- Database schema is behind current model changes.

Fix:

```bash
docker compose exec listed.api.1 dotnet ef database update --project /app/Listed.Infrastructure --startup-project /app/Listed.API --context ListedDbContext
```

Then verify migration history contains latest entries in `listed.__EFMigrationsHistory`.

### 7) Port confusion in local Docker

Current host ports:

- API via Nginx: `5000`
- PostgreSQL: `5432`
- pgAdmin: `5050`
- Seq: `5341`
- Valkey: `6379`

Notes:

- Client applications should call `http://localhost:5000` (Nginx), not API container-internal ports directly.
