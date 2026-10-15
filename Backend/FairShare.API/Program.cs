using System.Text.Json.Serialization;
using FairShare.Api.Filters;
using FairShare.API.Middleware;
using FairShare.Infrastructure.Caching;
using FairShare.Infrastructure.Data;
using FairShare.Infrastructure.Seed;
using FairShare.Infrastructure.Storage;
using FairShare.WebAPI.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- controllers + global filters ----------
// Order: account state first, then audit, then validation right before the action.
builder.Services.AddScoped<RequirePasswordChangeFilter>();
builder.Services.AddScoped<AuditLogFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<RequirePasswordChangeFilter>();
    options.Filters.AddService<AuditLogFilter>();
    options.Filters.Add<ValidationFilter>();
})
// Enums as text ("Admin", "Monthly") instead of numbers.
.AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---------- API documentation, CORS, rate limiting ----------
builder.Services.AddApiDocumentation();
builder.Services.AddFrontendCors(builder.Configuration);
builder.Services.AddApiRateLimiting();

// ---------- behind a reverse proxy (Nginx in docker-compose) ----------
var behindReverseProxy = builder.Configuration.GetValue<bool>("ReverseProxy:Enabled");
if (behindReverseProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
    });
}

// ---------- uploaded files ----------
builder.Services.AddFileStorage(builder.Configuration, builder.Environment);

// ---------- database ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<FairShareDbContext>((serviceProvider, options) =>
    options.UseNpgsql(connectionString)
        .AddInterceptors(
            serviceProvider.GetRequiredService<FileCleanupInterceptor>(),
            serviceProvider.GetRequiredService<CacheInvalidationInterceptor>()));

// ---------- identity, repositories, services ----------
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddRepositoryServices();
builder.Services.AddApplicationServices(builder.Configuration);

// ---------- features ----------
builder.Services.AddPayments();
builder.Services.AddExchangeRates();
builder.Services.AddReports();
builder.Services.AddRealtimeNotifications();   // SignalR + token for the hub
builder.Services.AddRecurringExpenses();

// Must stay AFTER AddApplicationServices: replaces some services with cached versions.
builder.Services.AddCaching();

var app = builder.Build();

if (behindReverseProxy)
    app.UseForwardedHeaders();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// In a container (no SDK) the API applies migrations itself.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<FairShareDbContext>().Database.MigrateAsync();
}

await AdminUserSeeder.SeedAsync(app.Services);
if (app.Configuration.GetValue<bool>("DemoData:Enabled"))
    await DemoDataSeeder.SeedAsync(app.Services);


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "FairShare API v1");
        options.DocumentTitle = "FairShare API";
    });
}

app.UseHttpsRedirection();

// Order: CORS -> authentication -> rate limiting -> authorization.
app.UseCors(CorsServiceExtensions.FrontendPolicy);
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapRealtimeNotifications();               // /api/hubs/notifications

app.Run();