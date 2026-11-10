namespace EShopClaude.Ordering.Api;

public record OrderItemRequest(int ProductId, string ProductName, decimal UnitPrice, int Quantity);
