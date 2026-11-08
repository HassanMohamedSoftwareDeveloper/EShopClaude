# Catalog.Api 02 — Seed products

**Status:** In progress

## Goal

Fill the catalog with a fixed set of 6 products on startup, so every fresh run has data to list and add to a basket. The seed is idempotent: restarting the service never duplicates rows.

## Context

- **Starting point on `main`** (after catalog-01):
  - `catalogapi` references and waits for `catalogdb`.
  - `Data/Product.cs` and `Data/CatalogDbContext.cs` exist; the `"Products"` table has Id (identity), Name (`varchar(100)`), Description (`varchar(500)`), Price (`numeric(18,2)`).
  - `Program.cs` creates a DI scope after `builder.Build()`, resolves `CatalogDbContext` and awaits `Database.EnsureCreatedAsync()`.
- **Seed placement**: a static `CatalogSeed.SeedAsync(CatalogDbContext)` in `Data/CatalogSeed.cs`, called in the same startup scope right after `EnsureCreatedAsync()`. No request is served before the seed finishes, and a seed failure crashes startup.
- **Idempotency**: the seed returns immediately when `Products` has any row (`AnyAsync`). It inserts all 6 rows in one `SaveChangesAsync`, so the table is either empty or fully seeded.
- **Ids**: not set by the seed. On the ephemeral database Postgres assigns 1..6 in insert order, which later specs and the e2e test can rely on.
- **Not `HasData`**: model seeding is not used. The insert-when-empty check is explicit code, and catalog-01's non-goal check forbids `HasData`.

## Depends on

- `catalog-api/01-persistence.md`

## Scope

- [ ] After `EnsureCreated()`, insert 6 products (name, description, price) only when `Products` is empty.

## Seed data

Inserted in this order:

| # | Name | Description | Price |
|---|---|---|---|
| 1 | Classic T-Shirt | Soft cotton crew-neck tee. | 19.99 |
| 2 | Hoodie | Warm fleece pullover hoodie. | 49.99 |
| 3 | Baseball Cap | Adjustable cotton cap. | 14.50 |
| 4 | Coffee Mug | 350 ml ceramic mug. | 9.99 |
| 5 | Water Bottle | 750 ml stainless steel bottle. | 24.00 |
| 6 | Backpack | 20 L everyday backpack. | 59.00 |

Names are distinct and fit 100 characters, descriptions fit 500, prices are non-zero `decimal` literals.

## Tasks

Run `dotnet build src/EShopClaude.sln` after each task.

1. **Catalog.Api**: add `Data/CatalogSeed.cs` (namespace `EShopClaude.Catalog.Api.Data`) with `public static class CatalogSeed` and `public static async Task SeedAsync(CatalogDbContext db)`:
   - return when `await db.Products.AnyAsync()` is true;
   - otherwise `AddRange` the 6 products from Seed data in order and `await db.SaveChangesAsync()`.
2. **Catalog.Api `Program.cs`**: in the existing startup scope, after `EnsureCreatedAsync()`, `await CatalogSeed.SeedAsync(db)`. Nothing else changes.
3. Run `aspire run` and walk the Definition of Done.

## Files touched

| File | Change |
|---|---|
| `src/EShopClaude.Catalog.Api/Data/CatalogSeed.cs` | new |
| `src/EShopClaude.Catalog.Api/Program.cs` | changed: seed call after `EnsureCreatedAsync` |
| `_agenticdev/microservices/catalog-api/02-seed-products.md`, `_agenticdev/microservices/README.md` | spec, and status at Mark Done |

Not touched: AppHost, csproj files, appsettings files, `EShopClaude.Catalog.Api.http` (no endpoints yet), `Product`, `CatalogDbContext`, ServiceDefaults, other services.

## Explicit non-goals

- No endpoints (catalog-03, catalog-04).
- No seed from file or config, no admin or write endpoints.
- No data volume. The seed runs again on every fresh AppHost run.
- No `HasData` model seeding, no migrations.

## Definition of Done

Each line is a check you can run, with its expected result. `<c>` is the container from `docker ps --filter name=postgres`; `PSQL "<sql>"` means `docker exec <c> sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" psql -U postgres -d catalogdb -tAc "<sql>"'` (quote identifiers as `\"Products\"`).

1. **Build**: `dotnet build src/EShopClaude.sln` → `0 Error(s)`, and the same warning count as the same command on `main`.
2. **Run**: `cd src && aspire run` → Dashboard → Resources: every resource shows State **Running** and Health **Healthy**.
3. **Rows**: `PSQL "SELECT count(*), count(DISTINCT \"Name\"), min(\"Price\") > 0 FROM \"Products\""` → `6|6|t`.
4. **Content**: `PSQL "SELECT \"Id\", \"Name\", \"Price\" FROM \"Products\" ORDER BY \"Id\""` → six rows, ids 1..6, names and prices as in Seed data. pgAdmin (opened from the dashboard) shows the same rows under `catalogdb` → Schemas → public → Tables → Products.
5. **Logs**: Dashboard → Structured logs, resource `catalogapi`, filter `INSERT INTO "Products"` → exactly one entry, category `Microsoft.EntityFrameworkCore.Database.Command`.
6. **Restart is safe**: Dashboard → `catalogapi` → Restart. Expect:
   - it returns to Running/Healthy;
   - Console logs show no exception;
   - check 3 still returns `6|6|t`;
   - check 5 still shows exactly one `INSERT INTO "Products"` entry (from the first start).
7. **Fresh run reseeds**: stop the AppHost, then `cd src && aspire run` again → checks 3 and 4 return the same results.
8. **Health**: `GET <catalogapi endpoint from dashboard>/health` → `200` with body `Healthy`.
9. **Diff scope**: `git diff --stat main` lists only the files in Files touched.
10. **Non-goals held**: `git grep -nE "Migrations|WithDataVolume|HasData|MapGet\(\"/api" -- src` → no matches.

## References

- `PROJECT.md` §3.1 Catalog.Api: "On startup: `EnsureCreated()`, then seed ~6 products if the table is empty."
- `PROJECT.md` §7 Persistence (ephemeral: catalog reseeded each AppHost run) and §10 Deliberately left out.
- `PLAN.md` 3.1 (Catalog.Api spec backlog).
- `WOW.md`: branch, cycle and Definition of Done rules.
