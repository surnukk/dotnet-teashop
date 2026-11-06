using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// true  -> SQL Server і MongoDB піднімаються як Docker-контейнери
// false -> підключення до локальних баз (запуск без Docker)
// за замовчуванням контейнери; локальні бази лише при UseContainers=false
var useContainers = !string.Equals(
    builder.Configuration["UseContainers"], "false", StringComparison.OrdinalIgnoreCase);

// ---------- 2.1 Бази даних ----------
IResourceBuilder<IResourceWithConnectionString> ordersDb;
IResourceBuilder<IResourceWithConnectionString> catalogDb;
IResourceBuilder<IResourceWithConnectionString> reviewsDb;
IResourceBuilder<IResourceWithConnectionString> redis;

if (useContainers)
{
    // пароль SQL Server лежить у секретах AppHost, а не у відкритому коді
    var sqlPassword = builder.AddParameter("sql-password", secret: true);

    var sql = builder.AddSqlServer("sql", sqlPassword)
                     .WithDataVolume();
    ordersDb = sql.AddDatabase("OrdersDb", "EastLeaf_OrderDB");
    catalogDb = sql.AddDatabase("CatalogDb"); 

    var mongo = builder.AddMongoDB("mongo")
                       .WithDataVolume();
    reviewsDb = mongo.AddDatabase("ReviewsDb", "EastLeaf_ReviewsDB");

    redis = builder.AddRedis("redis")
                   .WithDataVolume();
}
else
{
    // локальні бази: рядки беруться з ConnectionStrings:* в конфігурації AppHost
    ordersDb = builder.AddConnectionString("OrdersDb");
    catalogDb = builder.AddConnectionString("CatalogDb");
    reviewsDb = builder.AddConnectionString("ReviewsDb");
    redis = builder.AddConnectionString("redis");
}

IResourceBuilder<ProjectResource> WithDb(
    IResourceBuilder<ProjectResource> project,
    IResourceBuilder<IResourceWithConnectionString> db)
{
    project.WithReference(db);
    if (useContainers) project.WaitFor(db);
    return project;
}

IResourceBuilder<ProjectResource> WithRedis(IResourceBuilder<ProjectResource> project)
{
    project.WithReference(redis);
    if (useContainers) project.WaitFor(redis);
    return project;
}

// ---------- 2.2 Мікросервіси ----------
var orders = WithRedis(WithDb(builder.AddProject<Projects.Orders_Api>("orders-api"), ordersDb));
var catalog = WithRedis(WithDb(builder.AddProject<Projects.Catalog_Api>("catalog-api"), catalogDb));   // без змін
var reviews = WithRedis(WithDb(builder.AddProject<Projects.Reviews_WebApi>("reviews-api"), reviewsDb));

// ---------- 2.3 Aggregator та Gateway ----------
var aggregator = WithRedis(builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(orders).WithReference(catalog).WithReference(reviews)
    .WaitFor(orders).WaitFor(catalog).WaitFor(reviews));

builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithReference(orders).WithReference(catalog).WithReference(reviews)
    .WithReference(aggregator)
    .WaitFor(aggregator);

builder.Build().Run();