# Basket.Api 02 — Get basket

**Status:** Not started

## Goal

Read a buyer's basket from Redis and return it with a computed total. A missing basket comes back empty, not 404. The storefront's cart count and cart page use this.

## Depends on

- `basket-api/01-redis-connection.md`

## Scope

- [ ] Models `CustomerBasket { buyerId, items }` and `BasketItem { productId, productName, unitPrice, quantity }`.
- [ ] `BasketDto { buyerId, items, total }`, with `total = Σ unitPrice × quantity` computed on read.
- [ ] Basket repository: get by key `basket:{buyerId}` (JSON), returns null when missing.
- [ ] `GET /api/basket/{buyerId}` → `200 BasketDto` (empty items when the key is missing).
- [ ] `.http` file: get an empty basket.

## Explicit non-goals

- No save or TTL (basket-03), no delete (basket-05).
- No add, remove or checkout endpoints.
- No stored total.

## Definition of Done

- `dotnet build src/EShopClaude.sln`: 0 errors, 0 new warnings.
- `aspire run`: every resource Running/Healthy.
- HTTP `GET /api/basket/unknown-buyer` → 200 `{ buyerId: "unknown-buyer", items: [], total: 0 }`.
- Redis: put a basket JSON on `basket:test` with `redis-cli SET`, then GET returns its items with the correct computed total.
- `/openapi/v1.json` lists the route.

## References

