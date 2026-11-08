# Ordering.Api 01 — Persistence

**Status:** Not started

## Goal

Give Ordering.Api its own schema in `orderingdb`, with orders and their lines. After this spec the service connects to Postgres through Aspire, waits for the database, and creates the `Orders` and `OrderLines` tables on startup. Every order endpoint builds on this.

## Depends on

Nothing. PLAN 1.x and 2.1 (skeleton, infra resources) are already merged.

## Scope

- [ ] AppHost: `orderingapi` `.WithReference(orderingDb).WaitFor(orderingDb)`.
- [ ] `Order` entity: Id (int identity), BuyerId, CustomerName, CustomerEmail, CreatedAt (`DateTimeOffset` UTC), Total (`decimal`), Lines.
- [ ] `OrderLine` entity: Id, OrderId FK, ProductId, ProductName, UnitPrice (`decimal`), Quantity.
- [ ] `OrderingDbContext` with cascade delete from Order to Lines.
- [ ] `AddNpgsqlDbContext<OrderingDbContext>("orderingdb")`.
- [ ] Startup: `Database.EnsureCreated()`.
- Why AppHost wiring and DbContext registration share one spec: `AddNpgsqlDbContext` throws when the `orderingdb` connection string is missing, so neither part can be checked alone.

## Explicit non-goals

- No endpoints or DTOs (ordering-02 onward).
- No seed data, no status field.
- No EF migrations, no data volume.

## Definition of Done

- `dotnet build src/EShopClaude.sln`: 0 errors, 0 new warnings.
- `aspire run`: every resource Running/Healthy, and `orderingapi` starts only after `orderingdb` is healthy.
- Dashboard: `orderingapi` env vars contain `ConnectionStrings__orderingdb`.
- pgAdmin: `orderingdb` has `Orders` and `OrderLines` tables with the columns above, an FK `OrderLines.OrderId → Orders.Id` with cascade delete, and 0 rows.

## References

