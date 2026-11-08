# Catalog.Api 03 — List products

**Status:** In progress

## Goal

Expose the catalog over HTTP. `GET /api/products` returns every product, ordered by Id. This is what the storefront's Products page will use.

## Context

- **Starting point on `main`** (after catalog-02):
  - `Program.cs` registers `AddNpgsqlDbContext<CatalogDbContext>("catalogdb")`, runs `EnsureCreatedAsync()` then `CatalogSeed.SeedAsync(db)` in a startup scope, and creates an empty group `var api = app.MapGroup("/api");`.
  - `Data/Product.cs` (Id, Name, Description, Price) and `Data/CatalogDbContext.cs` (`DbSet<Product> Products`) exist. Every fresh run holds 6 seeded products with ids 1..6.
  - `EShopClaude.Catalog.Api.http` has `@host = http://localhost:5101`, a status request and an OpenAPI request.
- **DTO**: `ProductDto` is a positional record in `ProductDto.cs` at the project root, namespace `EShopClaude.Catalog.Api`. It belongs to Catalog.Api only; Basket.Api and Web declare their own. catalog-04 reuses it. System.Text.Json web defaults serialize it camelCase: `{ id, name, description, price }`.
- **Query**: `AsNoTracking()`, `OrderBy(p => p.Id)`, projected with `Select` into `ProductDto` so the SQL reads only the 4 columns and the `Product` entity never leaves the service.
- **Response**: the handler returns `List<ProductDto>`, so minimal APIs reply `200` with a JSON array and OpenAPI documents the 200 response schema without extra metadata. Endpoint name `ListProducts`.

## Depends on

- `catalog-api/02-seed-products.md`

## Scope

- [ ] `ProductDto { id, name, description, price }` record, owned by Catalog.Api.
- [ ] `GET /api/products` in the `/api` group → `200 ProductDto[]` ordered by Id.
- [ ] `.http` file: list request.

## Tasks

Run `dotnet build src/EShopClaude.sln` after each task.

1. **Catalog.Api**: add `ProductDto.cs` with `public record ProductDto(int Id, string Name, string Description, decimal Price);` in namespace `EShopClaude.Catalog.Api`.
2. **Catalog.Api `Program.cs`**: add `using Microsoft.EntityFrameworkCore;`. Replace the "added in Phase 3" comment and map on the `api` group:
   ```csharp
   api.MapGet("/products", async (CatalogDbContext db, CancellationToken ct) =>
           await db.Products
               .AsNoTracking()
               .OrderBy(p => p.Id)
               .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price))
               .ToListAsync(ct))
       .WithName("ListProducts");
   ```
3. **`EShopClaude.Catalog.Api.http`**: add `### List products` → `GET {{host}}/api/products`.
4. Run `aspire run` and walk the Definition of Done.

## Files touched

| File | Change |
|---|---|
| `src/EShopClaude.Catalog.Api/ProductDto.cs` | new |
| `src/EShopClaude.Catalog.Api/Program.cs` | changed: `GET /api/products` on the `/api` group |
| `src/EShopClaude.Catalog.Api/EShopClaude.Catalog.Api.http` | changed: list request |
| `_agenticdev/microservices/catalog-api/03-list-products.md`, `_agenticdev/microservices/README.md` | spec and status |

Not touched: AppHost, csproj files, appsettings files, `Data/*`, ServiceDefaults, other services.

## Explicit non-goals

- No get-by-id (catalog-04).
- No write endpoints, paging, search or filtering.
- No shared contracts project.

## Definition of Done

Each line is a check you can run, with its expected result. `<catalogapi>` is the `catalogapi` http endpoint shown in the Aspire dashboard.

1. **Build**: `dotnet build src/EShopClaude.sln` → `0 Error(s)`, `0 Warning(s)` (same as `main`).
2. **Run**: `cd src && aspire run` → Dashboard → Resources: every resource shows State **Running** and Health **Healthy**.
3. **List**: `curl -i <catalogapi>/api/products` → `HTTP/1.1 200`, `Content-Type: application/json`, body is a JSON array of exactly 6 objects:
   - ids `1,2,3,4,5,6` in that order;
   - each object has exactly the keys `id`, `name`, `description`, `price`;
   - values match the catalog-02 seed table, e.g. `{"id":1,"name":"Classic T-Shirt","description":"Soft cotton crew-neck tee.","price":19.99}` and `{"id":6,"name":"Backpack","description":"20 L everyday backpack.","price":59.00}`.
4. **.http file**: `EShopClaude.Catalog.Api.http` → `### List products` (with `@host` set to `<catalogapi>`) → same result as check 3.
5. **OpenAPI**: `curl <catalogapi>/openapi/v1.json` → `paths["/api/products"].get` exists with operationId `ListProducts` and a `200` response whose schema is an array of `ProductDto` (`id`, `name`, `description`, `price`). No `/api/products/{id}` path.
6. **Traces**: Dashboard → Traces, filter resource `catalogapi`, open `GET /api/products` → a server span with a child database span (`postgresql` / `catalogdb`) running `SELECT ... FROM "Products" ... ORDER BY p."Id"`, and no span in error status.
7. **Health**: `GET <catalogapi>/health` → `200` with body `Healthy`.
8. **Diff scope**: `git diff --stat main` lists only the files in Files touched.
9. **Non-goals held**: `git grep -nE "Migrations|WithDataVolume|HasData|products/\{|MapPost|MapPut|MapDelete|MapPatch" -- src` → no matches.

## References

- `PROJECT.md` §3.1 Catalog.Api: `GET /api/products` → `200 ProductDto[]` ordered by Id; `ProductDto { id, name, description, price }`; no write endpoints.
- `PROJECT.md` §2 Architecture → Conventions: contracts duplicated per service, no shared contracts library.
- `PROJECT.md` §10 Deliberately left out.
- `PLAN.md` 3.1 (Catalog.Api spec backlog).
- `WOW.md`: branch, cycle and Definition of Done rules.
