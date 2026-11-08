using EShopClaude.Catalog.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Create the schema and seed it before serving requests. No migrations (PROJECT.md §2).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.EnsureCreatedAsync();
    await CatalogSeed.SeedAsync(db);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Catalog API is running.");

// Catalog endpoints are added to this group in Phase 3.
var api = app.MapGroup("/api");

app.MapDefaultEndpoints();

app.Run();
