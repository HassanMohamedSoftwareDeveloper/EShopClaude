namespace EShopClaude.Ordering.Api;

public record OrderRequest(string BuyerId, string CustomerName, string CustomerEmail, List<OrderItemRequest> Items);
