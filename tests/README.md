# Tests

## Purpose
This folder contains all automated tests for the solution, split by layer:
- unit tests for isolated behavior (`Domain`, `Application`)
- integration tests for runtime behavior (`API`, `Infrastructure`)

## Prerequisites
- .NET 10 SDK
- Docker Desktop running (required for integration tests using Testcontainers)

## Quick Commands
Run all tests:
```bash
dotnet test Listed.sln
```

Run only unit tests:
```bash
dotnet test Listed.sln --filter "Category=Unit"
```

Run only integration tests:
```bash
dotnet test Listed.sln --filter "Category=Integration"
```

## Project Structure
- `tests/Listed.Testing`
Purpose: shared test support code used across test projects.
Contains:
- shared factories/builders (`Factories/*`)
- shared infrastructure helpers (`Infrastructure/PostgresTestDatabase.cs`)

- `tests/Listed.Domain.UnitTests`
Purpose: domain invariant and entity behavior tests.
Pattern: no external infrastructure; pure in-memory unit tests.

- `tests/Listed.Application.UnitTests`
Purpose: use-case/handler logic tests with mocks.
Pattern: mock ports/contracts (repositories, hashers, etc.).

- `tests/Listed.Infrastructure.IntegrationTests`
Purpose: repository behavior against real PostgreSQL (Testcontainers).
Pattern: uses `InfrastructureDatabaseFixture` + `ListedDbContext`.

- `tests/Listed.API.IntegrationTests`
Purpose: HTTP endpoint behavior against full ASP.NET host + PostgreSQL (Testcontainers).
Pattern: uses `ApiWebApplicationFactory : WebApplicationFactory<Program>`.

## Factory Guidelines
Factories live in `tests/Listed.Testing/Factories` and provide valid defaults plus overrides.
Examples:
- `UserFactory.Valid(...)`
- `UserInfoFactory.Valid(...)`

When to use factories:
- use for baseline valid objects and repeated setup
- override only the field relevant to the test

When not to use factories:
- if a literal value makes intent clearer and there is no reuse pressure

Current convention in this repository is strict consistency: prefer factory usage even in invalid-case tests.

## Shared Infrastructure Helper
`tests/Listed.Testing/Infrastructure/PostgresTestDatabase.cs` centralizes:
- PostgreSQL container setup
- container start/dispose
- connection string access

This is reused by both:
- `ApiWebApplicationFactory`
- `InfrastructureDatabaseFixture`

Host-specific logic remains local to each test project (for example, API host bootstrapping in `ApiWebApplicationFactory`).

## Adding New Tests
For Domain:
1. Add/extend tests in `tests/Listed.Domain.UnitTests`.
2. Reuse factories from `Listed.Testing` when possible.
3. Mark with `[Trait("Category", "Unit")]`.

For Application:
1. Add tests in `tests/Listed.Application.UnitTests`.
2. Mock contracts/ports.
3. Use shared command/entity factories.
4. Mark with `[Trait("Category", "Unit")]`.

For Infrastructure integration:
1. Add tests in `tests/Listed.Infrastructure.IntegrationTests`.
2. Use `InfrastructureDatabaseFixture` and call reset per test lifecycle.
3. Mark with `[Trait("Category", "Integration")]`.

For API integration:
1. Add tests in `tests/Listed.API.IntegrationTests`.
2. Use `ApiWebApplicationFactory` and reset DB state per test lifecycle.
3. Mark with `[Trait("Category", "Integration")]`.

## Creating a New Test Project
1. Create project in `tests/`.
2. Use Central Package Management (no package versions in test csproj).
3. Add project to `Listed.sln`.
4. Add trait conventions (`Unit` or `Integration`).
5. Reference `tests/Listed.Testing` for shared factories/helpers when needed.
6. Update this README `Project Structure` section.

## Maintenance Checklist
- keep tests deterministic (reset DB state in integration tests)
- avoid cross-test shared mutable state
- keep shared helpers in `Listed.Testing` small and reusable
- keep host-specific fixture logic in the owning test project
- run `dotnet test Listed.sln` before merging
