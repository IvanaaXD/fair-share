using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FairShare.Infrastructure.Email;

/// <summary>
/// Позадински сервис који чита поруке из EmailQueue и шаље их једну по једну.
/// Грешка при слању једне поруке се логује и не зауставља слање осталих.
/// </summary>
public class EmailBackgroundService : BackgroundService
{
    private readonly EmailQueue _queue;
    private readonly IEmailSender _sender;
    private readonly ILogger<EmailBackgroundService> _logger;

    public EmailBackgroundService(EmailQueue queue, IEmailSender sender, ILogger<EmailBackgroundService> logger)
    {
        _queue = queue;
        _sender = sender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _sender.SendAsync(message, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Слање e-mail поруке за {To} није успјело.", message.To);
            }
        }
    }
}
