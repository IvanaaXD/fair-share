using FairShare.Api.Filters;
using FairShare.Infrastructure.Data;
using FairShare.Infrastructure.Seed;
using FairShare.WebAPI.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<RequirePasswordChangeFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<RequirePasswordChangeFilter>();
});
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<FairShareDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentityServices(builder.Configuration);

builder.Services.AddRepositoryServices();

builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

await AdminUserSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();