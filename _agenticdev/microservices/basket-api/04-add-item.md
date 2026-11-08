# Basket.Api 04 — Add item

**Status:** Not started

## Goal

Let a buyer add a product to their basket. Basket.Api asks Catalog.Api for the product and stores a snapshot of its name and price. Adding a product that is already in the basket increases its quantity. This is the first service-to-service call.

## Depends on

- `basket-api/03-remove-item.md`
- `catalog-api/04-get-product.md`

## Scope

- [ ] Typed `CatalogClient` (`https+http://catalogapi`) with its own `ProductDto` record. `GetProduct(id)` returns null on 404.
- [ ] `POST /api/basket/{buyerId}/items` `{ productId, quantity }`:
  - `quantity < 1` → 400.
  - Product not found → 400.
  - Line already exists → `quantity += n`, keeping the original snapshot.
  - Otherwise → append a line with Catalog's name and price.
  - Save with TTL → `200 BasketDto`.
- [ ] `.http` file: add a valid product, add the same product again, add a missing product, add qty 0.
- Why the client and the endpoint share one spec: only this endpoint calls the client, so the client can't be verified alone.

## Explicit non-goals

- No price re-check against Catalog on later adds or at checkout.
- No custom retries or fallbacks, no shared contracts.
- No checkout (basket-05).

## Definition of Done

- `dotnet build src/EShopClaude.sln`: 0 errors, 0 new warnings.
- `aspire run`: every resource Running/Healthy.
- HTTP: add id 1 qty 2 → line qty 2, total = price × 2. Add id 1 qty 1 → qty 3. Add id 999 → 400. Add qty 0 → 400. Remove id 1 → empty.
- Redis: `basket:{buyerId}` JSON holds Catalog's name and price as the snapshot.
- Traces: `basketapi POST …/items` span with a child `GET catalogapi /api/products/{id}`.

## References

