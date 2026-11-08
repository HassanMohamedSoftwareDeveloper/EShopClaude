# Catalog.Api 04 — Get product by id

**Status:** In progress

## Goal

Return a single product by id, or 404. Basket.Api will call this when an item is added, to take a snapshot of the name and price.

## Context

- **Starting point on `main`** (after catalog-03):
  - `Program.cs` registers `AddNpgsqlDbContext<CatalogDbContext>("catalogdb")`, runs `EnsureCreatedAsync()` then `CatalogSeed.SeedAsync(db)` in a startup scope, and maps `GET /api/products` (`ListProducts`) on `var api = app.MapGroup("/api");`.
  - `ProductDto.cs` holds `public record ProductDto(int Id, string Name, string Description, decimal Price);`. This spec reuses it unchanged.
  - Every fresh run holds 6 seeded products with ids 1..6.
  - `EShopClaude.Catalog.Api.http` has `@host = http://localhost:5101`, status, OpenAPI and list requests.
- **Route**: `/products/{id:int}` on the `api` group. The `int` constraint means a non-numeric id never matches and routing returns 404.
- **Query**: `AsNoTracking()`, `Where(p => p.Id == id)`, projected with `Select` into `ProductDto`, then `FirstOrDefaultAsync(ct)`. Same projection as `ListProducts`, so both routes return identical data for the same id.
- **Response**: the handler returns `Results<Ok<ProductDto>, NotFound>` via `TypedResults`, so OpenAPI documents both the `200 ProductDto` and the `404` response without extra metadata. The 404 has an empty body. Endpoint name `GetProduct`.

## Depends on

- `catalog-api/03-list-products.md`

## Scope

- [ ] `GET /api/products/{id}` → `200 ProductDto`, or `404` when the id is unknown.
- [ ] `.http` file: get existing id, get missing id.

## Tasks

Run `dotnet build src/EShopClaude.sln` after each task.

1. **Catalog.Api `Program.cs`**: add `using Microsoft.AspNetCore.Http.HttpResults;`. After `ListProducts`, map on the `api` group:
   ```csharp
   api.MapGet("/products/{id:int}", async Task<Results<Ok<ProductDto>, NotFound>> (int id, CatalogDbContext db, CancellationToken ct) =>
           await db.Products
               .AsNoTracking()
               .Where(p => p.Id == id)
               .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price))
               .FirstOrDefaultAsync(ct) is { } product
               ? TypedResults.Ok(product)
               : TypedResults.NotFound())
       .WithName("GetProduct");
   ```
2. **`EShopClaude.Catalog.Api.http`**: add `### Get product (existing)` → `GET {{host}}/api/products/1` and `### Get product (missing → 404)` → `GET {{host}}/api/products/999`.
3. Run `aspire run` and walk the Definition of Done.

## Files touched

| File | Change |
|---|---|
| `src/EShopClaude.Catalog.Api/Program.cs` | changed: `GET /api/products/{id}` on the `/api` group |
| `src/EShopClaude.Catalog.Api/EShopClaude.Catalog.Api.http` | changed: get existing + get missing requests |
| `_agenticdev/microservices/catalog-api/04-get-product.md`, `_agenticdev/microservices/README.md` | spec and status |

Not touched: AppHost, csproj files, appsettings files, `Data/*`, `ProductDto.cs`, ServiceDefaults, other services.

## Explicit non-goals

- No write endpoints.
- No product detail page or extra fields.
- No shared contracts project.

## Definition of Done

Each line is a check you can run, with its expected result. `<catalogapi>` is the `catalogapi` http endpoint shown in the Aspire dashboard.

1. **Build**: `dotnet build src/EShopClaude.sln` → `0 Error(s)`, `0 Warning(s)` (same as `main`).
2. **Run**: `cd src && aspire run` → Dashboard → Resources: every resource shows State **Running** and Health **Healthy**.
3. **Get existing**: `curl -i <catalogapi>/api/products/1` → `HTTP/1.1 200`, `Content-Type: application/json`, body `{"id":1,"name":"Classic T-Shirt","description":"Soft cotton crew-neck tee.","price":19.99}`, identical to item 1 of `GET <catalogapi>/api/products`.
4. **Get missing**: `curl -i <catalogapi>/api/products/999` → `HTTP/1.1 404`, empty body.
5. **.http file**: `EShopClaude.Catalog.Api.http` (with `@host` set to `<catalogapi>`) → `### Get product (existing)` gives check 3's result, `### Get product (missing → 404)` gives check 4's.
6. **OpenAPI**: `curl <catalogapi>/openapi/v1.json` → `paths["/api/products"].get` (operationId `ListProducts`) and `paths["/api/products/{id}"].get` (operationId `GetProduct`, responses `200` with `ProductDto` schema and `404`).
7. **Traces**: Dashboard → Traces, filter resource `catalogapi`, open `GET /api/products/{id}` → a server span with a child database span (`postgresql` / `catalogdb`) running `SELECT ... FROM "Products" ... WHERE p."Id" = @id`. The request for id `999` shows the same shape with no span in error status.
8. **Health**: `GET <catalogapi>/health` → `200` with body `Healthy`.
9. **Diff scope**: `git diff --stat main` lists only the files in Files touched.
10. **Non-goals held**: `git grep -nE "Migrations|WithDataVolume|HasData|MapPost|MapPut|MapDelete|MapPatch" -- src/EShopClaude.Catalog.Api` → no matches.

## References

- `PROJECT.md` §3.1 Catalog.Api: `GET /api/products/{id}` → `200 ProductDto` / `404`; no write endpoints.
- `PROJECT.md` §3.2 Basket.Api add item: calls `GET catalogapi /api/products/{productId}`, maps `404` → `400`.
- `PROJECT.md` §2 Architecture → Conventions: contracts duplicated per service, no shared contracts library.
- `PROJECT.md` §10 Deliberately left out.
- `PLAN.md` 3.1 (Catalog.Api spec backlog).
- `WOW.md`: branch, cycle and Definition of Done rules.
