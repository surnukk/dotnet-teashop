var builder = DistributedApplication.CreateBuilder(args);

var ordersDb = builder.AddConnectionString("OrdersDb");

builder.AddProject<Projects.Orders_Api>("orders-api")
       .WithReference(ordersDb);

builder.AddProject<Projects.Catalog_Api>("catalog-api");

builder.Build().Run();