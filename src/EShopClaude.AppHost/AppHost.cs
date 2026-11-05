var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")     // no data volume: ephemeral
    .WithPgAdmin();                                 // browse catalogdb + orderingdb
var catalogDb = postgres.AddDatabase("catalogdb");
var orderingDb = postgres.AddDatabase("orderingdb");
var redis = builder.AddRedis("redis");              // no data volume: ephemeral

var catalogApi = builder.AddProject<Projects.EShopClaude_Catalog_Api>("catalogapi")
    .WithHttpHealthCheck("/health")
    .WithReference(catalogDb)
    .WaitFor(catalogDb);

var orderingApi = builder.AddProject<Projects.EShopClaude_Ordering_Api>("orderingapi")
    .WithHttpHealthCheck("/health")
    .WithReference(orderingDb)
    .WaitFor(orderingDb);

var basketApi = builder.AddProject<Projects.EShopClaude_Basket_Api>("basketapi")
    .WithHttpHealthCheck("/health")
    .WithReference(redis)
    .WaitFor(redis)
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
