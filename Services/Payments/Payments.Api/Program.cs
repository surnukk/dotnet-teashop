using Microsoft.AspNetCore.Authentication.JwtBearer;
using Platform.ServiceDefaults.Security;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var identityServerAuthority = builder.Configuration["IdentityServer:Authority"] ?? "http://localhost:5200";

// Payments приймає лише токени IdentityServer
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = identityServerAuthority;
        options.TokenValidationParameters.ValidateAudience = false;
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;
        options.RefreshInterval = TimeSpan.FromSeconds(10);          // NEW: дозволяє оновити метадані вже через 10 с після збою
        options.AutomaticRefreshInterval = TimeSpan.FromMinutes(5);
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "role";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddScopePolicy("payments.read", "payments.read");
    options.AddScopePolicy("payments.write", "payments.write");
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();