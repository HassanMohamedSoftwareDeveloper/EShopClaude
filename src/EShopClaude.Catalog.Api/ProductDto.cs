namespace EShopClaude.Catalog.Api;

// Catalog.Api's own contract. Basket.Api and Web declare their own copies (PROJECT.md §2).
public record ProductDto(int Id, string Name, string Description, decimal Price);
