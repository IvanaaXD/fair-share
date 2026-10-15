using System.Net.Security;
using FairShare.PaymentSimulator;
using FairShare.PaymentSimulator.Data;
using FairShare.PaymentSimulator.Endpoints;
using FairShare.PaymentSimulator.Processing;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- configuration (secrets: user-secrets in development, environment variables in Docker) ----------
builder.Services.AddOptions<SimulatorOptions>()
    .Bind(builder.Configuration.GetSection(SimulatorOptions.SectionName))
    .Validate(o => o.ApiKey.Length >= 32, "Simulator:ApiKey must be set (at least 32 characters).")
    .Validate(o => o.WebhookSecret.Length >= 32, "Simulator:WebhookSecret must be set (at least 32 characters).")
    .ValidateOnStart();

var connectionString = builder.Configuration.GetConnectionString("Payments");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:Payments is not set (see PROCITAJ_SIMULATOR.txt).");

// ---------- services ----------
builder.Services.AddDbContext<SimulatorDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton(TimeProvider.System);
var webhooks = builder.Services.AddHttpClient("webhooks", client => client.Timeout = TimeSpan.FromSeconds(10));
if (builder.Environment.IsDevelopment())
{
    // Locally FairShare runs on https://localhost with the ASP.NET development certificate.
    // Only for localhost and only in Development is that certificate accepted even if it is
    // not trusted on this machine; every other address is checked normally.
    webhooks.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, _, _, errors) =>
            errors == SslPolicyErrors.None || message.RequestUri?.IsLoopback == true
    });
}
builder.Services.AddHostedService<ChargeProcessor>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// ---------- database ----------
// In Docker the schema is created on start; locally "dotnet ef database update" is used.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<SimulatorDbContext>().Database.MigrateAsync();
}

// ---------- pipeline ----------
app.UseExceptionHandler();
app.MapSimulatorEndpoints();

app.Run();
