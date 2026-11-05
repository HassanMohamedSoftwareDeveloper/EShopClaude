# EShopClaude — Build Plan

Source of truth: [PROJECT.md](PROJECT.md). This plan orders the work; PROJECT.md defines behaviour. If they disagree, PROJECT.md wins and this plan is fixed.

## How to use this plan
- One step = one feature branch off `main` = one PR. Branch name is listed per step.
- A step is done only when every item in its **Verify** list passes. Merge, then start the next step.
- Steps are sequential; later steps assume earlier ones are merged.
- Tasks are grouped by project so each change has a clear owner.

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
| 2.1 | Infra resources: Postgres, pgAdmin, DBs, Redis | `feature/data-infra` | AppHost |
| 2.2 | Catalog data model + seed | `feature/data-catalog` | Catalog.Api |
| 2.3 | Ordering data model | `feature/data-ordering` | Ordering.Api |
| 2.4 | Basket Redis store | `feature/data-basket` | Basket.Api |
| 3.1 | Catalog.Api endpoints | `feature/catalog-api` | Catalog.Api |
| 3.2 | Ordering.Api CRUD endpoints | `feature/ordering-api` | Ordering.Api |
| 3.3 | Basket.Api read + remove endpoints | `feature/basket-api` | Basket.Api |
| 4.1 | Resilience: no retries on unsafe methods | `feature/resilience-unsafe-methods` | ServiceDefaults |
| 4.2 | Basket → Catalog: add item | `feature/basket-add-item` | Basket.Api |
| 4.3 | Basket → Ordering: checkout | `feature/basket-checkout` | Basket.Api |
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
**Touches:** AppHost

Tasks
- **AppHost**: add `Aspire.Hosting.PostgreSQL` and `Aspire.Hosting.Redis` (version = AppHost SDK 13.6.4).
- `postgres` server (no data volume) `.WithPgAdmin()`; databases `catalogdb`, `orderingdb`; `redis` (no data volume).
- `catalogapi` `.WithReference(catalogDb).WaitFor(catalogDb)`; `orderingapi` same with `orderingDb`; `basketapi` `.WithReference(redis).WaitFor(redis)`. Matches AppHost block in PROJECT.md §2.

Verify
- Build passes.
- `aspire run` → `postgres`, `pgadmin`, `catalogdb`, `orderingdb`, `redis` healthy; APIs start only after their dependency is healthy.
- pgAdmin opens from dashboard; both databases listed (empty).
- Restart AppHost → containers recreated, no persisted data (ephemeral).

### 2.2 Catalog data model + seed
**Branch:** `feature/data-catalog`
**Touches:** Catalog.Api

Tasks
- **Catalog.Api**: add `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`; `Product` entity (Id, Name, Description, Price `decimal`); `CatalogDbContext`; register with `AddNpgsqlDbContext<CatalogDbContext>("catalogdb")`.
- Startup: `EnsureCreated()`, then seed ~6 products if table empty (idempotent).

Verify
- Build passes.
- `aspire run` → `catalogapi` healthy; pgAdmin: `catalogdb` has `Products` table with ~6 rows.
- Restart `catalogapi` only (dashboard) → still ~6 rows (no duplicate seed).
- Traces/logs: EF Core DB command spans visible for `catalogapi`.

### 2.3 Ordering data model
**Branch:** `feature/data-ordering`
**Touches:** Ordering.Api

Tasks
- **Ordering.Api**: add `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`; entities `Order` (Id identity, BuyerId, CustomerName, CustomerEmail, CreatedAt `DateTimeOffset` UTC, Total `decimal`, Lines) and `OrderLine` (Id, OrderId FK, ProductId, ProductName, UnitPrice, Quantity); `OrderingDbContext` with cascade from Order to Lines; register with `"orderingdb"`.
- Startup: `EnsureCreated()`. No seed, no status field.

Verify
- Build passes.
- `aspire run` → pgAdmin: `orderingdb` has `Orders` and `OrderLines` tables, FK present, empty.

### 2.4 Basket Redis store
**Branch:** `feature/data-basket`
**Touches:** Basket.Api

Tasks
- **Basket.Api**: add `Aspire.StackExchange.Redis`; `AddRedisClient("redis")`.
- Models `CustomerBasket { buyerId, items }`, `BasketItem { productId, productName, unitPrice, quantity }`.
- Basket repository: get / save (key `basket:{buyerId}`, JSON, TTL 30 days reset on every save) / delete.

Verify
- Build passes.
- `aspire run` → `basketapi` healthy; Redis health check green in dashboard.
- (Repository exercised by endpoints in 3.3; key/TTL checked there.)

---

## Phase 3 — Microservices

### 3.1 Catalog.Api endpoints
**Branch:** `feature/catalog-api`
**Touches:** Catalog.Api

Tasks
- **Catalog.Api**: `ProductDto { id, name, description, price }`; `GET /api/products` (ordered by Id) and `GET /api/products/{id}` (200 / 404). No write endpoints.
- `.http` file: list, get existing id, get missing id.

Verify
- Build passes.
- HTTP: list → 200 with ~6 items ordered by id; `/api/products/1` → 200; `/api/products/999` → 404.
- OpenAPI doc lists both routes.
- Traces: request span → EF query span, no errors.

### 3.2 Ordering.Api CRUD endpoints
**Branch:** `feature/ordering-api`
**Touches:** Ordering.Api

Tasks
- **Ordering.Api**: own records `OrderRequest { buyerId, customerName, customerEmail, items[] }`, `OrderDto` (id, buyerId, customerName, customerEmail, createdAt, total, lines).
- `GET /api/orders` (ordered by Id), `GET /api/orders/{id}` (200/404), `POST /api/orders` (201 + `Location: /api/orders/{id}`, `CreatedAt` = UTC now, `Total` = Σ unitPrice × quantity), `PUT /api/orders/{id}` (replace customer data + lines, recompute Total; 200/404), `DELETE /api/orders/{id}` (204/404).
- Trusts caller: no Catalog call, no validation beyond model binding.
- `.http` file covering all five operations plus 404 cases.

Verify
- Build passes.
- HTTP: POST → 201 with Location and correct Total; GET by id matches; list contains it; PUT changes lines and Total; DELETE → 204, then GET → 404; PUT/DELETE on missing id → 404.
- pgAdmin: rows in `Orders`/`OrderLines` match; lines removed after DELETE.

### 3.3 Basket.Api read + remove endpoints
**Branch:** `feature/basket-api`
**Touches:** Basket.Api

Tasks
- **Basket.Api**: `BasketDto { buyerId, items, total }` with `total` computed on read.
- `GET /api/basket/{buyerId}` → 200, empty items when key missing.
- `DELETE /api/basket/{buyerId}/items/{productId}` → drop line (no-op if absent), save, 200 BasketDto.
- `.http` file: get empty basket, remove from empty basket (add/checkout requests added in 4.2/4.3).

Verify
- Build passes.
- HTTP: GET unknown buyer → 200 `{ items: [], total: 0 }`; DELETE on empty basket → 200 empty.
- Redis: after DELETE on empty basket, key `basket:{buyerId}` exists with `TTL` ≈ 2592000 s (write path + TTL proven).

---

## Phase 4 — Service communication and checkout

### 4.1 Resilience: no retries on unsafe methods
**Branch:** `feature/resilience-unsafe-methods`
**Touches:** ServiceDefaults

Tasks
- **ServiceDefaults**: in `Extensions.cs`, configure `AddStandardResilienceHandler(o => o.Retry.DisableForUnsafeHttpMethods())`. Nothing else changes.

Verify
- Build passes; `aspire run` → all resources healthy.
- (Behaviour observed in 4.3: a failing POST to Ordering shows a single attempt in traces.)

### 4.2 Basket → Catalog: add item
**Branch:** `feature/basket-add-item`
**Touches:** Basket.Api

Tasks
- **Basket.Api**: typed `CatalogClient` (`https+http://catalogapi`) with its own `ProductDto` record; `GetProduct(id)` returning null on 404.
- `POST /api/basket/{buyerId}/items` `{ productId, quantity }`: `quantity < 1` → 400; product 404 → 400; existing line → `quantity += n` keeping original snapshot; else append line with Catalog name + price; save with TTL; 200 BasketDto.
- `.http`: add valid product, add same again, add missing product, add qty 0.

Verify
- Build passes.
- HTTP: add id 1 qty 2 → line qty 2, total = price × 2; add id 1 qty 1 → qty 3; add id 999 → 400; qty 0 → 400; remove id 1 → empty.
- Redis: `basket:{buyerId}` JSON holds snapshot name/price.
- Traces: `basketapi POST …/items` span with child `GET catalogapi /api/products/{id}`.

### 4.3 Basket → Ordering: checkout
**Branch:** `feature/basket-checkout`
**Touches:** Basket.Api

Tasks
- **Basket.Api**: typed `OrderingClient` (`https+http://orderingapi`) with its own `OrderRequest`/response records.
- `POST /api/basket/{buyerId}/checkout` `{ customerName, customerEmail }`: missing/empty basket → 400; `POST orderingapi /api/orders` with items copied from snapshot; non-success → exception → 500, basket untouched; on 201 delete basket; return `201 { orderId }`.
- `.http`: checkout empty basket, add + checkout happy path.

Verify
- Build passes.
- HTTP: checkout empty → 400; add items then checkout → 201 `{ orderId }`; `GET orderingapi /api/orders/{orderId}` → lines/prices match basket snapshot, Total correct; `GET basket` → empty; Redis key gone.
- Failure path: stop `orderingapi` in dashboard, checkout → 500, basket still present in Redis, trace shows exactly one POST attempt (4.1 working). Restart `orderingapi`.
- Traces: one trace `basketapi checkout` → `POST orderingapi /api/orders` → EF insert.

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
