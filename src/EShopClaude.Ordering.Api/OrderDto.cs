using EShopClaude.Ordering.Api.Data;

namespace EShopClaude.Ordering.Api;

public record OrderDto(int Id, string BuyerId, string CustomerName, string CustomerEmail,
    DateTimeOffset CreatedAt, decimal Total, List<OrderLineDto> Lines)
{
    // The only Order -> OrderDto mapping; reused by get, list and update.
    public static OrderDto From(Order order) => new(
        order.Id, order.BuyerId, order.CustomerName, order.CustomerEmail,
        order.CreatedAt, order.Total,
        [.. order.Lines.Select(l => new OrderLineDto(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity))]);
}
