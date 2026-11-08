# Catalog.Api 01 — Persistence

**Status:** Not started

## Goal

Give Catalog.Api its own database schema in `catalogdb`. After this spec the service connects to Postgres through Aspire, waits for the database before it starts, and creates the `Products` table on startup. The seed and the endpoints build on this.

## Depends on

Nothing. PLAN 1.x and 2.1 (skeleton, infra resources) are already merged.

## Scope

- [ ] AppHost: `catalogapi` `.WithReference(catalogDb).WaitFor(catalogDb)`.
- [ ] `Product` entity: Id (int PK), Name, Description, Price (`decimal`).
- [ ] `CatalogDbContext` with `DbSet<Product> Products`.
- [ ] `AddNpgsqlDbContext<CatalogDbContext>("catalogdb")` in `Program.cs`.
- [ ] Startup: `Database.EnsureCreated()`.
- Why AppHost wiring and DbContext registration share one spec: `AddNpgsqlDbContext` throws when the `catalogdb` connection string is missing, so neither part can be checked alone.

## Explicit non-goals

- No seed data (catalog-02).
- No endpoints or DTOs (catalog-03, catalog-04).
- No EF migrations, no data volume.
- No images, stock, categories or brands on `Product`.

## Definition of Done

- `dotnet build src/EShopClaude.sln`: 0 errors, 0 new warnings.
- `aspire run`: every resource Running/Healthy, and `catalogapi` starts only after `catalogdb` is healthy.
- Dashboard: `catalogapi` env vars contain `ConnectionStrings__catalogdb`.
- pgAdmin: `catalogdb` has a `Products` table with columns Id, Name, Description, Price, and 0 rows.
- Traces/logs: EF Core DB command for `EnsureCreated` visible under `catalogapi`.

## References

