# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Minimal distributed e-commerce demo on .NET 10 + Aspire (AppHost SDK 13.6.4), built with spec-driven development. Four use cases: list products, add to cart, checkout, see the order. No login, payments or deployment.

**Spec documents are authoritative:**
- `_agenticdev/PROJECT.md` defines behaviour: routes, DTOs, data models, checkout sequence, and the decision log. If code and spec disagree, the spec wins.
- `_agenticdev/PLAN.md` orders the work into steps. Each step has its own branch name (`feature/...`), task list and **Verify** checklist. One step = one branch off `main` = one PR. A step is done only when every Verify item passes. If PLAN.md and PROJECT.md disagree, PROJECT.md wins and the plan gets fixed.

Read the relevant step in PLAN.md before you start work. Section 10 of PROJECT.md lists deliberately excluded features (shared contracts library, EF migrations, data volumes, message broker, custom retries, unit tests, etc.). Don't add any of them.

## Commands

The solution lives in `src/`. Docker Desktop must be running once infra exists (Phase 2+).

```sh
dotnet build src/EShopClaude.sln          # must be 0 errors, 0 new warnings
dotnet test src/EShopClaude.sln           # Aspire e2e test; needs Docker
dotnet test src/EShopClaude.sln --filter "FullyQualifiedName~<TestName>"   # single test
cd src && aspire run                      # runs AppHost via src/aspire.config.json; opens dashboard
```

For manual HTTP checks, use each API's `.http` file (`src/EShopClaude.<X>.Api/EShopClaude.<X>.Api.http`), the Postman CLI, or curl. Take the endpoint from the Aspire dashboard. When a project runs standalone, its launchSettings ports are: Catalog 5101, Basket 5102, Ordering 5103, Web 5093. OpenAPI is at `/openapi/v1.json` (Development only). `/health` and `/alive` are mapped only in Development.

## Architecture

```
webfrontend (Blazor, global InteractiveServer)
  ├─► catalogapi  ── catalogdb (Postgres)       read-only products, seeded on startup
  ├─► basketapi   ── redis                      basket + checkout orchestration
  │     ├─► catalogapi   GET /api/products/{id} on add-item (snapshot name+price)
  │     └─► orderingapi  POST /api/orders at checkout, then deletes basket
  └─► orderingapi ── orderingdb (Postgres)      plain CRUD, trusts caller, no outgoing calls
```

- **Synchronous HTTP only**, via Aspire service discovery (`https+http://<resource>`). There is no gateway, broker or events. Catalog and Ordering never call other services.
- **Resource names** (`catalogapi`, `basketapi`, `orderingapi`, `webfrontend`, `postgres`, `catalogdb`, `orderingdb`, `redis`) are wired in `src/EShopClaude.AppHost/AppHost.cs`. Service discovery and connection strings depend on them, so names must match exactly.
- **Basket.Api owns checkout**: it reads the basket, POSTs the order to Ordering, and deletes the basket only on 201. If the call fails, it returns 500 and leaves the basket untouched, with no retry or compensation.
- **ServiceDefaults** (`src/EShopClaude.ServiceDefaults/Extensions.cs`) supplies OpenTelemetry, health checks, service discovery and the standard resilience handler to every service through `AddServiceDefaults()` / `MapDefaultEndpoints()`. The only planned change is `Retry.DisableForUnsafeHttpMethods()`, so POSTs that time out don't create duplicate orders.
- **Persistence is ephemeral**: no data volumes, so every AppHost run starts clean. The e2e test relies on this.

## Conventions

- Minimal APIs, with every route under the `/api` route group already created in each `Program.cs`.
- **Contracts are duplicated per service.** Each API and the Web project declare their own small DTO records. Don't create a shared contracts project.
- EF Core with `Database.EnsureCreated()` at startup. No migrations.
- Money is `decimal`, in one implicit currency. Totals are computed, never trusted from input: the basket total is computed on read, and Ordering computes `Total` from the lines.
- Basket Redis key is `basket:{buyerId}`, stored as JSON with a 30-day TTL that resets on every write. The storefront uses the constant buyer id `demo-user`.
- Aspire hosting/client packages use the same version as the AppHost SDK.
- Every API keeps `AddOpenApi()` + `MapOpenApi()` (Development) and one `.http` file in its project folder. Update the `.http` file when you add endpoints.

## Current state

Phase 1 (skeleton) is complete. The three APIs exist with empty `/api` groups and are wired in AppHost without infra. Postgres/Redis, data models, endpoints, the Web storefront pages, and the `EShopClaude.Tests` project come in later PLAN.md steps.
