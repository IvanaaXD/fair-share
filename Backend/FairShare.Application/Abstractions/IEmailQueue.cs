namespace FairShare.Application.Abstractions;

/// <summary>A message waiting to be sent; the HTML template is applied only in the Infrastructure layer.</summary>
public record EmailMessage(string To, string RecipientName, string Subject, string Body);

/// <summary>
/// Queue for asynchronous sending of e-mail messages. The Application layer only enqueues the message,
/// and the sending is handled by a background service, so that a slow SMTP server doesn't slow down the API response.
/// </summary>
public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
