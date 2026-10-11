using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FairShare.Infrastructure.Email;

/// <summary>
/// A background service that reads messages from the EmailQueue and sends them one by one.
/// An error when sending one message is logged and does not stop the sending of others.
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
                _logger.LogError(ex, "Sending e-mail message to {To} failed.", message.To);
            }
        }
    }
}