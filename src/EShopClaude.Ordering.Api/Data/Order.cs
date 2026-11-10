namespace EShopClaude.Ordering.Api.Data;

public class Order
{
    public int Id { get; set; }

    public string BuyerId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public decimal Total { get; set; }

    public List<OrderLine> Lines { get; set; } = [];
}
