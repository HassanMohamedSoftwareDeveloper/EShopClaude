using EShopClaude.Ordering.Api;
using EShopClaude.Ordering.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<OrderingDbContext>("orderingdb");

builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Create the schema before serving requests. No migrations (PROJECT.md §2).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Ordering API is running.");

var api = app.MapGroup("/api");

// Store an order as sent; Ordering computes Total and trusts its caller (PROJECT.md §3.3).
api.MapPost("/orders", async Task<Created<OrderDto>> (OrderRequest request, OrderingDbContext db, CancellationToken ct) =>
    {
        var order = new Order
        {
            BuyerId = request.BuyerId,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            CreatedAt = DateTimeOffset.UtcNow,
            Total = request.Items.Sum(i => i.UnitPrice * i.Quantity),
            Lines = [.. request.Items.Select(i => new OrderLine
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
            })],
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/orders/{order.Id}", OrderDto.From(order));
    })
    .WithName("CreateOrder");

// Every order with its lines, ordered by Id (PROJECT.md §3.3). CRUD completeness; no storefront page uses it.
api.MapGet("/orders", async Task<Ok<List<OrderDto>>> (OrderingDbContext db, CancellationToken ct) =>
    {
        var orders = await db.Orders
            .AsNoTracking()
            .Include(o => o.Lines.OrderBy(l => l.Id))
            .OrderBy(o => o.Id)
            .ToListAsync(ct);

        return TypedResults.Ok(orders.ConvertAll(OrderDto.From));
    })
    .WithName("ListOrders");

// One order with its lines, or 404 (PROJECT.md §3.3). The storefront order page reads this.
api.MapGet("/orders/{id:int}", async Task<Results<Ok<OrderDto>, NotFound>> (int id, OrderingDbContext db, CancellationToken ct) =>
        await db.Orders
            .AsNoTracking()
            .Include(o => o.Lines.OrderBy(l => l.Id))
            .FirstOrDefaultAsync(o => o.Id == id, ct) is { } order
            ? TypedResults.Ok(OrderDto.From(order))
            : TypedResults.NotFound())
    .WithName("GetOrder");

app.MapDefaultEndpoints();

app.Run();
