using Microsoft.EntityFrameworkCore;

namespace EShopClaude.Catalog.Api.Data;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(product =>
        {
            product.HasKey(p => p.Id);
            product.Property(p => p.Name).IsRequired().HasMaxLength(100);
            product.Property(p => p.Description).IsRequired().HasMaxLength(500);
            product.Property(p => p.Price).HasPrecision(18, 2);
        });
    }
}
