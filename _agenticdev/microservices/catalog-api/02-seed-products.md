# Catalog.Api 02 — Seed products

**Status:** Not started

## Goal

Fill the catalog with a fixed set of about 6 products on startup, so every fresh run has data to list and add to a basket. The seed is idempotent: restarting the service never duplicates rows.

## Depends on

- `catalog-api/01-persistence.md`

## Scope

- [ ] After `EnsureCreated()`, insert about 6 products (name, description, price) only when `Products` is empty.

## Explicit non-goals

- No endpoints (catalog-03, catalog-04).
- No seed from file or config, no admin or write endpoints.
- No data volume. The seed runs again on every fresh AppHost run.

## Definition of Done

- `dotnet build src/EShopClaude.sln`: 0 errors, 0 new warnings.
- `aspire run`: every resource Running/Healthy.
- pgAdmin: `catalogdb.Products` has about 6 rows with distinct names and non-zero prices.
- Restart only `catalogapi` from the dashboard: same row count, no duplicates.
- Restart the AppHost: the table is recreated and reseeded with the same count.

## References

