# Ordering.Api 06 — Delete order

**Status:** Not started

## Goal

Remove an order and its lines, for CRUD completeness. No storefront page uses it.

## Depends on

- `ordering-api/03-get-order.md`

## Scope

- [ ] `DELETE /api/orders/{id}` → `204`, or `404`. Lines are removed by cascade.
- [ ] `.http` file: delete existing, delete missing.

## Explicit non-goals

- No soft delete or status field.
- No bulk delete.

## Definition of Done

- `dotnet build src/EShopClaude.sln`: 0 errors, 0 new warnings.
- `aspire run`: every resource Running/Healthy.
- HTTP: POST an order, DELETE it → 204, then GET by id → 404.
- HTTP `DELETE /api/orders/999` → 404.
- pgAdmin: no `Orders` or `OrderLines` rows left for the deleted id.
- All `.http` requests (create, get, list, update, delete, plus the 404 cases) run top to bottom with the expected status codes.

## References

