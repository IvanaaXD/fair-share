namespace FairShare.Application.Abstractions;

/// <summary>Порука која чека слање; HTML шаблон се примјењује тек у Infrastructure слоју.</summary>
public record EmailMessage(string To, string RecipientName, string Subject, string Body);

/// <summary>
/// Ред за асинхроно слање e-mail порука. Application слој само ставља поруку у ред,
/// а слање обавља позадински сервис, тако да спор SMTP сервер не успорава одговор API-ја.
/// </summary>
public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
