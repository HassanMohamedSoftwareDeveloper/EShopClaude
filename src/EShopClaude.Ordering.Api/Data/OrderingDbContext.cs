using Microsoft.EntityFrameworkCore;

namespace EShopClaude.Ordering.Api.Data;

public class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
            order.HasKey(o => o.Id);
            order.Property(o => o.BuyerId).IsRequired().HasMaxLength(100);
            order.Property(o => o.CustomerName).IsRequired().HasMaxLength(100);
            order.Property(o => o.CustomerEmail).IsRequired().HasMaxLength(254);
            order.Property(o => o.Total).HasPrecision(18, 2);

            // One-way: OrderLine has the FK but no back-navigation. Deleting an order deletes its lines.
            order.HasMany(o => o.Lines)
                .WithOne()
                .HasForeignKey(l => l.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderLine>(line =>
        {
            line.HasKey(l => l.Id);
            line.Property(l => l.ProductName).IsRequired().HasMaxLength(100);
            line.Property(l => l.UnitPrice).HasPrecision(18, 2);
        });
    }
}
