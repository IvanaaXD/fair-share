using FairShare.Api.Filters;
using FairShare.API.Middleware;
using FairShare.Infrastructure.Data;
using FairShare.Infrastructure.Seed;
using FairShare.Infrastructure.Storage;
using FairShare.WebAPI.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<RequirePasswordChangeFilter>();
builder.Services.AddScoped<AuditLogFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<RequirePasswordChangeFilter>();
    options.Filters.AddService<AuditLogFilter>();
    options.Filters.Add<ValidationFilter>();
});
builder.Services.AddOpenApi();

// ---------- API documentation, CORS, rate limiting ----------
builder.Services.AddApiDocumentation();
builder.Services.AddFrontendCors(builder.Configuration);
builder.Services.AddApiRateLimiting();

// ---------- running behind a reverse proxy (Nginx in docker-compose) ----------
// Without this the API would see Nginx's address as the client address for every request,
// and the per-IP rate limits would treat all users as one client.
var behindReverseProxy = builder.Configuration.GetValue<bool>("ReverseProxy:Enabled");
if (behindReverseProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // In docker-compose the API port is not published, so only Nginx can reach the API.
        // The proxy is therefore trusted without listing its address, which changes between runs.
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
    });
}

// ---------- uploaded files (profile images, receipts) ----------
builder.Services.AddFileStorage(builder.Configuration, builder.Environment);

// ---------- database ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<FairShareDbContext>((serviceProvider, options) =>
    options.UseNpgsql(connectionString)
        .AddInterceptors(serviceProvider.GetRequiredService<FileCleanupInterceptor>()));

// ---------- JWT, authorization, current user, password hashing ----------
builder.Services.AddIdentityServices(builder.Configuration);

// ---------- repositories + UnitOfWork 
builder.Services.AddRepositoryServices();

// ---------- services, AutoMapper, FluentValidation, e-mail ----------
builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddRecurringExpenses();

var app = builder.Build();
if (behindReverseProxy)
    app.UseForwardedHeaders();

// Catches exceptions from everything that runs after it.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Locally migrations are applied with "dotnet ef database update". In a container there is no
// SDK, so the API can apply them itself when Database:MigrateOnStartup is true.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<FairShareDbContext>().Database.MigrateAsync();
}

await AdminUserSeeder.SeedAsync(app.Services);
if (app.Environment.IsDevelopment())
{
    // /openapi/v1.json - the document itself; must stay reachable without a token.
    app.MapOpenApi().AllowAnonymous();

    // /swagger - interactive UI for the document above.
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "FairShare API v1");
        options.DocumentTitle = "FairShare API";
    });
}

app.UseHttpsRedirection();

app.UseCors(CorsServiceExtensions.FrontendPolicy);

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.Run();