using Microsoft.EntityFrameworkCore;

namespace EShopClaude.Catalog.Api.Data;

public static class CatalogSeed
{
    // Inserts the fixed catalog only when the table is empty, so restarts never duplicate rows.
    public static async Task SeedAsync(CatalogDbContext db)
    {
        if (await db.Products.AnyAsync())
        {
            return;
        }

        db.Products.AddRange(
            new Product { Name = "Classic T-Shirt", Description = "Soft cotton crew-neck tee.", Price = 19.99m },
            new Product { Name = "Hoodie", Description = "Warm fleece pullover hoodie.", Price = 49.99m },
            new Product { Name = "Baseball Cap", Description = "Adjustable cotton cap.", Price = 14.50m },
            new Product { Name = "Coffee Mug", Description = "350 ml ceramic mug.", Price = 9.99m },
            new Product { Name = "Water Bottle", Description = "750 ml stainless steel bottle.", Price = 24.00m },
            new Product { Name = "Backpack", Description = "20 L everyday backpack.", Price = 59.00m });

        await db.SaveChangesAsync();
    }
}
