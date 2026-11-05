using EShopClaude.Basket.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisClient("redis");

builder.Services.AddSingleton<BasketRepository>();

builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Basket API is running.");

// Basket endpoints are added to this group in Phase 3.
var api = app.MapGroup("/api");

app.MapDefaultEndpoints();

app.Run();
