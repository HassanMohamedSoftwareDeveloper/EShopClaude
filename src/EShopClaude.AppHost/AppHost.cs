var builder = DistributedApplication.CreateBuilder(args);

var catalogApi = builder.AddProject<Projects.EShopClaude_Catalog_Api>("catalogapi")
    .WithHttpHealthCheck("/health");

var orderingApi = builder.AddProject<Projects.EShopClaude_Ordering_Api>("orderingapi")
    .WithHttpHealthCheck("/health");

var basketApi = builder.AddProject<Projects.EShopClaude_Basket_Api>("basketapi")
    .WithHttpHealthCheck("/health")
    .WithReference(catalogApi)
    .WithReference(orderingApi);

builder.AddProject<Projects.EShopClaude_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(catalogApi)
    .WaitFor(catalogApi)
    .WithReference(basketApi)
    .WaitFor(basketApi)
    .WithReference(orderingApi)
    .WaitFor(orderingApi);

builder.Build().Run();
