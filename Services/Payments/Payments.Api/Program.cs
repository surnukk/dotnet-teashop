using Microsoft.AspNetCore.Authentication.JwtBearer;
using Platform.ServiceDefaults.Security;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var identityServerAuthority = builder.Configuration["IdentityServer:Authority"] ?? "http://localhost:5200";
var keycloakIssuer = builder.Configuration["Keycloak:Issuer"] ?? "http://localhost:8080/realms/DevRealmDrivers";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = PlatformAuthentication.SmartScheme;
        options.DefaultChallengeScheme = PlatformAuthentication.SmartScheme;
    })
    .AddPolicyScheme(PlatformAuthentication.SmartScheme, "IdentityServer or Keycloak", options =>
    {
        options.ForwardDefaultSelector = context =>
            PlatformAuthentication.SelectScheme(context, identityServerAuthority, keycloakIssuer)
                == PlatformAuthentication.KeycloakScheme
                ? PlatformAuthentication.KeycloakScheme
                : PlatformAuthentication.IdentityServerScheme;
    })
    .AddJwtBearer(PlatformAuthentication.IdentityServerScheme, options =>
    {
        options.Authority = identityServerAuthority;
        options.TokenValidationParameters.ValidateAudience = false;
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;
        options.RefreshInterval = TimeSpan.FromSeconds(10);
        options.AutomaticRefreshInterval = TimeSpan.FromMinutes(5);
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "role";
    })
    .AddKeycloakJwtBearer(
        serviceName: "keycloak",
        realm: "DevRealmDrivers",
        authenticationScheme: PlatformAuthentication.KeycloakScheme,
        configureOptions: options =>
        {
            options.Audience = "payments-api";
            options.MapInboundClaims = false;
            options.RequireHttpsMetadata = false;
        });

builder.Services.AddAuthorization(options =>
{
    options.AddScopePolicy("payments.read", "payments.read");
    options.AddScopePolicy("payments.write", "payments.write");

    options.AddPermissionPolicy("payments:read", legacyPolicy: "payments.read");
    options.AddPermissionPolicy("payments:process", legacyPolicy: "payments.write");
    options.AddPermissionPolicy("payments:refund", legacyPolicy: "payments.write");
});
builder.Services.AddPermissionAuthorization();

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