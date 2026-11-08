# Basket.Api 01 — Redis connection

**Status:** Not started

## Goal

Connect Basket.Api to Redis through Aspire, so the service waits for Redis, reports Redis health, and can resolve `IConnectionMultiplexer`. Every basket endpoint builds on this.

## Depends on

Nothing. PLAN 1.x and 2.1 (skeleton, infra resources) are already merged.

## Scope

- [ ] AppHost: `basketapi` `.WithReference(redis).WaitFor(redis)`.
- [ ] `AddRedisClient("redis")` in `Program.cs`.
- Why both share one spec: `AddRedisClient` fails without the `redis` connection string, so neither part can be checked alone.

## Explicit non-goals

- No models, repository or endpoints (basket-02 onward).
- No data volume, no RedisInsight.

## Definition of Done

- `dotnet build src/EShopClaude.sln`: 0 errors, 0 new warnings.
- `aspire run`: every resource Running/Healthy, and `basketapi` starts only after `redis` is healthy.
- Dashboard: `basketapi` env vars contain `ConnectionStrings__redis`.
- HTTP `GET /health` on `basketapi` → 200 (the Redis health check is included).
- Stop `redis` from the dashboard → `basketapi` turns Unhealthy. Start it again → Healthy.

## References

