using Aspire.Hosting;
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
IResourceBuilder<IResourceWithConnectionString> identityServerDb;
IResourceBuilder<IResourceWithConnectionString> redis;
IResourceBuilder<IResourceWithConnectionString> rabbit;
IResourceBuilder<KeycloakResource>? keycloak = null;

if (useContainers)
{
    // пароль SQL Server лежить у секретах AppHost, а не у відкритому коді
    var sqlPassword = builder.AddParameter("sql-password", secret: true);

    var sql = builder.AddSqlServer("sql", sqlPassword)
                     .WithDataVolume();
    ordersDb = sql.AddDatabase("OrdersDb", "EastLeaf_OrderDB");
    catalogDb = sql.AddDatabase("CatalogDb");
    identityDb = sql.AddDatabase("IdentityDb");

    var identityServerSql = builder.AddSqlServer("identityserver-sql", sqlPassword)
                                   .WithDataVolume();
    identityServerDb = identityServerSql.AddDatabase("IdentityServerDb");

    var mongo = builder.AddMongoDB("mongo")
                       .WithDataVolume();
    reviewsDb = mongo.AddDatabase("ReviewsDb", "EastLeaf_ReviewsDB");

    redis = builder.AddRedis("redis")
                   .WithDataVolume();

    var rabbitPassword = builder.AddParameter("rabbit-password", secret: true);
    rabbit = builder.AddRabbitMQ("messaging", password: rabbitPassword)
                    .WithDataVolume()
                    .WithManagementPlugin();

    // ---------- Ч.3: Keycloak ----------
    var keycloakAdmin = builder.AddParameter("keycloak-admin");
    var keycloakAdminPassword = builder.AddParameter("keycloak-admin-password", secret: true);

    keycloak = builder.AddKeycloak("keycloak", 8080, keycloakAdmin, keycloakAdminPassword)
                      .WithDataVolume();  
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
    identityServerDb = builder.AddConnectionString("IdentityServerDb");
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

var jwtSecret = builder.AddParameter("jwt-secret", secret: true);
const string jwtIssuer = "https://auth.teashop.local";
const string jwtAudience = "teashop-api";

IResourceBuilder<ProjectResource> WithJwt(IResourceBuilder<ProjectResource> project)
    => project
        .WithEnvironment("Jwt__Issuer", jwtIssuer)
        .WithEnvironment("Jwt__Audience", jwtAudience)
        .WithEnvironment("Jwt__SecretKey", jwtSecret);

// ---------- Authority IdentityServer для сервісів ----------
const string identityServerUrl = "http://localhost:5200";

IResourceBuilder<ProjectResource> WithIdentityServer(IResourceBuilder<ProjectResource> project)
    => project.WithEnvironment("IdentityServer__Authority", identityServerUrl);

// ---------- 2.2 Мікросервіси ----------
var orders = WithRabbit(WithRedis(WithDb(builder.AddProject<Projects.Orders_Api>("orders-api", launchProfileName: "https"), ordersDb))).WithHttpHealthCheck("/health", endpointName: "http");
var catalog = WithRabbit(WithRedis(WithDb(builder.AddProject<Projects.Catalog_Api>("catalog-api", launchProfileName: "https"), catalogDb))).WithHttpHealthCheck("/health", endpointName: "http");
var reviews = WithRabbit(WithRedis(WithDb(builder.AddProject<Projects.Reviews_WebApi>("reviews-api", launchProfileName: "https"), reviewsDb))).WithHttpHealthCheck("/health", endpointName: "http");
var auth = WithDb(builder.AddProject<Projects.Auth_Api>("auth-api", launchProfileName: "https"), identityDb).WithHttpHealthCheck("/health", endpointName: "http");
var identityServer = WithDb(builder.AddProject<Projects.Platform_IdentityServer>("identityserver"), identityServerDb).WithHttpEndpoint(port: 5200, name: "public").WithHttpHealthCheck("/health", endpointName: "public");
var payments = builder.AddProject<Projects.Payments_Api>("payments-api", launchProfileName: "https").WithHttpHealthCheck("/health", endpointName: "http");

// ---------- 2.3 Aggregator та Gateway ----------
var aggregator = WithRedis(builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(orders).WithReference(catalog).WithReference(reviews)
    .WaitFor(orders).WaitFor(catalog).WaitFor(reviews));

var gateway = builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithReference(orders).WithReference(catalog).WithReference(reviews)
    .WithReference(aggregator)
    .WithReference(auth)
    .WithReference(payments)
    .WaitFor(aggregator).WaitFor(auth);

WithJwt(auth); WithJwt(orders); WithJwt(catalog); WithJwt(reviews); WithJwt(aggregator); WithJwt(gateway);

WithIdentityServer(orders);
WithIdentityServer(catalog);
WithIdentityServer(payments);
WithIdentityServer(aggregator);
WithIdentityServer(gateway);
WithIdentityServer(reviews);

aggregator.WaitFor(identityServer);
catalog.WaitFor(identityServer);
orders.WaitFor(identityServer);
payments.WaitFor(identityServer);
gateway.WaitFor(identityServer);
reviews.WaitFor(identityServer);

// Keycloak: посилання для сервісів, що перевірятимуть його токени
if (keycloak is not null)
{
    foreach (var svc in new[] { orders, catalog, payments })
        svc.WithReference(keycloak).WaitFor(keycloak);
}

builder.Build().Run();