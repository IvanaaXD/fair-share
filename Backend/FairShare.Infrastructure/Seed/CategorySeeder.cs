using FairShare.Domain.Entities;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FairShare.Infrastructure.Seed;

/// <summary>
/// The basic list of expense categories for a new, empty database (e.g. the first start in
/// Docker). Runs only while there are no categories at all, so categories that an administrator
/// renamed, added or deleted later are never touched. Icon keys are the ones the web app knows.
/// </summary>
public static class CategorySeeder
{
    private static readonly (string Name, string Icon)[] Defaults =
    {
        ("Намирнице", "shopping-basket"),
        ("Храна и ресторани", "utensils"),
        ("Кафићи", "coffee"),
        ("Превоз", "bus"),
        ("Гориво", "car"),
        ("Станарина", "house"),
        ("Режије", "lightbulb"),
        ("Телефон и интернет", "wifi"),
        ("Здравље", "heart-pulse"),
        ("Одјећа", "shirt"),
        ("Обућа", "footprints"),
        ("Забава", "popcorn"),
        ("Рекреација", "dumbbell"),
        ("Образовање", "graduation-cap"),
        ("Путовања", "plane"),
        ("Поклони", "gift"),
        ("Остало", "tag"),
    };

    public static async Task SeedAsync(IServiceProvider rootServices)
    {
        using var scope = rootServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FairShareDbContext>();

        if (await db.Set<Category>().AnyAsync())
            return;

        foreach (var (name, icon) in Defaults)
        {
            var category = Category.CreateApproved(name, icon);
            category.IsSystemDefined = true;
            category.CreatedAt = DateTime.UtcNow;
            db.Set<Category>().Add(category);
        }

        await db.SaveChangesAsync();
    }
}
