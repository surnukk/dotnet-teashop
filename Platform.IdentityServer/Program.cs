using Duende.IdentityServer.EntityFramework.DbContexts;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.IdentityServer.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("IdentityServerDb");
var migrationsAssembly = typeof(Program).Assembly.GetName().Name;

builder.Services.AddRazorPages();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;
    options.KeyManagement.Enabled = false;

    options.Authentication.CookieSameSiteMode = SameSiteMode.Lax;
    options.Authentication.CheckSessionCookieSameSiteMode = SameSiteMode.Lax;  
})
.AddAspNetIdentity<ApplicationUser>()
.AddConfigurationStore(options =>
    options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
        sql => sql.MigrationsAssembly(migrationsAssembly)))
.AddOperationalStore(options =>
{
    options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
        sql => sql.MigrationsAssembly(migrationsAssembly));
    options.EnableTokenCleanup = true;
    options.TokenCleanupInterval = 3600;
}).AddDeveloperSigningCredential(persistKey: false);

builder.Services.ConfigureApplicationCookie(options => options.Cookie.SameSite = SameSiteMode.Lax);
builder.Services.ConfigureExternalCookie(options => options.Cookie.SameSite = SameSiteMode.Lax);

var app = builder.Build();

// Міграції трьох контекстів + початкові дані (clients, scopes, тестові користувачі)
using (var scope = app.Services.CreateScope())
{
    await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
}

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseStaticFiles();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorPages().RequireAuthorization();

app.Run();