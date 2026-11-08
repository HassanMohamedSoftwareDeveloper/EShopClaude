# Ordering.Api 03 — Get order by id

**Status:** Not started

## Goal

Return one stored order with its lines, or 404. The storefront's order page and the e2e test will use this. It also makes the `Location` header from create resolvable.

## Depends on

- `ordering-api/02-create-order.md`

## Scope

- [ ] `GET /api/orders/{id}` → `200 OrderDto` with lines, or `404`.
- [ ] `.http` file: get the created id, get a missing id.

## Explicit non-goals

- No list, PUT or DELETE (ordering-04 to ordering-06).
- No order history or filtering by buyer.

## Definition of Done

- `dotnet build src/EShopClaude.sln`: 0 errors, 0 new warnings.
- `aspire run`: every resource Running/Healthy.
- HTTP: POST an order, then GET the `Location` URL → 200, and the body equals the POST response (lines, total, createdAt).
- HTTP `GET /api/orders/999` → 404.
- Traces: request span with a child EF query, no errors.

## References

