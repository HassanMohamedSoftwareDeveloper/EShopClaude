namespace EShopClaude.Ordering.Api;

public record OrderLineDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity);
