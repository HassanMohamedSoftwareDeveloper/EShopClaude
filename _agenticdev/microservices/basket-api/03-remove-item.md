# Basket.Api 03 — Remove item

**Status:** Not started

## Goal

Let a buyer drop a line from their basket. This spec also adds the repository's save path (JSON plus a 30-day TTL that resets on every write), which add-item will reuse.

## Depends on

- `basket-api/02-get-basket.md`

## Scope

- [ ] Basket repository: save to `basket:{buyerId}` as JSON with a 30-day TTL, reset on every save.
- [ ] `DELETE /api/basket/{buyerId}/items/{productId}` → drop the line (no-op when absent), save, `200 BasketDto`.
- [ ] `.http` file: remove from an empty basket.
- Why save ships here: it's the first endpoint that writes. A save path without a caller can't be verified.

## Explicit non-goals

- No quantity editing (remove the whole line only).
- No add item (basket-04), no checkout or basket delete (basket-05).

## Definition of Done

- `dotnet build src/EShopClaude.sln`: 0 errors, 0 new warnings.
- `aspire run`: every resource Running/Healthy.
- HTTP `DELETE /api/basket/b1/items/1` on an empty basket → 200 with empty items and total 0.
- Redis: key `basket:b1` exists, holds JSON, and `TTL basket:b1` ≈ 2592000.
- Redis: put a basket with 2 lines via `redis-cli`, DELETE one → 200 with 1 line and the correct total. Redis JSON matches, and the TTL is reset.

## References

