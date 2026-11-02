var builder = DistributedApplication.CreateBuilder(args);

var ordersDb = builder.AddConnectionString("OrdersDb");

builder.AddProject<Projects.Orders_Api>("orders-api")
       .WithReference(ordersDb);

builder.AddProject<Projects.Catalog_Api>("catalog-api");

builder.AddProject<Projects.Reviews_WebApi>("reviews-api");

builder.AddProject<Projects.Platform_ApiGateway>("platform-apigateway");

builder.AddProject<Projects.Platform_Aggregator>("platform-aggregator");

builder.Build().Run();