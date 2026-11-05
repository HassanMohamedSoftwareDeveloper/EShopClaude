namespace EShopClaude.Basket.Api.Data;

public class CustomerBasket
{
    public string BuyerId { get; set; } = "";
    public List<BasketItem> Items { get; set; } = [];
}

// ProductName and UnitPrice are a snapshot taken from Catalog.Api when the item is added.
public class BasketItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
