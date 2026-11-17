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
IResourceBuilder<IResourceWithConnectionString> identityDb;
IResourceBuilder<IResourceWithConnectionString> redis;
IResourceBuilder<IResourceWithConnectionString> rabbit;

if (useContainers)
{
    // пароль SQL Server лежить у секретах AppHost, а не у відкритому коді
    var sqlPassword = builder.AddParameter("sql-password", secret: true);

    var sql = builder.AddSqlServer("sql", sqlPassword)
                     .WithDataVolume();
    ordersDb = sql.AddDatabase("OrdersDb", "EastLeaf_OrderDB");
    catalogDb = sql.AddDatabase("CatalogDb");
    identityDb = sql.AddDatabase("IdentityDb");

    var mongo = builder.AddMongoDB("mongo")
                       .WithDataVolume();
    reviewsDb = mongo.AddDatabase("ReviewsDb", "EastLeaf_ReviewsDB");

    redis = builder.AddRedis("redis")
                   .WithDataVolume();

    var rabbitPassword = builder.AddParameter("rabbit-password", secret: true);
    rabbit = builder.AddRabbitMQ("messaging", password: rabbitPassword)
                    .WithDataVolume()
                    .WithManagementPlugin();
}
else
{
    // локальні бази: рядки беруться з ConnectionStrings:* в конфігурації AppHost
    ordersDb = builder.AddConnectionString("OrdersDb");
    catalogDb = builder.AddConnectionString("CatalogDb");
    reviewsDb = builder.AddConnectionString("ReviewsDb");
    redis = builder.AddConnectionString("redis");
    rabbit = builder.AddConnectionString("messaging");
    identityDb = builder.AddConnectionString("IdentityDb");
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

IResourceBuilder<ProjectResource> WithRabbit(IResourceBuilder<ProjectResource> project)
{
    project.WithReference(rabbit);
    if (useContainers) project.WaitFor(rabbit);
    return project;
}

// ---------- 2.2 Мікросервіси ----------
var orders = WithRabbit(WithRedis(WithDb(builder.AddProject<Projects.Orders_Api>("orders-api", launchProfileName: "https"), ordersDb))).WithHttpHealthCheck("/health", endpointName: "http");
var catalog = WithRabbit(WithRedis(WithDb(builder.AddProject<Projects.Catalog_Api>("catalog-api", launchProfileName: "https"), catalogDb))).WithHttpHealthCheck("/health", endpointName: "http");
var reviews = WithRabbit(WithRedis(WithDb(builder.AddProject<Projects.Reviews_WebApi>("reviews-api", launchProfileName: "https"), reviewsDb))).WithHttpHealthCheck("/health", endpointName: "http");
var auth = WithDb(builder.AddProject<Projects.Auth_Api>("auth-api", launchProfileName: "https"), identityDb).WithHttpHealthCheck("/health", endpointName: "http");

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