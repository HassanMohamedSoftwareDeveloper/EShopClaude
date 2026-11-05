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
            order.Property(o => o.BuyerId).HasMaxLength(100);
            order.Property(o => o.CustomerName).HasMaxLength(200);
            order.Property(o => o.CustomerEmail).HasMaxLength(200);
            order.Property(o => o.Total).HasPrecision(18, 2);

            order.HasMany(o => o.Lines)
                .WithOne()
                .HasForeignKey(l => l.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderLine>(line =>
        {
            line.Property(l => l.ProductName).HasMaxLength(100);
            line.Property(l => l.UnitPrice).HasPrecision(18, 2);
        });
    }
}
