using System.Security.Cryptography;
using FairShare.Application.Abstractions;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FairShare.Infrastructure.Seed;

/// <summary>
/// Generates an administrator account upon the first application startup (feature
/// 5.2). Idempotent - if an account with AdminEmail already exists, it does nothing.
/// </summary>
public static class AdminUserSeeder
{
    private const string AdminEmail = "admin@fairshare.local";
    private const string PasswordFileName = "admin-initial-password.txt";

    public static async Task SeedAsync(IServiceProvider rootServices)
    {
        using var scope = rootServices.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        var existing = await unitOfWork.Users.GetByEmailAsync(AdminEmail);
        if (existing is not null)
            return;

        var generatedPassword = GenerateRandomPassword();

        var admin = new User
        {
            FirstName = "System",
            LastName = "Administrator",
            Email = AdminEmail,
            PasswordHash = passwordHasher.HashPassword(generatedPassword),
            Role = UserRole.Admin,
            IsBlocked = false,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.Users.AddAsync(admin);
        await unitOfWork.SaveChangesAsync();

        var filePath = Path.Combine(environment.ContentRootPath, PasswordFileName);
        await File.WriteAllTextAsync(
            filePath,
            $"Email: {AdminEmail}{Environment.NewLine}" +
            $"Password: {generatedPassword}{Environment.NewLine}" +
            $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC{Environment.NewLine}" +
            "You MUST change this password upon first login - all other endpoints " +
            "are blocked (HTTP 403) until this is done.");
    }

    private static string GenerateRandomPassword(int length = 20)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var result = new char[length];

        for (var i = 0; i < length; i++)
            result[i] = chars[bytes[i] % chars.Length];

        return new string(result);
    }
}