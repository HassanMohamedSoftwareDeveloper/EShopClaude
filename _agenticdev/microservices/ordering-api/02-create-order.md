# Ordering.Api 02 — Create order

**Status:** Not started

## Goal

Let a caller store an order. `POST /api/orders` takes the buyer, customer data and lines as sent, stamps `CreatedAt`, computes `Total` from the lines, and returns the new order. Basket.Api's checkout will use this.

## Context

- **Starting point on `main`** (after ordering-01):
  - AppHost wires `orderingapi` `.WithReference(orderingDb).WaitFor(orderingDb)`. Nothing in the AppHost changes in this spec.
  - `Program.cs` registers `AddNpgsqlDbContext<OrderingDbContext>("orderingdb")` and runs `EnsureCreatedAsync()` in a startup scope. It also has `UseExceptionHandler()` + `AddProblemDetails()`, the root `MapGet("/")` and an empty group `var api = app.MapGroup("/api");` with the comment "Ordering endpoints are added to this group in Phase 3."
  - `Data/Order.cs`, `Data/OrderLine.cs` and `Data/OrderingDbContext.cs` exist as in ordering-01. Every fresh run starts with 0 rows.
  - `EShopClaude.Ordering.Api.http` has `@host = http://localhost:5103`, a status request and an OpenAPI request.
  - Catalog.Api (`ProductDto.cs`, the typed-results `GET /api/products/{id}` in `Program.cs`) is the reference for DTO files and `TypedResults`. Mirror it.
- **DTOs**: four positional records, one file each at the project root, namespace `EShopClaude.Ordering.Api`. They belong to Ordering.Api only: Basket.Api and Web declare their own (PROJECT.md §2 Conventions). System.Text.Json web defaults serialize them camelCase. ordering-03..05 reuse them.
- **Mapping**: `OrderDto.From(Order order)` is a static factory on the record. It is the only Entity→DTO mapping, and ordering-03..05 reuse it. Lines keep their list order. The DB line `Id` and `OrderId` are not exposed.
- **Handler**: it maps the request into a new `Order` with `CreatedAt = DateTimeOffset.UtcNow` (offset `+00:00`, which Npgsql requires for `timestamptz`) and `Total = Σ UnitPrice × Quantity` over the request items. It adds the order (the lines come along through `Order.Lines`), calls `SaveChangesAsync`, then returns `TypedResults.Created($"/api/orders/{order.Id}", OrderDto.From(order))`. The handler's return type is `Task<Created<OrderDto>>`, so OpenAPI documents the `201` response without extra metadata. Endpoint name: `CreateOrder`.
- **Location**: the header holds the relative path `/api/orders/{id}`. `CreatedAtRoute` is not used, because the GET route arrives only in ordering-03.
- **Trusts its caller** (PROJECT.md §3.3): there are no checks beyond model binding. Prices, names and quantities are stored as sent, with no Catalog call. A body that is not valid JSON gets the framework's `400`. A null `items` list or a null required string throws (an NRE, or a DB NOT NULL violation), which becomes a `500` ProblemDetails through `UseExceptionHandler`. Basket.Api never sends either. Any `total` in the body is ignored, because `OrderRequest` has no such member.
- **Response values**: `OrderDto` is built from the in-memory entity after `SaveChangesAsync`, so `id` is the Postgres identity value and `total` is the computed `decimal` (for example `98.98`).

## Depends on

- `ordering-api/01-persistence.md`

## Scope

- [ ] `OrderRequest { buyerId, customerName, customerEmail, items[{ productId, productName, unitPrice, quantity }] }` record.
- [ ] `OrderDto` (id, buyerId, customerName, customerEmail, createdAt, total, lines) record.
- [ ] `POST /api/orders` → `201 OrderDto` with `Location: /api/orders/{id}`, `CreatedAt` = UTC now, `Total` = Σ unitPrice × quantity.
- [ ] `.http` file: create request.

## Data model

No schema change; tables as in ordering-01. Wire shapes (JSON camelCase):

`OrderRequest` (POST body), file `OrderRequest.cs`:

| Member | CLR type | JSON |
|---|---|---|
| BuyerId | `string` | `buyerId` |
| CustomerName | `string` | `customerName` |
| CustomerEmail | `string` | `customerEmail` |
| Items | `List<OrderItemRequest>` | `items` |

`OrderItemRequest`, file `OrderItemRequest.cs`:

| Member | CLR type | JSON |
|---|---|---|
| ProductId | `int` | `productId` |
| ProductName | `string` | `productName` |
| UnitPrice | `decimal` | `unitPrice` |
| Quantity | `int` | `quantity` |

`OrderDto` (response), file `OrderDto.cs`, with `public static OrderDto From(Order order)`:

| Member | CLR type | JSON | Source |
|---|---|---|---|
| Id | `int` | `id` | `Order.Id` |
| BuyerId | `string` | `buyerId` | `Order.BuyerId` |
| CustomerName | `string` | `customerName` | `Order.CustomerName` |
| CustomerEmail | `string` | `customerEmail` | `Order.CustomerEmail` |
| CreatedAt | `DateTimeOffset` | `createdAt` | `Order.CreatedAt` (UTC, `+00:00`) |
| Total | `decimal` | `total` | `Order.Total` |
| Lines | `List<OrderLineDto>` | `lines` | `Order.Lines`, in order |

`OrderLineDto`, file `OrderLineDto.cs`:

| Member | CLR type | JSON | Source |
|---|---|---|---|
| ProductId | `int` | `productId` | `OrderLine.ProductId` |
| ProductName | `string` | `productName` | `OrderLine.ProductName` |
| UnitPrice | `decimal` | `unitPrice` | `OrderLine.UnitPrice` |
| Quantity | `int` | `quantity` | `OrderLine.Quantity` |

No `total` member on the request, no `status` anywhere, and no line `id` / `orderId` on the response.

## Tasks

Run `dotnet build src/EShopClaude.sln` after each task.

1. **Ordering.Api**: add `OrderItemRequest.cs`: `public record OrderItemRequest(int ProductId, string ProductName, decimal UnitPrice, int Quantity);` in namespace `EShopClaude.Ordering.Api`.
2. **Ordering.Api**: add `OrderRequest.cs`: `public record OrderRequest(string BuyerId, string CustomerName, string CustomerEmail, List<OrderItemRequest> Items);`, same namespace.
3. **Ordering.Api**: add `OrderLineDto.cs`: `public record OrderLineDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity);`, same namespace.
4. **Ordering.Api**: add `OrderDto.cs`:
   ```csharp
   public record OrderDto(int Id, string BuyerId, string CustomerName, string CustomerEmail,
       DateTimeOffset CreatedAt, decimal Total, List<OrderLineDto> Lines)
   {
       public static OrderDto From(Order order) => new(
           order.Id, order.BuyerId, order.CustomerName, order.CustomerEmail,
           order.CreatedAt, order.Total,
           [.. order.Lines.Select(l => new OrderLineDto(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity))]);
   }
   ```
   (`using EShopClaude.Ordering.Api.Data;`)
5. **Ordering.Api `Program.cs`**: add `using EShopClaude.Ordering.Api;` and `using Microsoft.AspNetCore.Http.HttpResults;`. Replace the "added in Phase 3" comment and map on the `api` group:
   ```csharp
   // Store an order as sent; Ordering computes Total and trusts its caller (PROJECT.md §3.3).
   api.MapPost("/orders", async Task<Created<OrderDto>> (OrderRequest request, OrderingDbContext db, CancellationToken ct) =>
       {
           var order = new Order
           {
               BuyerId = request.BuyerId,
               CustomerName = request.CustomerName,
               CustomerEmail = request.CustomerEmail,
               CreatedAt = DateTimeOffset.UtcNow,
               Total = request.Items.Sum(i => i.UnitPrice * i.Quantity),
               Lines = [.. request.Items.Select(i => new OrderLine
               {
                   ProductId = i.ProductId,
                   ProductName = i.ProductName,
                   UnitPrice = i.UnitPrice,
                   Quantity = i.Quantity,
               })],
           };

           db.Orders.Add(order);
           await db.SaveChangesAsync(ct);

           return TypedResults.Created($"/api/orders/{order.Id}", OrderDto.From(order));
       })
       .WithName("CreateOrder");
   ```
6. **`EShopClaude.Ordering.Api.http`**: add `### Create order`, a `POST {{host}}/api/orders` with `Content-Type: application/json` and the two-line body from DoD check 3.
7. Run `aspire run` and walk the Definition of Done.

## Files touched

| File | Change |
|---|---|
| `src/EShopClaude.Ordering.Api/OrderRequest.cs` | new |
| `src/EShopClaude.Ordering.Api/OrderItemRequest.cs` | new |
| `src/EShopClaude.Ordering.Api/OrderDto.cs` | new |
| `src/EShopClaude.Ordering.Api/OrderLineDto.cs` | new |
| `src/EShopClaude.Ordering.Api/Program.cs` | changed: `POST /api/orders` on the `/api` group |
| `src/EShopClaude.Ordering.Api/EShopClaude.Ordering.Api.http` | changed: create request |
| `_agenticdev/microservices/ordering-api/02-create-order.md`, `_agenticdev/microservices/README.md` | spec and status |

Not touched: AppHost, csproj files, appsettings files, `Data/*`, ServiceDefaults, Catalog.Api, Basket.Api, Web.

## Explicit non-goals

- No GET, PUT or DELETE (ordering-03 to ordering-06).
- No Catalog call, no price check, no validation beyond model binding (Ordering trusts its caller).
- No `Total` taken from input, no status field.

## Definition of Done

Each line is a check you can run, with its expected result. Shell commands use Git Bash syntax. `<orderingapi>` is the `orderingapi` http endpoint shown in the Aspire dashboard. `<container>` is the Postgres container from `docker ps --filter name=postgres`. Run the checks in order on a fresh `aspire run`, because the ids depend on it.

1. **Build**: `dotnet build src/EShopClaude.sln` → `0 Error(s)`, and the same warning count as the same command on `main`.
2. **Run**: `cd src && aspire run` → Dashboard → Resources: every resource shows State **Running** and Health **Healthy**.
3. **Create**:
   ```sh
   curl -i -X POST <orderingapi>/api/orders -H "Content-Type: application/json" -d '{"buyerId":"dod-buyer","customerName":"Ada Lovelace","customerEmail":"ada@example.com","items":[{"productId":1,"productName":"Classic T-Shirt","unitPrice":19.99,"quantity":2},{"productId":6,"productName":"Backpack","unitPrice":59.00,"quantity":1}]}'
   ```
   → `HTTP/1.1 201 Created`, `Location: /api/orders/1`, `Content-Type: application/json`, and a body with exactly the keys `id`, `buyerId`, `customerName`, `customerEmail`, `createdAt`, `total`, `lines`:
   - `id` = `1`; `buyerId`, `customerName` and `customerEmail` as sent;
   - `createdAt` is an ISO timestamp ending in `+00:00`, within a minute of now;
   - `total` = `98.98`;
   - `lines` = `[{"productId":1,"productName":"Classic T-Shirt","unitPrice":19.99,"quantity":2},{"productId":6,"productName":"Backpack","unitPrice":59.00,"quantity":1}]`, so each line has exactly those 4 keys and no `id` or `orderId`.
4. **Stored, `Orders`**: `docker exec <container> sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" psql -U postgres -d orderingdb -tAc "SELECT \"Id\",\"BuyerId\",\"CustomerName\",\"CustomerEmail\",\"Total\" FROM \"Orders\""'` → exactly one row, `1|dod-buyer|Ada Lovelace|ada@example.com|98.98`.
5. **Stored, `OrderLines`**: `docker exec <container> sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" psql -U postgres -d orderingdb -tAc "SELECT \"OrderId\",\"ProductId\",\"ProductName\",\"UnitPrice\",\"Quantity\" FROM \"OrderLines\" ORDER BY \"Id\""'` → exactly two rows, `1|1|Classic T-Shirt|19.99|2` and `1|6|Backpack|59.00|1`. pgAdmin (opened from the dashboard) shows the same rows under `orderingdb`.
6. **Total not trusted**: the same request as check 3, with `"total":1` added to the body → `201`, `Location: /api/orders/2`, body `total` = `98.98`.
7. **Caller trusted, no Catalog call**: `curl -i -X POST <orderingapi>/api/orders -H "Content-Type: application/json" -d '{"buyerId":"dod-buyer","customerName":"Ada Lovelace","customerEmail":"ada@example.com","items":[{"productId":999,"productName":"Not in catalog","unitPrice":5.50,"quantity":3}]}'` → `201`, `Location: /api/orders/3`, `total` = `16.50`, line stored as sent. Dashboard → Traces: this request has no span on `catalogapi`.
8. **.http file**: `EShopClaude.Ordering.Api.http` → `### Create order` (with `@host` set to `<orderingapi>`) → `201` with the check 3 body and the next id.
9. **OpenAPI**: `curl <orderingapi>/openapi/v1.json` → `paths["/api/orders"].post` exists with operationId `CreateOrder`, a JSON request body schema `OrderRequest` (`buyerId`, `customerName`, `customerEmail`, `items` of `OrderItemRequest`), and a `201` response schema `OrderDto`. There is no `get`, `put` or `delete` under `/api/orders`, and no `/api/orders/{id}` path.
10. **Traces**: Dashboard → Traces, filter resource `orderingapi`, open the check 3 `POST /api/orders` → a server span with child database spans (`postgresql` / `orderingdb`) running `INSERT INTO "Orders"` and `INSERT INTO "OrderLines"`, and no span in error status.
11. **Health**: `GET <orderingapi>/health` → `200` with body `Healthy`.
12. **Diff scope**: `git diff --stat main` lists only the files in Files touched.
13. **Non-goals held**: each command → no matches:
    - `git grep -nE "Migrations|WithDataVolume|HasData|Map(Get|Put|Delete|Patch)\(\"/orders|HttpClient|catalogapi" -- src/EShopClaude.Ordering.Api`
    - `git grep -nw "Status" -- src/EShopClaude.Ordering.Api`
    - `git grep -n "Total" -- src/EShopClaude.Ordering.Api/OrderRequest.cs src/EShopClaude.Ordering.Api/OrderItemRequest.cs`

## References

- `PROJECT.md` §3.3 Ordering.Api: `POST /api/orders` → `201 OrderDto`, `Location: /api/orders/{id}`; `OrderRequest` shape; `Total` computed at create; trusts its caller; no status field.
- `PROJECT.md` §3.2 Checkout, step 2: Basket.Api is the caller (basket-05).
- `PROJECT.md` §2 Conventions: minimal APIs under `/api`, contracts duplicated per service, `decimal` money, OpenAPI + `.http` per API.
- `PROJECT.md` §6 Checkout sequence and §10 Deliberately left out.
- `PLAN.md` 3.2 (Ordering.Api spec backlog).
- `ordering-api/01-persistence.md`: entities and schema this builds on. `catalog-api/03-list-products.md` / `04-get-product.md`: DTO-file and `TypedResults` pattern.
- `WOW.md`: branch, cycle and Definition of Done rules.

