# Basket.Api 05 — Checkout

**Status:** Not started

## Goal

Turn a basket into an order. Basket.Api reads the basket, sends a create-order request to Ordering.Api, and deletes the basket only after the order is created. If Ordering fails, the basket is left untouched. This completes the backend journey.

## Depends on

- `basket-api/04-add-item.md`
- `ordering-api/02-create-order.md`
- PLAN step 4.1 (ServiceDefaults `Retry.DisableForUnsafeHttpMethods()`). It isn't a microservice spec, but it must be merged first, because the failure check below expects exactly one POST attempt.

## Scope

- [ ] Typed `OrderingClient` (`https+http://orderingapi`) with its own `OrderRequest` and response records.
- [ ] Basket repository: delete `basket:{buyerId}`.
- [ ] `POST /api/basket/{buyerId}/checkout` `{ customerName, customerEmail }`:
  - Missing or empty basket → 400.
  - `POST orderingapi /api/orders` with items copied from the snapshot.
  - Non-success → exception → 500, basket untouched.
  - On 201 → delete the basket → `201 { orderId }`.
- [ ] `.http` file: checkout an empty basket; happy path (add item, then checkout).
- Why the client, repository delete and endpoint share one spec: only checkout uses them, so none can be verified without it.

## Explicit non-goals

- No retries, compensation, saga or outbox.
- No price re-validation against Catalog.
- No payment, tax or shipping.

## Definition of Done

- `dotnet build src/EShopClaude.sln`: 0 errors, 0 new warnings.
- `aspire run`: every resource Running/Healthy.
- HTTP: checkout an empty basket → 400.
- HTTP: add items, then checkout → 201 `{ orderId }`. `GET orderingapi /api/orders/{orderId}` → lines and prices match the basket snapshot, and the total is correct.
- After that checkout: `GET basket` → empty, and the Redis key `basket:{buyerId}` is gone.
- Failure path: stop `orderingapi` in the dashboard, then checkout → 500. The basket is still in Redis, and the trace shows exactly one POST attempt. Restart `orderingapi`.
- Traces: one trace `basketapi checkout` → `POST orderingapi /api/orders` → EF insert.

## References

