# Catalog.Api 03 — List products

**Status:** Not started

## Goal

Expose the catalog over HTTP. `GET /api/products` returns every product, ordered by Id. This is what the storefront's Products page will use.

## Depends on

- `catalog-api/02-seed-products.md`

## Scope

- [ ] `ProductDto { id, name, description, price }` record, owned by Catalog.Api.
- [ ] `GET /api/products` in the `/api` group → `200 ProductDto[]` ordered by Id.
- [ ] `.http` file: list request.

## Explicit non-goals

- No get-by-id (catalog-04).
- No write endpoints, paging, search or filtering.
- No shared contracts project.

## Definition of Done

- `dotnet build src/EShopClaude.sln`: 0 errors, 0 new warnings.
- `aspire run`: every resource Running/Healthy.
- HTTP `GET /api/products` → 200, about 6 items ordered by ascending id, each with id, name, description, price.
- `/openapi/v1.json` lists `GET /api/products`.
- Traces: request span for `catalogapi` with a child EF query span, no errors.

## References

