# Ordering.Api 04 — List orders

**Status:** Not started

## Goal

Return every stored order, ordered by Id, for CRUD completeness and manual checks from the `.http` file. No storefront page uses it.

## Context

- **Starting point on `main`** (after ordering-03):
  - AppHost wires `orderingapi` `.WithReference(orderingDb).WaitFor(orderingDb)`. Nothing in the AppHost changes in this spec.
  - `Program.cs` already has `using EShopClaude.Ordering.Api;`, `using EShopClaude.Ordering.Api.Data;`, `using Microsoft.AspNetCore.Http.HttpResults;` and `using Microsoft.EntityFrameworkCore;`. It registers `AddNpgsqlDbContext<OrderingDbContext>("orderingdb")`, runs `EnsureCreatedAsync()` at startup, and maps `POST /api/orders` (`CreateOrder`) and `GET /api/orders/{id:int}` (`GetOrder`) on `var api = app.MapGroup("/api");`. No new `using` is needed.
  - `OrderDto.cs` holds `OrderDto` with `public static OrderDto From(Order order)`, the only Entity→DTO mapping (its comment already names "get, list and update"). `OrderLineDto.cs` holds the line record. This spec reuses both unchanged.
  - Every fresh run starts with 0 rows (no data volume), so the list starts as `[]`.
  - `EShopClaude.Ordering.Api.http` has `@host = http://localhost:5103`, status, OpenAPI, `### Create order`, `### Get order (created)` and `### Get order (missing → 404)` requests.
- **Route**: `/orders` on the `api` group, `MapGet`. It shares the path with `CreateOrder` (POST); the verb tells them apart.
- **Query** (decided: mirror `GetOrder`): `AsNoTracking()`, `Include(o => o.Lines.OrderBy(l => l.Id))`, `OrderBy(o => o.Id)`, `ToListAsync(ct)`. EF runs one SQL query (`Orders` LEFT JOIN `OrderLines`, `ORDER BY o."Id", ... l."Id"`). Orders come back in ascending id; lines in each order come back in insert order, which is the order the POST sent them, the same as `GetOrder`.
- **Mapping** (decided): the loaded entities go through `orders.ConvertAll(OrderDto.From)`. No SQL projection, no second mapping.
- **Response** (decided): the handler returns `Task<Ok<List<OrderDto>>>` via `TypedResults.Ok`, matching the `CreateOrder` / `GetOrder` style. OpenAPI documents `200` with an `OrderDto` array. Endpoint name `ListOrders`. No 404: an empty store returns `200 []`.
- **Values**: `createdAt`, `total` and `unitPrice` come back from the database exactly as `GetOrder` returns them (microsecond `createdAt`, 2-decimal `numeric(18,2)` money). Each list element equals the `GET /api/orders/{id}` body for that id.

## Depends on

- `ordering-api/02-create-order.md`

## Scope

- [ ] `GET /api/orders` → `200 OrderDto[]` with lines, ordered by Id.
- [ ] `.http` file: list request.

## Tasks

Run `dotnet build src/EShopClaude.sln` after each task.

1. **Ordering.Api `Program.cs`**: after `CreateOrder` and before `GetOrder`, map on the `api` group:
   ```csharp
   // Every order with its lines, ordered by Id (PROJECT.md §3.3). CRUD completeness; no storefront page uses it.
   api.MapGet("/orders", async Task<Ok<List<OrderDto>>> (OrderingDbContext db, CancellationToken ct) =>
       {
           var orders = await db.Orders
               .AsNoTracking()
               .Include(o => o.Lines.OrderBy(l => l.Id))
               .OrderBy(o => o.Id)
               .ToListAsync(ct);

           return TypedResults.Ok(orders.ConvertAll(OrderDto.From));
       })
       .WithName("ListOrders");
   ```
2. **`EShopClaude.Ordering.Api.http`**: after `### Create order` and before `### Get order (created)`, add `### List orders` → `GET {{host}}/api/orders` with the comment `# [] on a fresh run; every created order, ascending id, with lines`.
3. Run `aspire run` and walk the Definition of Done.

## Files touched

| File | Change |
|---|---|
| `src/EShopClaude.Ordering.Api/Program.cs` | changed: `GET /api/orders` (`ListOrders`) on the `/api` group |
| `src/EShopClaude.Ordering.Api/EShopClaude.Ordering.Api.http` | changed: list orders request |
| `_agenticdev/microservices/ordering-api/04-list-orders.md`, `_agenticdev/microservices/README.md` | spec and status |

Not touched: AppHost, csproj files, appsettings files, `Data/*`, `OrderDto.cs`, `OrderLineDto.cs`, `OrderRequest.cs`, `OrderItemRequest.cs`, the `CreateOrder` and `GetOrder` handlers, ServiceDefaults, Catalog.Api, Basket.Api, Web.

## Explicit non-goals

- No paging, filtering by buyer, or order history page.
- No PUT or DELETE (ordering-05, ordering-06).
- No change to `POST /api/orders` or `GET /api/orders/{id}`, no new DTO, no status field.

## Definition of Done

Each line is a check you can run, with its expected result. Shell commands use Git Bash syntax. `<orderingapi>` is the `orderingapi` http endpoint shown in the Aspire dashboard. Run the checks in order on a fresh `aspire run`, because the ids and the empty list depend on it.

1. **Build**: `dotnet build src/EShopClaude.sln` → `0 Error(s)`, and the same warning count as the same command on `main`.
2. **Run**: `cd src && aspire run` → Dashboard → Resources: every resource shows State **Running** and Health **Healthy**.
3. **Empty list**: `curl -i <orderingapi>/api/orders` → `HTTP/1.1 200 OK`, `Content-Type: application/json`, body exactly `[]`.
4. **Create two orders**:
   ```sh
   curl -i -X POST <orderingapi>/api/orders -H "Content-Type: application/json" -d '{"buyerId":"dod-buyer","customerName":"Ada Lovelace","customerEmail":"ada@example.com","items":[{"productId":1,"productName":"Classic T-Shirt","unitPrice":19.99,"quantity":2},{"productId":6,"productName":"Backpack","unitPrice":59.00,"quantity":1}]}'
   curl -i -X POST <orderingapi>/api/orders -H "Content-Type: application/json" -d '{"buyerId":"dod-buyer-2","customerName":"Alan Turing","customerEmail":"alan@example.com","items":[{"productId":6,"productName":"Backpack","unitPrice":59.00,"quantity":1}]}'
   ```
   → `201 Created` with `Location: /api/orders/1`, then `201 Created` with `Location: /api/orders/2`.
5. **List both**: `curl -i <orderingapi>/api/orders` → `HTTP/1.1 200 OK`, `Content-Type: application/json`, a JSON array of length `2`:
   - `[0]`: `id` = `1`, `buyerId` = `dod-buyer`, `customerName` = `Ada Lovelace`, `customerEmail` = `ada@example.com`, `total` = `98.98`, `lines` = `[{"productId":1,"productName":"Classic T-Shirt","unitPrice":19.99,"quantity":2},{"productId":6,"productName":"Backpack","unitPrice":59.00,"quantity":1}]` in this order;
   - `[1]`: `id` = `2`, `buyerId` = `dod-buyer-2`, `customerName` = `Alan Turing`, `customerEmail` = `alan@example.com`, `total` = `59.00`, `lines` = `[{"productId":6,"productName":"Backpack","unitPrice":59.00,"quantity":1}]`;
   - each element has exactly the keys `id`, `buyerId`, `customerName`, `customerEmail`, `createdAt`, `total`, `lines`; each line exactly `productId`, `productName`, `unitPrice`, `quantity`.
6. **Matches get by id**: `curl -s <orderingapi>/api/orders/1` and `curl -s <orderingapi>/api/orders/2` → bodies identical (including `createdAt`) to list elements `[0]` and `[1]` from check 5.
7. **Line order kept**: `curl -i -X POST <orderingapi>/api/orders -H "Content-Type: application/json" -d '{"buyerId":"dod-buyer","customerName":"Ada Lovelace","customerEmail":"ada@example.com","items":[{"productId":6,"productName":"Backpack","unitPrice":59.00,"quantity":1},{"productId":1,"productName":"Classic T-Shirt","unitPrice":19.99,"quantity":2}]}'` → `201`, `Location: /api/orders/3`; then `curl -s <orderingapi>/api/orders` → array length `3`, ids `1`, `2`, `3` in this order, and `[2].lines[0].productId` = `6`, `[2].lines[1].productId` = `1`, `[2].total` = `98.98`.
8. **Existing routes unchanged**: `curl -i <orderingapi>/api/orders/999` → `HTTP/1.1 404 Not Found`, `Content-Length: 0`.
9. **.http file**: `EShopClaude.Ordering.Api.http` (with `@host` set to `<orderingapi>`) → `### List orders` gives `200` with the check 7 array.
10. **OpenAPI**: `curl <orderingapi>/openapi/v1.json` → `paths["/api/orders"]` has exactly `get` (operationId `ListOrders`, response `200` with an array of `OrderDto`) and `post` (`CreateOrder`); `paths["/api/orders/{id}"]` has only `get` (`GetOrder`); no `put`, `delete` or `patch` anywhere under `/api/orders`.
11. **Traces**: Dashboard → Traces, filter resource `orderingapi`, open the check 5 `GET /api/orders` → a server span with one child database span (`postgresql` / `orderingdb`) running `SELECT ... FROM "Orders" ... LEFT JOIN "OrderLines" ... ORDER BY o."Id"`, and no span in error status. The check 3 request (empty list) shows the same shape.
12. **Health**: `GET <orderingapi>/health` → `200` with body `Healthy`.
13. **Diff scope**: `git diff --stat main` lists only the files in Files touched.
14. **Non-goals held**: each command → no matches:
    - `git grep -nE "Migrations|WithDataVolume|HasData|MapPut|MapDelete|MapPatch|HttpClient|catalogapi" -- src/EShopClaude.Ordering.Api`
    - `git grep -nE "\.Skip\(|\.Take\(|pageSize|page=" -- src/EShopClaude.Ordering.Api`
    - `git grep -nE "o\.BuyerId ==|Where\(o => o\.BuyerId" -- src/EShopClaude.Ordering.Api`
    - `git grep -nw "Status" -- src/EShopClaude.Ordering.Api`

## References

- `PROJECT.md` §3.3 Ordering.Api: `GET /api/orders` → `200 OrderDto[]` ordered by Id; "List, PUT and DELETE exist for CRUD completeness and the `.http` file; no storefront page uses them"; no status field; no outgoing calls.
- `PROJECT.md` §2 Conventions: minimal APIs under `/api`, contracts duplicated per service, `decimal` money, OpenAPI + `.http` per API.
- `PROJECT.md` §5 Storefront: no order history page. `PROJECT.md` §10 Deliberately left out (paging, order history, order status).
- `PLAN.md` 3.2 (Ordering.Api spec backlog).
- `ordering-api/02-create-order.md`: `OrderDto` / `OrderDto.From`. `ordering-api/03-get-order.md`: ordered `Include` + `OrderDto.From` + `TypedResults` pattern this spec mirrors. `catalog-api/03-list-products.md`: list ordered by Id.
- `WOW.md`: branch, cycle and Definition of Done rules.

