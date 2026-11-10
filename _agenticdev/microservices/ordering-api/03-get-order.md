# Ordering.Api 03 — Get order by id

**Status:** Not started

## Goal

Return one stored order with its lines, or 404. The storefront's order page and the e2e test will use this. It also makes the `Location` header from create resolvable.

## Context

- **Starting point on `main`** (after ordering-02):
  - AppHost wires `orderingapi` `.WithReference(orderingDb).WaitFor(orderingDb)`. Nothing in the AppHost changes in this spec.
  - `Program.cs` has `using EShopClaude.Ordering.Api;`, `using EShopClaude.Ordering.Api.Data;` and `using Microsoft.AspNetCore.Http.HttpResults;` (no `using Microsoft.EntityFrameworkCore;` yet). It registers `AddNpgsqlDbContext<OrderingDbContext>("orderingdb")`, runs `EnsureCreatedAsync()` in a startup scope, has `UseExceptionHandler()` + `AddProblemDetails()`, and maps `POST /api/orders` (`CreateOrder`) on `var api = app.MapGroup("/api");`.
  - `OrderDto.cs` holds `OrderDto` with `public static OrderDto From(Order order)`, the only Entity→DTO mapping. `OrderLineDto.cs` holds the line record. This spec reuses both unchanged.
  - `Data/Order.cs` has `List<OrderLine> Lines`; `OrderingDbContext` maps `HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId)`. Every fresh run starts with 0 rows.
  - `EShopClaude.Ordering.Api.http` has `@host = http://localhost:5103`, status, OpenAPI and `### Create order` requests.
  - Catalog.Api's `GET /api/products/{id:int}` (`GetProduct`, `Results<Ok<ProductDto>, NotFound>`) is the reference pattern. Mirror it.
- **Route**: `/orders/{id:int}` on the `api` group. The `int` constraint means a non-numeric id never matches and routing returns 404.
- **Query**: `AsNoTracking()`, `Include(o => o.Lines.OrderBy(l => l.Id))`, then `FirstOrDefaultAsync(o => o.Id == id, ct)`. The ordered (filtered) include returns lines in insert order, which is the order the POST sent them, so GET and POST bodies list lines identically. EF runs one SQL query (`Orders` LEFT JOIN `OrderLines`).
- **Mapping**: the loaded entity goes through `OrderDto.From(order)`. No second projection; the DB line `Id` / `OrderId` stay hidden as in ordering-02.
- **Response**: the handler returns `Task<Results<Ok<OrderDto>, NotFound>>` via `TypedResults`, so OpenAPI documents `200 OrderDto` and `404` without extra metadata. The 404 has an empty body. Endpoint name `GetOrder`.
- **CreatedAt precision**: POST returns `CreatedAt` from memory (`DateTimeOffset.UtcNow`, 100 ns ticks, up to 7 fractional digits). Postgres `timestamptz` keeps microseconds (6 digits), so GET returns the same instant cut to the microsecond. This is accepted: the DoD compares `createdAt` to microsecond precision. POST is not changed here.
- **Decimals**: `total` and `unitPrice` come back from `numeric(18,2)` columns, so GET shows the stored 2-decimal values (`98.98`, `19.99`, `59.00`).

## Depends on

- `ordering-api/02-create-order.md`

## Scope

- [ ] `GET /api/orders/{id}` → `200 OrderDto` with lines, or `404`.
- [ ] `.http` file: get the created id, get a missing id.

## Tasks

Run `dotnet build src/EShopClaude.sln` after each task.

1. **Ordering.Api `Program.cs`**: add `using Microsoft.EntityFrameworkCore;`. After `CreateOrder`, map on the `api` group:
   ```csharp
   // One order with its lines, or 404 (PROJECT.md §3.3). The storefront order page reads this.
   api.MapGet("/orders/{id:int}", async Task<Results<Ok<OrderDto>, NotFound>> (int id, OrderingDbContext db, CancellationToken ct) =>
           await db.Orders
               .AsNoTracking()
               .Include(o => o.Lines.OrderBy(l => l.Id))
               .FirstOrDefaultAsync(o => o.Id == id, ct) is { } order
               ? TypedResults.Ok(OrderDto.From(order))
               : TypedResults.NotFound())
       .WithName("GetOrder");
   ```
2. **`EShopClaude.Ordering.Api.http`**: after `### Create order`, add `### Get order (created)` → `GET {{host}}/api/orders/1` with the comment `# id 1 = first Create order on a fresh run; use the id from its Location header`, and `### Get order (missing → 404)` → `GET {{host}}/api/orders/999`.
3. Run `aspire run` and walk the Definition of Done.

## Files touched

| File | Change |
|---|---|
| `src/EShopClaude.Ordering.Api/Program.cs` | changed: `using Microsoft.EntityFrameworkCore;` + `GET /api/orders/{id}` on the `/api` group |
| `src/EShopClaude.Ordering.Api/EShopClaude.Ordering.Api.http` | changed: get created + get missing requests |
| `_agenticdev/microservices/ordering-api/03-get-order.md`, `_agenticdev/microservices/README.md` | spec and status |

Not touched: AppHost, csproj files, appsettings files, `Data/*`, `OrderDto.cs`, `OrderLineDto.cs`, `OrderRequest.cs`, `OrderItemRequest.cs`, ServiceDefaults, Catalog.Api, Basket.Api, Web.

## Explicit non-goals

- No list, PUT or DELETE (ordering-04 to ordering-06).
- No order history or filtering by buyer.
- No change to `POST /api/orders` (including `CreatedAt` precision), no new DTO, no status field.

## Definition of Done

Each line is a check you can run, with its expected result. Shell commands use Git Bash syntax. `<orderingapi>` is the `orderingapi` http endpoint shown in the Aspire dashboard. Run the checks in order on a fresh `aspire run`, because the ids depend on it.

1. **Build**: `dotnet build src/EShopClaude.sln` → `0 Error(s)`, and the same warning count as the same command on `main`.
2. **Run**: `cd src && aspire run` → Dashboard → Resources: every resource shows State **Running** and Health **Healthy**.
3. **Create**:
   ```sh
   curl -i -X POST <orderingapi>/api/orders -H "Content-Type: application/json" -d '{"buyerId":"dod-buyer","customerName":"Ada Lovelace","customerEmail":"ada@example.com","items":[{"productId":1,"productName":"Classic T-Shirt","unitPrice":19.99,"quantity":2},{"productId":6,"productName":"Backpack","unitPrice":59.00,"quantity":1}]}'
   ```
   → `201 Created`, `Location: /api/orders/1`. Keep the body for check 4.
4. **Get created**: `curl -i <orderingapi>/api/orders/1` (the `Location` path from check 3) → `HTTP/1.1 200 OK`, `Content-Type: application/json`, and a body with exactly the keys `id`, `buyerId`, `customerName`, `customerEmail`, `createdAt`, `total`, `lines`, equal to the check 3 body:
   - `id` = `1`, `buyerId` = `dod-buyer`, `customerName` = `Ada Lovelace`, `customerEmail` = `ada@example.com`;
   - `total` = `98.98`;
   - `lines` = `[{"productId":1,"productName":"Classic T-Shirt","unitPrice":19.99,"quantity":2},{"productId":6,"productName":"Backpack","unitPrice":59.00,"quantity":1}]`, in this order, each line with exactly those 4 keys;
   - `createdAt` ends in `+00:00` and equals the check 3 `createdAt` with its fraction cut to 6 digits (trailing zeros may be dropped). Calling check 4 a second time returns the identical body.
5. **Line order kept**: `curl -i -X POST <orderingapi>/api/orders -H "Content-Type: application/json" -d '{"buyerId":"dod-buyer","customerName":"Ada Lovelace","customerEmail":"ada@example.com","items":[{"productId":6,"productName":"Backpack","unitPrice":59.00,"quantity":1},{"productId":1,"productName":"Classic T-Shirt","unitPrice":19.99,"quantity":2}]}'` → `201`, `Location: /api/orders/2`; then `curl -s <orderingapi>/api/orders/2` → `lines[0].productId` = `6`, `lines[1].productId` = `1`, `total` = `98.98`.
6. **Get missing**: `curl -i <orderingapi>/api/orders/999` → `HTTP/1.1 404 Not Found`, `Content-Length: 0`.
7. **Non-numeric id**: `curl -i <orderingapi>/api/orders/abc` → `HTTP/1.1 404 Not Found` (route constraint, handler not hit).
8. **.http file**: `EShopClaude.Ordering.Api.http` (with `@host` set to `<orderingapi>`) → `### Get order (created)` gives `200` with order `1` as in check 4; `### Get order (missing → 404)` gives check 6's result.
9. **OpenAPI**: `curl <orderingapi>/openapi/v1.json` → `paths["/api/orders/{id}"].get` exists with operationId `GetOrder`, an integer path parameter `id`, and responses `200` (schema `OrderDto`) and `404`. `paths["/api/orders"]` still has only `post` (`CreateOrder`); no `put`, `delete` or `patch` anywhere under `/api/orders`.
10. **Traces**: Dashboard → Traces, filter resource `orderingapi`, open the check 4 `GET /api/orders/{id}` → a server span with one child database span (`postgresql` / `orderingdb`) running `SELECT ... FROM "Orders" ... LEFT JOIN "OrderLines" ... WHERE o."Id" = @id`, and no span in error status. The check 6 request (id `999`) shows the same shape with no span in error status.
11. **Health**: `GET <orderingapi>/health` → `200` with body `Healthy`.
12. **Diff scope**: `git diff --stat main` lists only the files in Files touched.
13. **Non-goals held**: each command → no matches:
    - `git grep -nE "Migrations|WithDataVolume|HasData|MapPut|MapDelete|MapPatch|HttpClient|catalogapi" -- src/EShopClaude.Ordering.Api`
    - `git grep -n 'MapGet("/orders"' -- src/EShopClaude.Ordering.Api` (list is ordering-04)
    - `git grep -nE "o\.BuyerId ==|Where\(o => o\.BuyerId" -- src/EShopClaude.Ordering.Api`
    - `git grep -nw "Status" -- src/EShopClaude.Ordering.Api`

## References

- `PROJECT.md` §3.3 Ordering.Api: `GET /api/orders/{id}` → `200 OrderDto` / `404`; "the storefront uses GET by id"; no status field; no outgoing calls.
- `PROJECT.md` §2 Conventions: minimal APIs under `/api`, contracts duplicated per service, `decimal` money, OpenAPI + `.http` per API.
- `PROJECT.md` §10 Deliberately left out.
- `PLAN.md` 3.2 (Ordering.Api spec backlog); `PLAN.md` 5.5 (Web order page `/orders/{id}` reads this) and the e2e test step (order has 1 line, qty 2, total = price × 2).
- `ordering-api/02-create-order.md`: `OrderDto` / `OrderDto.From`, the `Location` header this resolves. `catalog-api/04-get-product.md`: `{id:int}` + `Results<Ok<T>, NotFound>` pattern.
- `WOW.md`: branch, cycle and Definition of Done rules.

