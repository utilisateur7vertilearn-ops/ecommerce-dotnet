# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Run the entire application (from the repo root, next to ECommerce.slnx)
dotnet run --project src/ECommerce.AppHost

# Build without running
dotnet build

# Build a single project
dotnet build src/ECommerce.Catalog.Api

# Run all tests
dotnet test ECommerce.slnx --configuration Release

# Run tests for a single project
dotnet test tests/ECommerce.Catalog.Tests

# Run a single test class or method (filter by display name)
dotnet test tests/ECommerce.Catalog.Tests --filter "FullyQualifiedName~GetProductsTests"

# Trust the dev HTTPS certificate (required once per machine)
dotnet dev-certs https --trust

# Run without Aspire (Docker Compose — no dashboard, plain HTTP on fixed ports)
docker compose up --build
# Catalog: http://localhost:5001  Ordering: http://localhost:5002
# Gateway: http://localhost:5000  Web:     http://localhost:5003
```

After starting with Aspire, the terminal prints a **Login URL** (`https://localhost:<port>/login?t=<token>`). Open that URL to reach the Aspire dashboard. The port is dynamic — never hardcode it. From the dashboard, click the `web` resource to reach the Blazor UI.

## Architecture

Six projects orchestrated by **.NET Aspire** (`ECommerce.AppHost`). The startup order is declared in [AppHost.cs](src/ECommerce.AppHost/AppHost.cs): Catalog starts first, then Ordering (which waits for Catalog), then Gateway (which waits for both), then Web (which waits for Gateway).

```
Web (Blazor) → Gateway (YARP) → Catalog API
                              → Ordering API → Catalog API (product validation)
```

- **ECommerce.AppHost** — the only project you launch; declares the service graph via Aspire's `DistributedApplication` builder.
- **ECommerce.ServiceDefaults** — a shared library (not a runnable service) that every other project references. Calling `builder.AddServiceDefaults()` wires up OpenTelemetry (traces + metrics + logs), health check endpoints (`/health`, `/alive`), standard HTTP resilience handlers, and Aspire service discovery on all `HttpClient` instances.
- **ECommerce.Gateway** — a thin YARP reverse proxy with no custom code. Routing rules live entirely in [appsettings.json](src/ECommerce.Gateway/appsettings.json): `/catalog/{**}` strips the prefix and forwards to `catalog`, `/ordering/{**}` forwards to `ordering`.
- **ECommerce.Catalog.Api** — minimal API. EF Core with an in-memory database seeded with 4 demo products on startup. Endpoints in [CatalogEndpoints.cs](src/ECommerce.Catalog.Api/Endpoints/CatalogEndpoints.cs).
- **ECommerce.Ordering.Api** — minimal API. EF Core with an in-memory database. When creating an order, it calls Catalog synchronously via the typed `CatalogServiceClient` to validate every product. Endpoints in [OrderingEndpoints.cs](src/ECommerce.Ordering.Api/Endpoints/OrderingEndpoints.cs).
- **ECommerce.Web** — Blazor Server (interactive server render mode). Two typed `HttpClient`s (`CatalogApiClient`, `OrderingApiClient`) both pointing at `https+http://gateway`; the clients call `/catalog/api/...` and `/ordering/api/...` respectively.

## Service discovery

Services never hardcode addresses. Aspire resolves `https+http://<resource-name>` URIs at runtime using the names declared in `AppHost.cs` (e.g. `"catalog"`, `"ordering"`, `"gateway"`). Adding a new inter-service call means registering a typed `HttpClient` with `new Uri("https+http://<resource-name>")` as the base address — service discovery kicks in automatically via `AddServiceDefaults()`.

## Data persistence

Both databases are **in-memory only** (EF Core `UseInMemoryDatabase`). All data is lost on restart; Catalog is re-seeded from `CatalogDbContext.OnModelCreating`. To add persistence, swap `UseInMemoryDatabase` for a real provider and add the corresponding Aspire resource in `AppHost.cs`.

## Adding API endpoints

Follow the minimal-API pattern already used: add a static `MapXxxEndpoints` extension method on `IEndpointRouteBuilder` (see [CatalogEndpoints.cs](src/ECommerce.Catalog.Api/Endpoints/CatalogEndpoints.cs)), call it from `Program.cs`, and declare request/response shapes as `record` types in the same file.

## Tests

Integration tests live in `tests/ECommerce.Catalog.Tests/`. They use `WebApplicationFactory<Program>` (xUnit + `Microsoft.AspNetCore.Mvc.Testing`) and replace the EF Core provider with a fresh `UseInMemoryDatabase` instance per factory. Each test class receives the factory via `IClassFixture<CatalogApiFactory>` — the factory is shared within the class but isolated across classes by using a `Guid`-named DB.

When adding tests for a new API project, follow the same pattern: create a `WebApplicationFactory` subclass that swaps the `DbContextOptions` descriptor, then group related endpoint tests into classes that share one factory instance.

## CI / CD

Two GitHub Actions workflows in `.github/workflows/`:

- **ci.yml** — triggers on every push/PR to `main`. Two parallel jobs: `build-and-test` (restore → build Release → `dotnet test`) and `docker-scan` (build each Docker image, then scan with Trivy for CRITICAL/HIGH CVEs; fails the job if any are found).
- **cd.yml** — deployment pipeline with a manual gate before the production stage.

Docker images are built with the context set to the repo root (not the service subdirectory) because the `COPY` instructions in each Dockerfile reference sibling projects (e.g., `ECommerce.ServiceDefaults`).
