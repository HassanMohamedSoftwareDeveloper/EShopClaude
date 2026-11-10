# Ordering.Api 05 — Update order

**Status:** Done (6e42690ba6caa578b04b240a6ff5f41adf14f8ab)

## Goal

Replace an existing order's customer data and lines, and recompute its total, for CRUD completeness. No storefront page uses it.

## Context

- **Starting point on `main`** (after ordering-04):
  - AppHost wires `orderingapi` `.WithReference(orderingDb).WaitFor(orderingDb)`. Nothing in the AppHost changes in this spec.
  - `Program.cs` already has `using EShopClaude.Ordering.Api;`, `using EShopClaude.Ordering.Api.Data;`, `using Microsoft.AspNetCore.Http.HttpResults;` and `using Microsoft.EntityFrameworkCore;`. It registers `AddNpgsqlDbContext<OrderingDbContext>("orderingdb")`, runs `EnsureCreatedAsync()` at startup, has `UseExceptionHandler()` + `AddProblemDetails()`, and maps `POST /api/orders` (`CreateOrder`), `GET /api/orders` (`ListOrders`) and `GET /api/orders/{id:int}` (`GetOrder`) on `var api = app.MapGroup("/api");`. No new `using` is needed.
  - `OrderRequest.cs` / `OrderItemRequest.cs` are the request records from ordering-02; `OrderDto.cs` holds `OrderDto` with `public static OrderDto From(Order order)`, the only Entity→DTO mapping (its comment already names "update"). This spec reuses all of them unchanged. No new DTO.
  - `Data/OrderingDbContext.cs` configures `Order.Lines` as a required one-way relationship (`HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade)`). Because the FK is required, a line removed from a tracked order's `Lines` becomes an orphan that EF deletes on `SaveChangesAsync`. No change to `Data/*`.
  - Every fresh run starts with 0 rows (no data volume), so ids start at `1`.
  - `EShopClaude.Ordering.Api.http` has `@host = http://localhost:5103`, status, OpenAPI, `### Create order`, `### List orders`, `### Get order (created)` and `### Get order (missing → 404)` requests.
- **Route**: `/orders/{id:int}` on the `api` group, `MapPut`. It shares the path with `GetOrder`; the verb tells them apart. Body is `OrderRequest`, the same shape as POST (PROJECT.md §3.3).
- **Replaced fields** (decided: full PUT): `BuyerId`, `CustomerName`, `CustomerEmail` and `Lines` all come from the body. `Total` is recomputed as `Σ UnitPrice × Quantity` over the body items. `Id` and `CreatedAt` are never assigned, so they keep their stored values. Any `total`, `id` or `createdAt` in the body is ignored, because `OrderRequest` has no such member.
- **Line replacement** (decided: tracked load + reassign): load the order **tracked** (no `AsNoTracking`) with `Include(o => o.Lines)`, then assign `order.Lines` a new list built from the body items. On `SaveChangesAsync`, EF deletes the old lines (orphans of a required FK), inserts the new ones, and updates the `Orders` row, all in one transaction. New lines get new identity `Id` values; old line ids are not reused.
- **Response** (decided: mirror `GetOrder`): the handler returns `Task<Results<Ok<OrderDto>, NotFound>>` via `TypedResults`, so OpenAPI documents `200 OrderDto` and `404` without extra metadata. Endpoint name `UpdateOrder`. The `200` body is `OrderDto.From(order)` on the tracked entity after the save, so `createdAt` is the stored value (as `GetOrder` returns it) and `lines` are in body order.
- **Missing id**: no row → `404` with an empty body, and nothing is written. The body is not validated first.
- **Trusts its caller** (PROJECT.md §3.3), same as `CreateOrder`: no checks beyond model binding, no Catalog call. An empty `items` list is stored as an order with no lines and `total` `0`. A null `items` list or null required string becomes a `500` ProblemDetails through `UseExceptionHandler`.

## Depends on

- `ordering-api/03-get-order.md`

## Scope

- [ ] `PUT /api/orders/{id}` with `OrderRequest` → replace buyer id, customer name, email and lines, recompute `Total` → `200 OrderDto`, or `404`.
- [ ] `.http` file: update existing, update missing.

## Tasks

Run `dotnet build src/EShopClaude.sln` after each task.

1. **Ordering.Api `Program.cs`**: after `GetOrder` and before `app.MapDefaultEndpoints();`, map on the `api` group:
   ```csharp
   // Replace customer data and lines, recompute Total; Id and CreatedAt are kept (PROJECT.md §3.3). CRUD completeness; no storefront page uses it.
   api.MapPut("/orders/{id:int}", async Task<Results<Ok<OrderDto>, NotFound>> (int id, OrderRequest request, OrderingDbContext db, CancellationToken ct) =>
       {
           // Tracked: the old lines become orphans of a required FK, so SaveChangesAsync deletes them.
           var order = await db.Orders
               .Include(o => o.Lines)
               .FirstOrDefaultAsync(o => o.Id == id, ct);

           if (order is null)
           {
               return TypedResults.NotFound();
           }

           order.BuyerId = request.BuyerId;
           order.CustomerName = request.CustomerName;
           order.CustomerEmail = request.CustomerEmail;
           order.Total = request.Items.Sum(i => i.UnitPrice * i.Quantity);
           order.Lines = [.. request.Items.Select(i => new OrderLine
           {
               ProductId = i.ProductId,
               ProductName = i.ProductName,
               UnitPrice = i.UnitPrice,
               Quantity = i.Quantity,
           })];

           await db.SaveChangesAsync(ct);

           return TypedResults.Ok(OrderDto.From(order));
       })
       .WithName("UpdateOrder");
   ```
2. **`EShopClaude.Ordering.Api.http`**: after `### Get order (missing → 404)`, add two requests, each `Content-Type: application/json` with the check 4 body (without `"total"`):
   - `### Update order (created)` → `PUT {{host}}/api/orders/1`, comment `# id 1 = first Create order on a fresh run; replaces buyer, customer and lines, recomputes total, keeps createdAt`.
   - `### Update order (missing → 404)` → `PUT {{host}}/api/orders/999`.
3. Run `aspire run` and walk the Definition of Done.

## Files touched

| File | Change |
|---|---|
| `src/EShopClaude.Ordering.Api/Program.cs` | changed: `PUT /api/orders/{id:int}` (`UpdateOrder`) on the `/api` group |
| `src/EShopClaude.Ordering.Api/EShopClaude.Ordering.Api.http` | changed: update existing and update missing requests |
| `_agenticdev/microservices/ordering-api/05-update-order.md`, `_agenticdev/microservices/README.md` | spec and status |

Not touched: AppHost, csproj files, appsettings files, `Data/*`, `OrderDto.cs`, `OrderLineDto.cs`, `OrderRequest.cs`, `OrderItemRequest.cs`, the `CreateOrder`, `ListOrders` and `GetOrder` handlers, ServiceDefaults, Catalog.Api, Basket.Api, Web.

## Explicit non-goals

- No partial update (PATCH) and no line-level endpoints.
- `CreatedAt` and `Id` are never changed.
- No status field.
- No DELETE (ordering-06), no new DTO, no change to `POST`, list or get by id.
- No Catalog call, no price check, no validation beyond model binding, no `Total` taken from input.
- No concurrency token or ETag / `If-Match` handling.

## Definition of Done

Each line is a check you can run, with its expected result. Shell commands use Git Bash syntax. `<orderingapi>` is the `orderingapi` http endpoint shown in the Aspire dashboard. `<container>` is the Postgres container from `docker ps --filter name=postgres`. Run the checks in order on a fresh `aspire run`, because the ids depend on it.

1. **Build**: `dotnet build src/EShopClaude.sln` → `0 Error(s)`, and the same warning count as the same command on `main`.
2. **Run**: `cd src && aspire run` → Dashboard → Resources: every resource shows State **Running** and Health **Healthy**.
3. **Create two orders**:
   ```sh
   curl -i -X POST <orderingapi>/api/orders -H "Content-Type: application/json" -d '{"buyerId":"dod-buyer","customerName":"Ada Lovelace","customerEmail":"ada@example.com","items":[{"productId":1,"productName":"Classic T-Shirt","unitPrice":19.99,"quantity":2},{"productId":6,"productName":"Backpack","unitPrice":59.00,"quantity":1}]}'
   curl -i -X POST <orderingapi>/api/orders -H "Content-Type: application/json" -d '{"buyerId":"dod-buyer-2","customerName":"Alan Turing","customerEmail":"alan@example.com","items":[{"productId":6,"productName":"Backpack","unitPrice":59.00,"quantity":1}]}'
   ```
   → `201 Created` with `Location: /api/orders/1` and `total` `98.98`, then `201 Created` with `Location: /api/orders/2`. Note order 1's `createdAt` as `<created1>` from `curl -s <orderingapi>/api/orders/1`.
4. **Update existing**:
   ```sh
   curl -i -X PUT <orderingapi>/api/orders/1 -H "Content-Type: application/json" -d '{"buyerId":"dod-buyer-3","customerName":"Grace Hopper","customerEmail":"grace@example.com","total":1,"items":[{"productId":3,"productName":"Coffee Mug","unitPrice":12.50,"quantity":3}]}'
   ```
   → `HTTP/1.1 200 OK`, `Content-Type: application/json`, a body with exactly the keys `id`, `buyerId`, `customerName`, `customerEmail`, `createdAt`, `total`, `lines`:
   - `id` = `1`; `buyerId` = `dod-buyer-3`, `customerName` = `Grace Hopper`, `customerEmail` = `grace@example.com`;
   - `createdAt` = `<created1>` exactly;
   - `total` = `37.50` (the body's `"total":1` is ignored);
   - `lines` = `[{"productId":3,"productName":"Coffee Mug","unitPrice":12.50,"quantity":3}]`, with no `id` or `orderId`.
5. **Get by id matches**: `curl -s <orderingapi>/api/orders/1` → body identical to the check 4 body, including `createdAt` = `<created1>`.
6. **Stored, `Orders`**: `docker exec <container> sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" psql -U postgres -d orderingdb -tAc "SELECT \"Id\",\"BuyerId\",\"CustomerName\",\"CustomerEmail\",\"Total\" FROM \"Orders\" ORDER BY \"Id\""'` → exactly two rows, `1|dod-buyer-3|Grace Hopper|grace@example.com|37.50` and `2|dod-buyer-2|Alan Turing|alan@example.com|59.00`.
7. **Stored, `OrderLines`**: `docker exec <container> sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" psql -U postgres -d orderingdb -tAc "SELECT \"Id\",\"OrderId\",\"ProductId\",\"ProductName\",\"UnitPrice\",\"Quantity\" FROM \"OrderLines\" ORDER BY \"Id\""'` → exactly two rows, `3|2|6|Backpack|59.00|1` and `4|1|3|Coffee Mug|12.50|3`. Lines `1` and `2` (order 1's old lines) are gone; order 2's line is untouched. pgAdmin (opened from the dashboard) shows the same rows under `orderingdb`.
8. **Other order untouched**: `curl -s <orderingapi>/api/orders/2` → `buyerId` = `dod-buyer-2`, `customerName` = `Alan Turing`, `total` = `59.00`, `lines` = `[{"productId":6,"productName":"Backpack","unitPrice":59.00,"quantity":1}]`, same `createdAt` as before check 4.
9. **Update missing**: the check 4 request against `<orderingapi>/api/orders/999` → `HTTP/1.1 404 Not Found`, `Content-Length: 0`. Then the check 6 and check 7 queries return the same rows as before (nothing written, no order `999`).
10. **List reflects update**: `curl -s <orderingapi>/api/orders` → array of length `2`; `[0]` is identical to the check 4 body, `[1]` to the check 8 body.
11. **.http file**: `EShopClaude.Ordering.Api.http` (with `@host` set to `<orderingapi>`) → `### Update order (created)` gives `200` with `id` `1`, `total` `37.50`, one `Coffee Mug` line and `createdAt` = `<created1>`; `### Update order (missing → 404)` gives `404`.
12. **OpenAPI**: `curl <orderingapi>/openapi/v1.json` → `paths["/api/orders/{id}"]` has exactly `get` (operationId `GetOrder`) and `put` (operationId `UpdateOrder`, JSON request body schema `OrderRequest`, responses `200` with schema `OrderDto` and `404`); `paths["/api/orders"]` has exactly `get` (`ListOrders`) and `post` (`CreateOrder`); no `delete` or `patch` anywhere under `/api/orders`.
13. **Traces**: Dashboard → Traces, filter resource `orderingapi`, open the check 4 `PUT /api/orders/{id}` → a server span with child database spans (`postgresql` / `orderingdb`): a `SELECT ... FROM "Orders" ... LEFT JOIN "OrderLines"`, then the save running `DELETE FROM "OrderLines"`, `INSERT INTO "OrderLines"` and `UPDATE "Orders"`; no span in error status. The check 9 request shows only the `SELECT`.
14. **Health**: `GET <orderingapi>/health` → `200` with body `Healthy`.
15. **Diff scope**: `git diff --stat main` lists only the files in Files touched.
16. **Non-goals held**: each command → no matches:
    - `git grep -nE "Migrations|WithDataVolume|HasData|MapDelete|MapPatch|HttpClient|catalogapi" -- src/EShopClaude.Ordering.Api`
    - `git grep -nE "order\.(Id|CreatedAt) =" -- src/EShopClaude.Ordering.Api`
    - `git grep -nE "IsConcurrencyToken|IsRowVersion|If-Match|ETag" -- src/EShopClaude.Ordering.Api`
    - `git grep -nw "Status" -- src/EShopClaude.Ordering.Api`

## References

- `PROJECT.md` §3.3 Ordering.Api: `PUT /api/orders/{id}` with `OrderRequest` → `200 OrderDto` / `404`, "replaces customer data and lines, recomputes `Total`"; "List, PUT and DELETE exist for CRUD completeness and the `.http` file; no storefront page uses them"; trusts its caller; no status field; no outgoing calls.
- `PROJECT.md` §2 Conventions: minimal APIs under `/api`, contracts duplicated per service, `decimal` money, totals computed never trusted, OpenAPI + `.http` per API.
- `PROJECT.md` §10 Deliberately left out (order status, shared contracts library, EF migrations).
- `PLAN.md` 3.2 (Ordering.Api spec backlog).
- `ordering-api/01-persistence.md`: required cascade relationship `Order.Lines`. `ordering-api/02-create-order.md`: `OrderRequest`, `OrderDto.From`, `Total` computation. `ordering-api/03-get-order.md`: `Results<Ok<OrderDto>, NotFound>` + `TypedResults` pattern this spec mirrors.
- `WOW.md`: branch, cycle and Definition of Done rules.
