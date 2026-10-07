using System.Security.Cryptography;
using FairShare.Application.Abstractions;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FairShare.Infrastructure.Seed;

/// <summary>
/// При првом покретању апликације генерише администраторски налог (функционалност
/// 5.2). Идемпотентно - ако налог са AdminEmail већ постоји, не ради ништа.
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
            FirstName = "Систем",
            LastName = "Администратор",
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
            $"Лозинка: {generatedPassword}{Environment.NewLine}" +
            $"Генерисано: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC{Environment.NewLine}" +
            "ОБАВЕЗНО промијените ову лозинку при првој пријави - сви остали endpointi " +
            "су блокирани (HTTP 403) док се то не уради.");
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
