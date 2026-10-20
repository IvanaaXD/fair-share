using System.Text.RegularExpressions;
using FairShare.Domain.Entities;
using FluentValidation;

namespace FairShare.Application.Validators;

/// <summary>The same rules for a new category, a renamed one and an approved proposal.</summary>
internal static class CategoryValidationRules
{
    public static IRuleBuilderOptions<T, string> CategoryName<T>(this IRuleBuilder<T, string> rule)
        => rule
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Назив категорије је обавезан.")
            .Must(name => name is null || name.Trim().Length >= 2).WithMessage("Назив мора имати бар 2 карактера.")
            .MaximumLength(Category.MaxNameLength)
            .WithMessage($"Назив може имати највише {Category.MaxNameLength} карактера.");

    // Icon keys are the names the web app knows ("shopping-basket", "paw-print"...).
    public static IRuleBuilderOptions<T, string?> CategoryIcon<T>(this IRuleBuilder<T, string?> rule)
        => rule
            .Must(icon => string.IsNullOrEmpty(icon) || IconKey.IsMatch(icon))
            .WithMessage("Иконица није исправна.");

    private static readonly Regex IconKey = new("^[a-z0-9-]{1,50}$", RegexOptions.Compiled);
}
