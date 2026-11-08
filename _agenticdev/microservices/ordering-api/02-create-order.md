# Ordering.Api 02 — Create order

**Status:** Not started

## Goal

Let a caller store an order. `POST /api/orders` takes the buyer, customer data and lines as sent, stamps `CreatedAt`, computes `Total` from the lines, and returns the new order. Basket.Api's checkout will use this.

## Depends on

- `ordering-api/01-persistence.md`

## Scope

- [ ] `OrderRequest { buyerId, customerName, customerEmail, items[{ productId, productName, unitPrice, quantity }] }` record.
- [ ] `OrderDto` (id, buyerId, customerName, customerEmail, createdAt, total, lines) record.
- [ ] `POST /api/orders` → `201 OrderDto` with `Location: /api/orders/{id}`, `CreatedAt` = UTC now, `Total` = Σ unitPrice × quantity.
- [ ] `.http` file: create request.

## Explicit non-goals

- No GET, PUT or DELETE (ordering-03 to ordering-06).
- No Catalog call, no price check, no validation beyond model binding (Ordering trusts its caller).
- No `Total` taken from input, no status field.

## Definition of Done

- `dotnet build src/EShopClaude.sln`: 0 errors, 0 new warnings.
- `aspire run`: every resource Running/Healthy.
- HTTP `POST /api/orders` with 2 lines → 201, `Location: /api/orders/{id}`, body with id, UTC `createdAt` and the correct `total`.
- pgAdmin: one `Orders` row and 2 `OrderLines` rows that match the request.
- `/openapi/v1.json` lists `POST /api/orders`.
- Traces: request span with a child EF insert, no errors.

## References

