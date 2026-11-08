# EShopClaude — Build Plan

Source of truth: [PROJECT.md](PROJECT.md). This plan orders the work; PROJECT.md defines behaviour. If they disagree, PROJECT.md wins and this plan is fixed.

## How to use this plan
- One step = one feature branch off `main` = one PR. Branch name is listed per step.
- A step is done only when every item in its **Verify** list passes. Merge, then start the next step.
- Steps are sequential; later steps assume earlier ones are merged.
- Tasks are grouped by project so each change has a clear owner.
- Exception: the microservice steps (3.1, 3.2, 4.2) are spec backlogs. Each spec is its own branch and merge, per [WOW.md](WOW.md); the spec files hold the tasks and Definition of Done, and [microservices/README.md](microservices/README.md) holds dependencies and status.

## Verification toolkit
| Check | How |
|---|---|
| Build | `dotnet build src/EShopClaude.sln` — 0 errors, 0 new warnings |
| Tests | `dotnet test src/EShopClaude.sln` (Docker must be running) |
| Aspire run | `aspire run` from `src/` (uses `aspire.config.json`) → dashboard: all resources **Running/Healthy** |
| HTTP checks | Per-API `.http` file (VS Code / VS), or Postman CLI / curl against the endpoint shown in the dashboard |
| Traces | Dashboard → Traces: expected spans and cross-service calls present, no errors |
| Data | pgAdmin (from dashboard) for `catalogdb` / `orderingdb`; Redis via `redis-cli` in the container (dashboard → redis → console/exec) |
| Browser | Storefront URL from dashboard (`webfrontend`) |

Prerequisites: .NET 10 SDK, Aspire CLI, Docker Desktop running (from Phase 2 on).

## Overview
| # | Step | Branch | Main projects |
|---|---|---|---|
| 1.1 | Remove starter samples | `feature/skeleton-cleanup` | AppHost, Web, sln |
| 1.2 | Create three API projects + wire AppHost | `feature/skeleton-services` | Catalog.Api, Ordering.Api, Basket.Api, AppHost |
| 2.1 | Infra resources: Postgres, pgAdmin, DBs, Redis | `feature/data-infra` | AppHost (+ API csproj packages) |
| 3.1 | Catalog.Api spec backlog | `feature/catalog-NN-<short-name>` | Catalog.Api, AppHost |
| 3.2 | Ordering.Api spec backlog | `feature/ordering-NN-<short-name>` | Ordering.Api, AppHost |
| 4.1 | Resilience: no retries on unsafe methods | `feature/resilience-unsafe-methods` | ServiceDefaults |
| 4.2 | Basket.Api spec backlog | `feature/basket-NN-<short-name>` | Basket.Api, AppHost |
| 5.1 | Storefront foundation: clients, CartState, nav | `feature/web-foundation` | Web |
| 5.2 | Products page `/` | `feature/web-products` | Web |
| 5.3 | Cart page `/cart` | `feature/web-cart` | Web |
| 5.4 | Checkout page `/checkout` | `feature/web-checkout` | Web |
| 5.5 | Order page `/orders/{id}` | `feature/web-order` | Web |
| 6.1 | Aspire e2e test | `feature/e2e-test` | Tests |
| 6.2 | Full journey verification + docs | `feature/e2e-walkthrough` | README, `_agenticdev` |

---

## Phase 1 — Project skeleton

### 1.1 Remove starter samples
**Branch:** `feature/skeleton-cleanup`
**Touches:** EShopClaude.AppHost, EShopClaude.Web, `src/EShopClaude.sln`

Tasks
- **sln / repo**: remove `EShopClaude.ApiService` from the solution and delete the folder.
- **AppHost**: drop the `ApiService` project reference and the `apiservice` resource; `webfrontend` stays (temporarily no references).
- **Web**: delete `WeatherApiClient.cs` and its `AddHttpClient` registration in `Program.cs`; delete `Pages/Counter.razor`, `Pages/Weather.razor`, their links in `NavMenu.razor`.

Verify
- Build passes.
- `aspire run` → only `webfrontend` running and healthy; Home page loads in browser, nav has no Counter/Weather.

### 1.2 Create three API projects and wire AppHost
**Branch:** `feature/skeleton-services`
**Touches:** Catalog.Api, Ordering.Api, Basket.Api (new), AppHost, sln

Tasks
- **Catalog.Api / Ordering.Api / Basket.Api** (same pattern each): new empty ASP.NET Core minimal API `src/EShopClaude.<X>.Api`, added to sln, references ServiceDefaults; `AddServiceDefaults()`, `MapDefaultEndpoints()`; add `Microsoft.AspNetCore.OpenApi`, `AddOpenApi()` + `MapOpenApi()` in Development; empty `/api` route group; placeholder `.http` file with a `@host` variable.
- **AppHost**: project references; resources `catalogapi`, `orderingapi`, `basketapi`, each `.WithHttpHealthCheck("/health")`; `basketapi` `.WithReference(catalogapi).WithReference(orderingapi)`; `webfrontend` `.WithReference(...)` + `.WaitFor(...)` all three APIs (infra comes in 2.1).

Verify
- Build passes.
- `aspire run` → four project resources Running/Healthy.
- HTTP: `GET /health` → 200 and `GET /openapi/v1.json` → 200 on each API.
- Dashboard → `basketapi` / `webfrontend` env vars show `services__catalogapi__…` etc. (service discovery wired).

---

## Phase 2 — Data persistence

### 2.1 Infrastructure resources
**Branch:** `feature/data-infra`
**Touches:** AppHost, Catalog.Api, Ordering.Api, Basket.Api (csproj only)

No API ↔ infra wiring in this step: APIs do not reference the infra resources yet (`WithReference`/`WaitFor` come in 2.2–2.4). All Aspire packages use version 13.6.4 (= AppHost SDK).

Tasks
- **2.1a AppHost hosting packages**: add `Aspire.Hosting.PostgreSQL` and `Aspire.Hosting.Redis` to `EShopClaude.AppHost.csproj`. Build.
- **2.1b Postgres + databases**: `postgres` server (no data volume); databases `catalogdb`, `orderingdb` via `postgres.AddDatabase(...)`. Build.
- **2.1c pgAdmin**: `.WithPgAdmin()` on `postgres`. Build.
- **2.1d Redis**: `redis` (no data volume). Build.
- **2.1e Client packages (packages only, no code)**: `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` in Catalog.Api and Ordering.Api; `Aspire.StackExchange.Redis` in Basket.Api. Build.
- **2.1f Run + verify**: `aspire run`, walk the Verify list.

Verify
- Build passes (0 errors, 0 new warnings).
- `dotnet list src/EShopClaude.sln package` → the four Aspire packages at 13.6.4, only in the projects above. No `AddNpgsqlDbContext` / `AddRedisClient` / infra `WithReference` in code.
- `aspire run` → `postgres`, `pgadmin`, `catalogdb`, `orderingdb`, `redis` Running/Healthy; the four projects still Running/Healthy; API env vars contain no `ConnectionStrings__*`.
- pgAdmin opens from dashboard; both databases listed (empty).
- Redis: `redis-cli PING` → `PONG`, `DBSIZE` → `0`.
- Restart AppHost → containers recreated, no volumes, no persisted data (ephemeral).

---

## Phase 3 — Microservices

Each service is built as a backlog of small specs, in file-number order. The spec files are the source of truth for tasks and Definition of Done.

### 3.1 Catalog.Api
**Owns:** `catalogdb` and the read-only product catalog (list, get by id), seeded on startup.
**Specs:** [microservices/catalog-api/](microservices/catalog-api/)
- `01-persistence.md`
- `02-seed-products.md`
- `03-list-products.md`
- `04-get-product.md`

### 3.2 Ordering.Api
**Owns:** `orderingdb` and plain CRUD over orders. Trusts its caller; makes no outgoing calls.
**Specs:** [microservices/ordering-api/](microservices/ordering-api/)
- `01-persistence.md`
- `02-create-order.md`
- `03-get-order.md`
- `04-list-orders.md`
- `05-update-order.md`
- `06-delete-order.md`

---

## Phase 4 — Service communication and checkout

### 4.1 Resilience: no retries on unsafe methods
**Branch:** `feature/resilience-unsafe-methods`
**Touches:** ServiceDefaults

Tasks
- **ServiceDefaults**: in `Extensions.cs`, configure `AddStandardResilienceHandler(o => o.Retry.DisableForUnsafeHttpMethods())`. Nothing else changes.

Verify
- Build passes; `aspire run` → all resources healthy.
- (Behaviour observed in basket-api `05-checkout.md`: a failing POST to Ordering shows a single attempt in traces.)

### 4.2 Basket.Api
**Owns:** Redis baskets (`basket:{buyerId}`, 30-day TTL) and checkout orchestration. Calls Catalog on add item and Ordering on checkout.
**Specs:** [microservices/basket-api/](microservices/basket-api/)
- `01-redis-connection.md`
- `02-get-basket.md`
- `03-remove-item.md`
- `04-add-item.md`
- `05-checkout.md` (runs after 4.1)

---

## Phase 5 — Storefront pages

### 5.1 Storefront foundation
**Branch:** `feature/web-foundation`
**Touches:** Web

Tasks
- **Web**: global `InteractiveServer` on `Routes` and `HeadOutlet` in `App.razor`.
- Own DTO records; typed clients `CatalogClient` (`https+http://catalogapi`), `BasketClient` (`https+http://basketapi`), `OrderingClient` (`https+http://orderingapi`) registered in `Program.cs`.
- `const string BuyerId = "demo-user"` in one place.
- `CartState` (scoped): item count, `Changed` event, `LoadAsync` from `GET basket`.
- `NavMenu`: **Products** (`/`), **Cart (n)** (`/cart`) subscribed to `CartState.Changed`.

Verify
- Build passes.
- Browser: nav shows Products and Cart (0); pre-filling the basket via `basketapi.http` for `demo-user` then reloading shows Cart (n).
- Traces: page load → `GET basketapi /api/basket/demo-user`.

### 5.2 Products page `/`
**Branch:** `feature/web-products`
**Touches:** Web

Tasks
- **Web**: replace `Home.razor` with product cards (name, description, price) from `CatalogClient`; **Add to cart** → `POST items` qty 1, show "Added", update `CartState`; stay on page.

Verify
- Browser: ~6 cards; Add → "Added" feedback, nav count increments; adding same product twice → count reflects quantity.
- Traces: Add click → `webfrontend` → `basketapi` → `catalogapi`.

### 5.3 Cart page `/cart`
**Branch:** `feature/web-cart`
**Touches:** Web

Tasks
- **Web**: `Cart.razor` lines (name, unit price, qty, line total) + basket total; **Remove** per line → update `CartState`; **Checkout** link hidden when empty; empty state text + link to `/`.

Verify
- Browser: totals match `GET basket` via `.http`; Remove updates list, total and nav count; empty basket shows empty state, no Checkout link.

### 5.4 Checkout page `/checkout`
**Branch:** `feature/web-checkout`
**Touches:** Web

Tasks
- **Web**: `Checkout.razor` basket summary + `EditForm` with DataAnnotations (Name required, Email required + email format); **Place order** → `POST checkout` → `CartState` count 0 → navigate `/orders/{orderId}`; empty basket → message + link to `/`.

Verify
- Browser: submit empty form → validation messages; bad email → message; valid → redirect to `/orders/{id}` (404 page acceptable until 5.5), nav Cart (0).
- `/checkout` with empty basket → message + link.
- pgAdmin: new order row with entered name/email.

### 5.5 Order page `/orders/{id}`
**Branch:** `feature/web-order`
**Touches:** Web

Tasks
- **Web**: `Order.razor` via `OrderingClient` GET by id: order id, date, name, email, lines, total; link back to `/`. Unknown id → simple "not found" message.

Verify
- Browser: after checkout, page shows correct data matching pgAdmin; `/orders/999` → not-found message.
- Traces: `webfrontend` → `GET orderingapi /api/orders/{id}`.

---

## Phase 6 — End-to-end verification

### 6.1 Aspire e2e test
**Branch:** `feature/e2e-test`
**Touches:** EShopClaude.Tests (new), sln

Tasks
- **Tests**: new xUnit project `src/EShopClaude.Tests` with `Aspire.Hosting.Testing`, references AppHost, added to sln.
- One test using `DistributedApplicationTestingBuilder`, waiting for resources healthy, unique test buyerId: products non-empty → add first product qty 2 → checkout 201 + orderId → order has 1 line, qty 2, total = price × 2 → basket empty (PROJECT.md §9).

Verify
- `dotnet test src/EShopClaude.sln` passes with Docker running; runs twice in a row green (ephemeral data).

### 6.2 Full journey verification + docs
**Branch:** `feature/e2e-walkthrough`
**Touches:** README.md, `_agenticdev/`

Tasks
- Fresh `aspire run`, then walk the journey in the browser: `/` → Add 2 products (one twice) → `/cart` → remove one line → `/checkout` → validation errors → place order → `/orders/{id}`.
- Cross-check: pgAdmin order + lines; Redis `basket:demo-user` gone; dashboard traces show Web→Basket→Catalog (add) and Web→Basket→Ordering (checkout) with no errors; structured logs clean.
- Run each `.http` file top to bottom once more.
- README: how to run (`aspire run`), prerequisites, the four use cases, how to run the test. Tick off this plan.

Verify
- Every check above passes; `dotnet build` + `dotnet test` green; checklist recorded in the PR description.
