# Ordering.Api 04 — List orders

**Status:** Not started

## Goal

Return every stored order, ordered by Id, for CRUD completeness and manual checks from the `.http` file. No storefront page uses it.

## Depends on

- `ordering-api/02-create-order.md`

## Scope

- [ ] `GET /api/orders` → `200 OrderDto[]` with lines, ordered by Id.
- [ ] `.http` file: list request.

## Explicit non-goals

- No paging, filtering by buyer, or order history page.
- No PUT or DELETE (ordering-05, ordering-06).

## Definition of Done

- `dotnet build src/EShopClaude.sln`: 0 errors, 0 new warnings.
- `aspire run`: every resource Running/Healthy.
- HTTP on a fresh run: `GET /api/orders` → 200 `[]`.
- HTTP: POST two orders, then list → 200, both present in ascending id order, with lines.

## References

