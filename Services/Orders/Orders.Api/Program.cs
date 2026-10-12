using Microsoft.Data.SqlClient;
using Orders.Bll.Services;
using Orders.Dal.Interfaces;
using Orders.Dal;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("OrdersDb")
    ?? throw new InvalidOperationException("Connection string 'OrdersDb' is missing");

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllers();

builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.Logger.LogInformation("Database: {Database}",
    new SqlConnectionStringBuilder(connectionString).InitialCatalog);

app.MapControllers();
app.Run();