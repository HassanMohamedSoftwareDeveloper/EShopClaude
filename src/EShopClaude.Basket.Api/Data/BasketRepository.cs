using System.Text.Json;
using StackExchange.Redis;

namespace EShopClaude.Basket.Api.Data;

// Stores each basket as JSON under "basket:{buyerId}". Every save resets the 30-day TTL.
public class BasketRepository(IConnectionMultiplexer redis)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDatabase _db = redis.GetDatabase();

    private static RedisKey Key(string buyerId) => $"basket:{buyerId}";

    public async Task<CustomerBasket?> GetAsync(string buyerId)
    {
        var json = await _db.StringGetAsync(Key(buyerId));
        return json.IsNullOrEmpty ? null : JsonSerializer.Deserialize<CustomerBasket>(json.ToString(), JsonOptions);
    }

    public async Task SaveAsync(CustomerBasket basket)
    {
        var json = JsonSerializer.Serialize(basket, JsonOptions);
        await _db.StringSetAsync(Key(basket.BuyerId), json, Ttl);
    }

    public Task<bool> DeleteAsync(string buyerId) => _db.KeyDeleteAsync(Key(buyerId));
}
