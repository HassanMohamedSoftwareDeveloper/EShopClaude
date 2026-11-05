# EShopClaude — Project Document

## 1. Goal
Minimal e-commerce demo showing agentic coding with microservices on .NET 10 + Aspire. Small scope, few tokens, fast delivery. Four use cases, each with a storefront page: list products, add to cart, checkout, see the order.

Out of scope: login, identity, payments, deployment.

## 2. Architecture

```
               ┌────────────────────┐
               │  webfrontend       │  Blazor, InteractiveServer
               └─┬───────┬────────┬─┘
                 │       │        │
                 ▼       ▼        ▼
         catalogapi  basketapi  orderingapi
             │        │  │  │        │
             │        │  │  └──────► │   (POST /api/orders at checkout)
             │ ◄──────┘  │           │   (GET /api/products/{id} on add)
             ▼           ▼           ▼
        catalogdb      redis     orderingdb
        └──────── postgres ────────┘
```

- Synchronous HTTP only, via Aspire service discovery (`https+http://<resource>`). No gateway, no broker, no events.
- Call edges: Web→Catalog, Web→Basket, Web→Ordering, Basket→Catalog, Basket→Ordering. Ordering never calls anyone. Catalog never calls anyone.
- Failure handling: Aspire defaults only, with one change: retries disabled for unsafe methods (see §8). No custom retries, fallbacks, compensation or outbox.

### Projects (`src/`)
| Project | Aspire resource | Owns |
|---|---|---|
| EShopClaude.Catalog.Api | `catalogapi` | `catalogdb` (Postgres) |
| EShopClaude.Basket.Api | `basketapi` | Redis `redis` |
| EShopClaude.Ordering.Api | `orderingapi` | `orderingdb` (Postgres) |
| EShopClaude.Web | `webfrontend` | nothing (UI only) |
| EShopClaude.AppHost | — | orchestration |
| EShopClaude.ServiceDefaults | — | shared defaults (existing) |
| EShopClaude.Tests | — | one Aspire e2e test |

Delete from starter: `EShopClaude.ApiService`, `Web/WeatherApiClient.cs`, `Pages/Counter.razor`, `Pages/Weather.razor` and their nav links.

### AppHost
```csharp
var postgres   = builder.AddPostgres("postgres")           // no data volume: ephemeral
                    .WithPgAdmin();                        // browse catalogdb + orderingdb
var catalogDb  = postgres.AddDatabase("catalogdb");
var orderingDb = postgres.AddDatabase("orderingdb");
var redis      = builder.AddRedis("redis");                // no data volume: ephemeral

var catalogApi  = AddProject<Catalog.Api>("catalogapi").WithReference(catalogDb).WaitFor(catalogDb);
var orderingApi = AddProject<Ordering.Api>("orderingapi").WithReference(orderingDb).WaitFor(orderingDb);
var basketApi   = AddProject<Basket.Api>("basketapi").WithReference(redis).WaitFor(redis)
                    .WithReference(catalogApi).WithReference(orderingApi);
AddProject<Web>("webfrontend").WithExternalHttpEndpoints()
    .WithReference(catalogApi).WithReference(basketApi).WithReference(orderingApi)
    .WaitFor(catalogApi).WaitFor(basketApi).WaitFor(orderingApi);
```
All APIs keep `.WithHttpHealthCheck("/health")`.

One PostgreSQL server (`postgres`) hosts two dedicated databases: `catalogdb` (Catalog.Api only) and `orderingdb` (Ordering.Api only). pgAdmin runs as a container next to it and is opened from the Aspire dashboard to inspect both databases.

### Packages
- AppHost: `Aspire.Hosting.PostgreSQL`, `Aspire.Hosting.Redis` (same Aspire version as the AppHost SDK).
- Catalog.Api, Ordering.Api: `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.AspNetCore.OpenApi`.
- Basket.Api: `Aspire.StackExchange.Redis`, `Microsoft.AspNetCore.OpenApi`.
- Tests: `Aspire.Hosting.Testing`, xUnit.

### Conventions
- Minimal APIs, all routes under `/api`.
- Contracts are **duplicated per service**: each service and the Web declare their own small records. No shared contracts library; JSON is the contract.
- Schema via EF Core `Database.EnsureCreated()` at startup. No migrations.
- Money is `decimal`, single implicit currency, no tax or shipping.
- Each API: `AddOpenApi()` + `MapOpenApi()` in Development; one `.http` file per API in its project folder.

## 3. Services

### 3.1 Catalog.Api — read-only product catalog
**Data (`catalogdb`)**
| Product | |
|---|---|
| Id | int, PK |
| Name | string |
| Description | string |
| Price | decimal |

No images, stock, categories or brands. On startup: `EnsureCreated()`, then seed ~6 products if the table is empty.

**API**
| Method | Route | Response |
|---|---|---|
| GET | `/api/products` | `200 ProductDto[]` ordered by Id |
| GET | `/api/products/{id}` | `200 ProductDto` / `404` |

`ProductDto { id, name, description, price }`. No write endpoints.

### 3.2 Basket.Api — basket and checkout orchestration
Basket.Api owns all checkout logic. Whenever it needs product data, it calls Catalog.Api; it never reads `catalogdb` directly.

**Data (Redis)**
- Key `basket:{buyerId}`, value = JSON `CustomerBasket`.
- TTL 30 days, reset on every write.
- `CustomerBasket { buyerId, items: BasketItem[] }`
- `BasketItem { productId, productName, unitPrice, quantity }` — name and price are a **snapshot taken at add time** from Catalog.Api.

**API**
| Method | Route | Body | Response |
|---|---|---|---|
| GET | `/api/basket/{buyerId}` | — | `200 BasketDto` (empty items if no key) |
| POST | `/api/basket/{buyerId}/items` | `{ productId, quantity }` | `200 BasketDto` / `400` |
| DELETE | `/api/basket/{buyerId}/items/{productId}` | — | `200 BasketDto` |
| POST | `/api/basket/{buyerId}/checkout` | `{ customerName, customerEmail }` | `201 { orderId }` / `400` |

`BasketDto { buyerId, items: BasketItem[], total }` — `total = Σ unitPrice × quantity`, computed on read, not stored.

**Add item**
1. `quantity < 1` → `400`.
2. `GET catalogapi /api/products/{productId}`; `404` → `400`.
3. Load basket (or new). If line exists → `quantity += n` (keeps original snapshot). Else append line with Catalog name + price.
4. Save with TTL. Return basket.

**Remove item**: drop the line (no-op if absent), save, return basket. No quantity editing.

**Checkout** (owned by Basket.Api): `POST /api/basket/{buyerId}/checkout` reads the basket, sends a create-order request to Ordering.Api, and deletes the basket after the order is created.
1. Load basket. Missing or empty → `400`.
2. `POST orderingapi /api/orders` with `{ buyerId, customerName, customerEmail, items }` (items copied from the basket snapshot).
3. Non-success → exception → `500`; basket left untouched. No retry, no compensation.
4. On `201`: delete `basket:{buyerId}`.
5. Return `201 { orderId }`.

### 3.3 Ordering.Api — order store (CRUD only)
Ordering.Api stays simple: plain CRUD over orders, no business logic, no orchestration, no outgoing calls.

**Data (`orderingdb`)**
| Order | |
|---|---|
| Id | int, PK identity |
| BuyerId | string |
| CustomerName | string |
| CustomerEmail | string |
| CreatedAt | DateTimeOffset (UTC) |
| Total | decimal (computed at create from lines) |
| Lines | OrderLine[] |

| OrderLine | |
|---|---|
| Id | int, PK |
| OrderId | int, FK |
| ProductId | int |
| ProductName | string |
| UnitPrice | decimal |
| Quantity | int |

No status field.

**API**
| Method | Route | Body | Response |
|---|---|---|---|
| GET | `/api/orders` | — | `200 OrderDto[]` ordered by Id |
| GET | `/api/orders/{id}` | — | `200 OrderDto` / `404` |
| POST | `/api/orders` | `OrderRequest` | `201 OrderDto`, `Location: /api/orders/{id}` |
| PUT | `/api/orders/{id}` | `OrderRequest` | `200 OrderDto` / `404` (replaces customer data and lines, recomputes `Total`) |
| DELETE | `/api/orders/{id}` | — | `204` / `404` |

`OrderRequest { buyerId, customerName, customerEmail, items[{ productId, productName, unitPrice, quantity }] }`.

Ordering **trusts its caller**: stores lines and prices as sent, computes `Total` itself, never calls Catalog. Basket.Api uses POST; the storefront uses GET by id. List, PUT and DELETE exist for CRUD completeness and the `.http` file; no storefront page uses them.

## 4. Basket identity
No login. The storefront uses one constant: `const string BuyerId = "demo-user";`. Every browser shares this one basket — accepted for the demo. Basket.Api stays generic (`/api/basket/{buyerId}`) so real identity can replace the constant later, and the e2e test can use its own buyerId.

## 5. Storefront (EShopClaude.Web)
- Render mode: global `InteractiveServer` (on `Routes` and `HeadOutlet` in `App.razor`) so the layout/nav is interactive too.
- Typed clients: `CatalogClient` (`https+http://catalogapi`), `BasketClient` (`https+http://basketapi`), `OrderingClient` (`https+http://orderingapi`). Own DTO records.
- `CartState` (scoped): holds item count, raises `Changed`. Loaded from `GET basket` on first render, updated after add/remove/checkout.
- Nav: **Products**, **Cart (n)**.

### Pages and journey
```
/ Products ──add (stay)──► "Added" ──nav Cart──► /cart ──Checkout──► /checkout ──Place order──► /orders/{id}
```
| Route | Content | Actions |
|---|---|---|
| `/` | Product cards: name, description, price | **Add to cart** (qty 1) → POST items, show "Added", update nav count. Stays on page. |
| `/cart` | Lines: name, unit price, qty, line total; basket total | **Remove** per line. **Checkout** link (hidden when empty). Empty state text + link to `/`. |
| `/checkout` | Basket summary + form: Name (required), Email (required, email format) via `EditForm` + DataAnnotations | **Place order** → POST checkout → count = 0 → navigate `/orders/{orderId}`. Empty basket → message + link to `/`. |
| `/orders/{id}` | Order id, date, name, email, lines, total | Link back to `/`. |

No product detail page, no order history page.

## 6. Checkout sequence
```
Web /checkout         Basket.Api                    Ordering.Api
  │ POST /api/basket/demo-user/checkout {name,email}
  │──────────────────►│ GET redis basket:demo-user
  │                   │ empty? → 400
  │                   │ POST /api/orders {buyerId,name,email,items}
  │                   │──────────────────────────────►│ insert Order+Lines, compute Total
  │                   │◄──────────── 201 OrderDto ─────│
  │                   │ DEL basket:demo-user
  │◄── 201 {orderId} ─│
  │ NavigateTo /orders/{orderId} → GET /api/orders/{id} (Ordering.Api)
```

## 7. Persistence
Ephemeral: no data volumes on Postgres or Redis. Each AppHost run starts clean — catalog reseeded, no orders, no baskets. Keeps demos and the e2e test predictable.

## 8. Resilience
Keep `AddStandardResilienceHandler()` from ServiceDefaults, but configure `options.Retry.DisableForUnsafeHttpMethods()` so POST/DELETE are never retried (prevents duplicate orders when POST /api/orders times out). GETs keep default retries. Nothing else.

## 9. Testing
- **One e2e test** in `EShopClaude.Tests` using `DistributedApplicationTestingBuilder` on the AppHost:
  1. `GET catalogapi /api/products` → non-empty.
  2. `POST basketapi /api/basket/{testBuyer}/items` with first product, qty 2.
  3. `POST basketapi /api/basket/{testBuyer}/checkout` → `201`, orderId.
  4. `GET orderingapi /api/orders/{orderId}` → 1 line, qty 2, total = price × 2.
  5. `GET basketapi /api/basket/{testBuyer}` → empty.
- Manual: Aspire dashboard, storefront walkthrough, `.http` files.

## 10. Deliberately left out
Login/identity, payments, deployment; product images, stock, categories, search, paging; product detail page; quantity editing; order status and order history page; price re-validation at checkout; shared contracts library; EF migrations; data volumes; API gateway, message broker, events; custom retries, fallbacks, sagas, outbox; unit tests; RedisInsight; tax, shipping, currency.

## 11. Delivery order
1. AppHost infra (Postgres, Redis) + ServiceDefaults retry tweak; delete starter ApiService/samples.
2. Catalog.Api (+ seed, .http).
3. Ordering.Api (+ .http).
4. Basket.Api incl. checkout (+ .http).
5. Web: clients, CartState, four pages, nav.
6. E2E test.

## 12. Decision log
| Topic | Decision |
|---|---|
| Basket line data | Snapshot name + price from Catalog at add |
| Basket identity | Constant `demo-user`, passed as route param by Web |
| Checkout data | Customer name + email |
| Checkout owner | Basket.Api: read basket → create order in Ordering.Api → delete basket |
| Order trust | Ordering trusts Basket; no Catalog call |
| Ordering.Api scope | Plain CRUD over orders, no business logic |
| Databases | One Postgres server, `catalogdb` + `orderingdb`; Redis for baskets |
| DB tooling | pgAdmin on the Postgres server |
| Product fields | Id, Name, Description, Price; seeded; read-only API |
| Cart edits | Add (increments) + remove line |
| See the order | Detail page only, `/orders/{id}` |
| Contracts | Duplicated per service |
| Schema | EF Core + EnsureCreated |
| Order status | None |
| Tests | One Aspire e2e test |
| Naming | `EShopClaude.<X>.Api`; starter samples deleted |
| Add flow | Button on list, stay on list |
| Render mode | InteractiveServer |
| Extras kept | .http files, OpenAPI, cart count in nav, basket TTL 30d |
| Retries | Disabled for unsafe methods |
| Persistence | Ephemeral |
