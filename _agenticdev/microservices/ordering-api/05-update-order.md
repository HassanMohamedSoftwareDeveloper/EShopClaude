# Ordering.Api 05 — Update order

**Status:** Not started

## Goal

Replace an existing order's customer data and lines, and recompute its total, for CRUD completeness. No storefront page uses it.

## Depends on

- `ordering-api/03-get-order.md`

## Scope

- [ ] `PUT /api/orders/{id}` with `OrderRequest` → replace customer name, email and lines, recompute `Total` → `200 OrderDto`, or `404`.
- [ ] `.http` file: update existing, update missing.

## Explicit non-goals

- No partial update (PATCH) and no line-level endpoints.
- `CreatedAt` and `Id` are never changed.
- No status field.

## Definition of Done

- `dotnet build src/EShopClaude.sln`: 0 errors, 0 new warnings.
- `aspire run`: every resource Running/Healthy.
- HTTP: POST an order with 2 lines, then PUT with 1 different line → 200, new lines and recomputed total. GET by id returns the same data, with the original `createdAt`.
- HTTP `PUT /api/orders/999` → 404.
- pgAdmin: only the new line exists for that order, and the old lines are removed.

## References

