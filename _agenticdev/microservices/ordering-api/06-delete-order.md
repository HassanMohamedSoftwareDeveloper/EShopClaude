# Ordering.Api 06 — Delete order

**Status:** Not started

## Goal

Remove an order and its lines, for CRUD completeness. No storefront page uses it.

## Context

- **Starting point on `main`** (after ordering-05):
  - AppHost wires `orderingapi` `.WithReference(orderingDb).WaitFor(orderingDb)`. Nothing in the AppHost changes in this spec.
  - `Program.cs` already has `using EShopClaude.Ordering.Api;`, `using EShopClaude.Ordering.Api.Data;`, `using Microsoft.AspNetCore.Http.HttpResults;` and `using Microsoft.EntityFrameworkCore;`. It registers `AddNpgsqlDbContext<OrderingDbContext>("orderingdb")`, runs `EnsureCreatedAsync()` at startup, has `UseExceptionHandler()` + `AddProblemDetails()`, and maps `POST /api/orders` (`CreateOrder`), `GET /api/orders` (`ListOrders`), `GET /api/orders/{id:int}` (`GetOrder`) and `PUT /api/orders/{id:int}` (`UpdateOrder`) on `var api = app.MapGroup("/api");`. No new `using` is needed (`ExecuteDeleteAsync` is in `Microsoft.EntityFrameworkCore`).
  - `Data/OrderingDbContext.cs` configures `Order.Lines` as a required one-way relationship with `.OnDelete(DeleteBehavior.Cascade)`. `EnsureCreatedAsync()` therefore creates the FK `FK_OrderLines_Orders_OrderId` with `ON DELETE CASCADE` in Postgres. No change to `Data/*`.
  - Every fresh run starts with 0 rows (no data volume), so ids start at `1`.
  - `EShopClaude.Ordering.Api.http` has `@host = http://localhost:5103`, status, OpenAPI, `### Create order`, `### List orders`, `### Get order (created)`, `### Get order (missing → 404)`, `### Update order (created)` and `### Update order (missing → 404)` requests.
- **Route**: `/orders/{id:int}` on the `api` group, `MapDelete`. It shares the path with `GetOrder` and `UpdateOrder`; the verb tells them apart. No request body.
- **Delete mechanism** (decided: `ExecuteDeleteAsync`): `db.Orders.Where(o => o.Id == id).ExecuteDeleteAsync(ct)` sends one `DELETE FROM "Orders" ... WHERE "Id" = @id` with no prior `SELECT` and no change tracking. Postgres `ON DELETE CASCADE` removes the order's `OrderLines` rows in the same statement. The returned row count decides the response: `> 0` → `204`, `0` → `404`.
- **Response** (mirrors `GetOrder`): the handler returns `Task<Results<NoContent, NotFound>>` via `TypedResults`, so OpenAPI documents `204` and `404` without extra metadata. Endpoint name `DeleteOrder`. Both responses have an empty body.
- **Repeat delete**: deleting an id that was already deleted is `404` (row count `0`), same as a never-existing id.
- **Trusts its caller** (PROJECT.md §3.3): no checks beyond route binding, no outgoing calls.

## Depends on

- `ordering-api/03-get-order.md`

## Scope

- [ ] `DELETE /api/orders/{id}` → `204`, or `404`. Lines are removed by the database cascade.
- [ ] `.http` file: delete existing, delete missing.

## Tasks

Run `dotnet build src/EShopClaude.sln` after each task.

1. **Ordering.Api `Program.cs`**: after `UpdateOrder` and before `app.MapDefaultEndpoints();`, map on the `api` group:
   ```csharp
   // Delete an order; Postgres ON DELETE CASCADE removes its lines (PROJECT.md §3.3). CRUD completeness; no storefront page uses it.
   api.MapDelete("/orders/{id:int}", async Task<Results<NoContent, NotFound>> (int id, OrderingDbContext db, CancellationToken ct) =>
           await db.Orders
               .Where(o => o.Id == id)
               .ExecuteDeleteAsync(ct) > 0
               ? TypedResults.NoContent()
               : TypedResults.NotFound())
       .WithName("DeleteOrder");
   ```
2. **`EShopClaude.Ordering.Api.http`**: after `### Update order (missing → 404)`, add two requests, no body:
   - `### Delete order (created)` → `DELETE {{host}}/api/orders/1`, comment `# id 1 = first Create order on a fresh run; 204, lines removed by cascade; run again → 404`.
   - `### Delete order (missing → 404)` → `DELETE {{host}}/api/orders/999`.
3. Run `aspire run` and walk the Definition of Done.

## Files touched

| File | Change |
|---|---|
| `src/EShopClaude.Ordering.Api/Program.cs` | changed: `DELETE /api/orders/{id:int}` (`DeleteOrder`) on the `/api` group |
| `src/EShopClaude.Ordering.Api/EShopClaude.Ordering.Api.http` | changed: delete existing and delete missing requests |
| `_agenticdev/microservices/ordering-api/06-delete-order.md`, `_agenticdev/microservices/README.md` | spec and status |

Not touched: AppHost, csproj files, appsettings files, `Data/*`, `OrderDto.cs`, `OrderLineDto.cs`, `OrderRequest.cs`, `OrderItemRequest.cs`, the `CreateOrder`, `ListOrders`, `GetOrder` and `UpdateOrder` handlers, ServiceDefaults, Catalog.Api, Basket.Api, Web.

## Explicit non-goals

- No soft delete, no `IsDeleted` / `DeletedAt` column, no status field.
- No bulk delete (`DELETE /api/orders`) and no line-level delete.
- No `DELETE` response body; no new DTO.
- No change to `Data/*` (the cascade already exists), no change to `POST`, list, get by id or `PUT`.
- No load-then-`Remove` round trip; no outgoing calls; no concurrency token or ETag / `If-Match` handling.

## Definition of Done

Each line is a check you can run, with its expected result. Shell commands use Git Bash syntax. `<orderingapi>` is the `orderingapi` http endpoint shown in the Aspire dashboard. `<container>` is the Postgres container from `docker ps --filter name=postgres`. `<psql>` stands for `docker exec <container> sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" psql -U postgres -d orderingdb -tAc "<query>"'`. Run checks 3–12 in order on a fresh `aspire run`, because the ids depend on it.

1. **Build**: `dotnet build src/EShopClaude.sln` → `0 Error(s)`, and the same warning count as the same command on `main`.
2. **Run**: `cd src && aspire run` → Dashboard → Resources: every resource shows State **Running** and Health **Healthy**.
3. **Cascade in schema**: `<psql>` with `SELECT confdeltype FROM pg_constraint WHERE conname = 'FK_OrderLines_Orders_OrderId'` → exactly one row, `c`.
4. **Create two orders**:
   ```sh
   curl -i -X POST <orderingapi>/api/orders -H "Content-Type: application/json" -d '{"buyerId":"dod-buyer","customerName":"Ada Lovelace","customerEmail":"ada@example.com","items":[{"productId":1,"productName":"Classic T-Shirt","unitPrice":19.99,"quantity":2},{"productId":6,"productName":"Backpack","unitPrice":59.00,"quantity":1}]}'
   curl -i -X POST <orderingapi>/api/orders -H "Content-Type: application/json" -d '{"buyerId":"dod-buyer-2","customerName":"Alan Turing","customerEmail":"alan@example.com","items":[{"productId":6,"productName":"Backpack","unitPrice":59.00,"quantity":1}]}'
   ```
   → `201 Created` with `Location: /api/orders/1`, then `201 Created` with `Location: /api/orders/2`. Note the second body as `<order2>`.
5. **Delete existing**: `curl -i -X DELETE <orderingapi>/api/orders/1` → `HTTP/1.1 204 No Content`, empty body.
6. **Get after delete**: `curl -i <orderingapi>/api/orders/1` → `HTTP/1.1 404 Not Found`, `Content-Length: 0`.
7. **Stored, `Orders`**: `<psql>` with `SELECT \"Id\",\"BuyerId\",\"Total\" FROM \"Orders\" ORDER BY \"Id\"` → exactly one row, `2|dod-buyer-2|59.00`.
8. **Stored, `OrderLines`**: `<psql>` with `SELECT \"Id\",\"OrderId\",\"ProductId\",\"Quantity\" FROM \"OrderLines\" ORDER BY \"Id\"` → exactly one row, `3|2|6|1`. No row with `OrderId` `1` (lines `1` and `2` are gone). pgAdmin (opened from the dashboard) shows the same rows under `orderingdb`.
9. **Other order untouched**: `curl -s <orderingapi>/api/orders/2` → body identical to `<order2>`; `curl -s <orderingapi>/api/orders` → array of length `1` whose `[0]` is identical to `<order2>`.
10. **Repeat delete**: `curl -i -X DELETE <orderingapi>/api/orders/1` → `HTTP/1.1 404 Not Found`, `Content-Length: 0`.
11. **Delete missing**: `curl -i -X DELETE <orderingapi>/api/orders/999` → `HTTP/1.1 404 Not Found`, `Content-Length: 0`. Then checks 7 and 8 return the same rows as before.
12. **Traces**: Dashboard → Traces, filter resource `orderingapi`, open the check 5 `DELETE /api/orders/{id}` → a server span with exactly one child database span (`postgresql` / `orderingdb`) running `DELETE FROM "Orders"` (no `SELECT`, no `DELETE FROM "OrderLines"`); no span in error status. The check 11 request shows the same single `DELETE` span.
13. **OpenAPI**: `curl <orderingapi>/openapi/v1.json` → `paths["/api/orders/{id}"]` has exactly `get` (`GetOrder`), `put` (`UpdateOrder`) and `delete` (operationId `DeleteOrder`, no `requestBody`, responses exactly `204` and `404`); `paths["/api/orders"]` has exactly `get` (`ListOrders`) and `post` (`CreateOrder`); no `patch` anywhere under `/api/orders`.
14. **.http file**: stop and restart `aspire run` (fresh, 0 rows), set `@host` to `<orderingapi>`, run `EShopClaude.Ordering.Api.http` top to bottom → status `200`, OpenAPI `200`, Create `201` (`Location: /api/orders/1`), List `200`, Get (created) `200`, Get (missing) `404`, Update (created) `200`, Update (missing) `404`, Delete (created) `204`, Delete (missing) `404`. Running `### Delete order (created)` once more → `404`.
15. **Health**: `GET <orderingapi>/health` → `200` with body `Healthy`.
16. **Diff scope**: `git diff --stat main` lists only the files in Files touched.
17. **Non-goals held**: each command → no matches:
    - `git grep -nE "Migrations|WithDataVolume|HasData|MapPatch|HttpClient|catalogapi" -- src/EShopClaude.Ordering.Api`
    - `git grep -nE "IsDeleted|DeletedAt|SoftDelete|\.Remove\(|RemoveRange" -- src/EShopClaude.Ordering.Api`
    - `git grep -nF 'MapDelete("/orders",' -- src/EShopClaude.Ordering.Api`
    - `git grep -nE "IsConcurrencyToken|IsRowVersion|If-Match|ETag" -- src/EShopClaude.Ordering.Api`
    - `git grep -nw "Status" -- src/EShopClaude.Ordering.Api`
    - `git diff main -- src/EShopClaude.Ordering.Api/Data src/EShopClaude.AppHost`

## References

- `PROJECT.md` §3.3 Ordering.Api: `DELETE /api/orders/{id}` → `204` / `404`; "List, PUT and DELETE exist for CRUD completeness and the `.http` file; no storefront page uses them"; trusts its caller; no status field; no outgoing calls.
- `PROJECT.md` §2 Conventions: minimal APIs under `/api`, `EnsureCreated()` with no migrations, OpenAPI + `.http` per API.
- `PROJECT.md` §10 Deliberately left out (order status, shared contracts library, EF migrations).
- `PLAN.md` 3.2 (Ordering.Api spec backlog).
- `ordering-api/01-persistence.md`: required `Order.Lines` relationship with `OnDelete(DeleteBehavior.Cascade)`. `ordering-api/03-get-order.md`: `Results<Ok<OrderDto>, NotFound>` + `TypedResults` pattern this spec mirrors. `ordering-api/05-update-order.md`: last handler on `main`, `.http` layout.
- `WOW.md`: branch, cycle and Definition of Done rules.

