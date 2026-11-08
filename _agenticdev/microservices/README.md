# Microservices spec backlog

One spec = one sub-feature = one feature branch. The process is in [WOW.md](../WOW.md), and every spec follows [templates/spec-template.md](../templates/spec-template.md). Pick the lowest-numbered spec that isn't Done and whose dependencies are all merged.

| Service | Spec | Branch | Depends on | Status |
|---|---|---|---|---|
| catalog-api | [01 — Persistence](catalog-api/01-persistence.md) | `feature/catalog-01-persistence` | nothing | Done |
| catalog-api | [02 — Seed products](catalog-api/02-seed-products.md) | `feature/catalog-02-seed-products` | catalog-01 | Done |
| catalog-api | [03 — List products](catalog-api/03-list-products.md) | `feature/catalog-03-list-products` | catalog-02 | Done |
| catalog-api | [04 — Get product by id](catalog-api/04-get-product.md) | `feature/catalog-04-get-product` | catalog-03 | Not started |
| ordering-api | [01 — Persistence](ordering-api/01-persistence.md) | `feature/ordering-01-persistence` | nothing | Not started |
| ordering-api | [02 — Create order](ordering-api/02-create-order.md) | `feature/ordering-02-create-order` | ordering-01 | Not started |
| ordering-api | [03 — Get order by id](ordering-api/03-get-order.md) | `feature/ordering-03-get-order` | ordering-02 | Not started |
| ordering-api | [04 — List orders](ordering-api/04-list-orders.md) | `feature/ordering-04-list-orders` | ordering-02 | Not started |
| ordering-api | [05 — Update order](ordering-api/05-update-order.md) | `feature/ordering-05-update-order` | ordering-03 | Not started |
| ordering-api | [06 — Delete order](ordering-api/06-delete-order.md) | `feature/ordering-06-delete-order` | ordering-03 | Not started |
| basket-api | [01 — Redis connection](basket-api/01-redis-connection.md) | `feature/basket-01-redis-connection` | nothing | Not started |
| basket-api | [02 — Get basket](basket-api/02-get-basket.md) | `feature/basket-02-get-basket` | basket-01 | Not started |
| basket-api | [03 — Remove item](basket-api/03-remove-item.md) | `feature/basket-03-remove-item` | basket-02 | Not started |
| basket-api | [04 — Add item](basket-api/04-add-item.md) | `feature/basket-04-add-item` | basket-03, catalog-04 | Not started |
| basket-api | [05 — Checkout](basket-api/05-checkout.md) | `feature/basket-05-checkout` | basket-04, ordering-02, PLAN 4.1 (ServiceDefaults) | Not started |
