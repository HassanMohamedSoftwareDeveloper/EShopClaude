# Catalog.Api 04 — Get product by id

**Status:** Not started

## Goal

Return a single product by id, or 404. Basket.Api will call this when an item is added, to take a snapshot of the name and price.

## Depends on

- `catalog-api/03-list-products.md`

## Scope

- [ ] `GET /api/products/{id}` → `200 ProductDto`, or `404` when the id is unknown.
- [ ] `.http` file: get existing id, get missing id.

## Explicit non-goals

- No write endpoints.
- No product detail page or extra fields.

## Definition of Done

- `dotnet build src/EShopClaude.sln`: 0 errors, 0 new warnings.
- `aspire run`: every resource Running/Healthy.
- HTTP `GET /api/products/1` → 200 with the same data as item 1 in the list.
- HTTP `GET /api/products/999` → 404.
- `/openapi/v1.json` lists both product routes.
- Traces: request span with a child EF query span, no errors on the 404 path.

## References

