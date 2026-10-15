using FairShare.API.Hubs;
using FairShare.API.Middleware;
using FairShare.WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ---------- services, layer by layer (Onion architecture) ----------
builder.Services
    .AddApplicationLayer()                                                     // business logic
    .AddInfrastructureLayer(builder.Configuration, builder.Environment)        // database, e-mail, payments...
    .AddIdentityServices(builder.Configuration)                               // JWT, current user, passwords
    .AddWebApi(builder.Configuration)                                         // controllers, OpenAPI, CORS, SignalR
    .AddApiRateLimiting();

var app = builder.Build();

// ---------- request pipeline (the order matters) ----------
if (app.Configuration.IsBehindReverseProxy())
    app.UseForwardedHeaders();

app.UseMiddleware<ExceptionHandlingMiddleware>();

await app.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
    app.MapApiDocumentation();

app.UseHttpsRedirection();

// CORS -> authentication -> rate limiting (knows the user) -> authorization.
app.UseCors(WebApiExtensions.FrontendCorsPolicy);
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>(NotificationHub.Path);

app.Run();
