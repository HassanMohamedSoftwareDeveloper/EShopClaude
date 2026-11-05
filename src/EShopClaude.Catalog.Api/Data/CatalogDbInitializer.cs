using Microsoft.EntityFrameworkCore;

namespace EShopClaude.Catalog.Api.Data;

public static class CatalogDbInitializer
{
    // Creates the schema and seeds products once. Safe to run on every startup.
    public static async Task InitializeCatalogDbAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        await db.Database.EnsureCreatedAsync();

        if (await db.Products.AnyAsync())
        {
            return;
        }

        // Added one by one so ids are assigned in this order (1..6).
        foreach (var product in SeedProducts())
        {
            db.Products.Add(product);
            await db.SaveChangesAsync();
        }
    }

    private static IEnumerable<Product> SeedProducts() =>
    [
        new() { Name = "Classic T-Shirt", Description = "Soft cotton t-shirt in navy blue.", Price = 19.99m },
        new() { Name = "Hoodie", Description = "Warm fleece hoodie with front pocket.", Price = 49.90m },
        new() { Name = "Coffee Mug", Description = "Ceramic mug, 350 ml, dishwasher safe.", Price = 12.50m },
        new() { Name = "Notebook", Description = "A5 dotted notebook, 120 pages.", Price = 8.75m },
        new() { Name = "Water Bottle", Description = "Insulated steel bottle, 750 ml.", Price = 24.00m },
        new() { Name = "Backpack", Description = "Everyday backpack with laptop sleeve.", Price = 79.00m },
    ];
}
